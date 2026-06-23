using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DirectoryWatcher.Models;
using DirectoryWatcher.Repositories;
using Microsoft.Extensions.Logging;

namespace DirectoryWatcher.Services
{
	public class DirectoryAnalyzerService : IDirectoryAnalyzerService
	{
		private readonly IStateRepository _stateRepository;
		private readonly ILogger<DirectoryAnalyzerService> _logger;

		public DirectoryAnalyzerService(IStateRepository stateRepository, ILogger<DirectoryAnalyzerService> logger)
		{
			_stateRepository = stateRepository;
			_logger = logger;
		}

		public async Task<AnalysisResult> AnalyzeDirectoryAsync(string directoryPath)
		{
			var result = new AnalysisResult { AnalyzedDirectory = directoryPath };

			if (!Directory.Exists(directoryPath))
			{
				result.ErrorMessage = "Zadaný adresář neexistuje nebo k němu není přístup.";
				return result;
			}

			var allStates = await _stateRepository.LoadStateAsync();
			if (!allStates.TryGetValue(directoryPath, out var previousState))
			{
				previousState = new Dictionary<string, FileRecord>();
			}

			var currentState = new Dictionary<string, FileRecord>();
			string[] currentFiles;

			try
			{
				currentFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
			}
			catch (Exception ex)
			{
				result.ErrorMessage = $"Chyba při čtení adresáře: {ex.Message}";
				return result;
			}

			foreach (var filePath in currentFiles)
			{
				try
				{
					var relativePath = Path.GetRelativePath(directoryPath, filePath);
					var currentHash = await ComputeFileHashAsync(filePath);

					if (!previousState.TryGetValue(relativePath, out var prevRecord))
					{
						var newRecord = new FileRecord(relativePath, currentHash, 1);
						currentState[relativePath] = newRecord;
						result.NewFiles.Add(newRecord);
					}
					else
					{
						if (prevRecord.Hash != currentHash)
						{
							var changedRecord = new FileRecord(relativePath, currentHash, prevRecord.Version + 1);
							currentState[relativePath] = changedRecord;
							result.ChangedFiles.Add(changedRecord);
						}
						else
						{
							currentState[relativePath] = prevRecord; // Žádná změna, zachová starou verzi
						}
					}
				}
				catch (UnauthorizedAccessException)
				{
					_logger.LogWarning("Přístup odepřen k souboru: {FilePath}", filePath);
				}
				catch (IOException ex)
				{
					_logger.LogWarning("Soubor je používán jiným procesem: {FilePath}. Chyba: {Error}", filePath, ex.Message);
				}
			}

			// Detekce smazaných souborů
			var deletedFiles = previousState.Keys.Except(currentState.Keys);
			result.DeletedFiles.AddRange(deletedFiles);

			// Uložení aktualizovaného stavu zpět
			allStates[directoryPath] = currentState;
			await _stateRepository.SaveStateAsync(allStates);

			return result;
		}

		private async Task<string> ComputeFileHashAsync(string filePath)
		{
			using var sha256 = SHA256.Create();
			// useAsync: true optimalizuje výkon pro větší soubory (např. 50MB) 
			using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

			var hashBytes = await sha256.ComputeHashAsync(stream);
			return Convert.ToHexString(hashBytes);
		}
	}
}