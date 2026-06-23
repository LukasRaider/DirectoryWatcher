using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DirectoryWatcher.Models;

namespace DirectoryWatcher.Repositories
{
	public class JsonStateRepository : IStateRepository
	{
		private readonly string _stateFilePath = "state.json";

		public async Task<Dictionary<string, Dictionary<string, FileRecord>>> LoadStateAsync()
		{
			if (!File.Exists(_stateFilePath)) return new();

			try
			{
				using var stream = new FileStream(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				return await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, FileRecord>>>(stream) ?? new();
			}
			catch (Exception)
			{
				// Pokud je soubor poškozený nebo nečitelný, vrátí prázdný stav
				return new();
			}
		}

		public async Task SaveStateAsync(Dictionary<string, Dictionary<string, FileRecord>> state)
		{
			using var stream = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
			await JsonSerializer.SerializeAsync(stream, state, new JsonSerializerOptions { WriteIndented = true });
		}
	}
}