using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectoryWatcher.Models;
using DirectoryWatcher.Repositories;
using DirectoryWatcher.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DirectoryWatcher.Tests
{
	// Implementace IDisposable zajistí, že po každém testu po sobě uklidíme nepořádek na disku
	public class DirectoryAnalyzerServiceTests : IDisposable
	{
		private readonly Mock<IStateRepository> _stateRepoMock;
		private readonly Mock<ILogger<DirectoryAnalyzerService>> _loggerMock;
		private readonly string _tempDirectoryPath;

		public DirectoryAnalyzerServiceTests()
		{
			_stateRepoMock = new Mock<IStateRepository>();
			_loggerMock = new Mock<ILogger<DirectoryAnalyzerService>>();

			// Vytvoříme unikátní dočasnou složku v systému pro každý běžící test
			_tempDirectoryPath = Path.Combine(Path.GetTempPath(), "WatcherTest_" + Guid.NewGuid().ToString());
			Directory.CreateDirectory(_tempDirectoryPath);
		}

		// Tuta metoda se spustí automaticky po skončení KAŽDÉHO testu
		public void Dispose()
		{
			if (Directory.Exists(_tempDirectoryPath))
			{
				Directory.Delete(_tempDirectoryPath, true);
			}
		}

		[Fact]
		public async Task AnalyzeDirectoryAsync_NovySoubor_VraciVerzi1()
		{
			// --- ARRANGE (Příprava dat) ---
			// Vytvoříme fyzický soubor v našem dočasném sandboxu
			var testFilePath = Path.Combine(_tempDirectoryPath, "dummy.txt");
			await File.WriteAllTextAsync(testFilePath, "Ahoj, toto je testovací obsah.");

			// Simulujeme, že v "databázi" (JSONu) ještě žádná historie pro tuto složku není
			_stateRepoMock.Setup(r => r.LoadStateAsync())
				.ReturnsAsync(new Dictionary<string, Dictionary<string, FileRecord>>());

			var service = new DirectoryAnalyzerService(_stateRepoMock.Object, _loggerMock.Object);

			// --- ACT (Provedení akce) ---
			var result = await service.AnalyzeDirectoryAsync(_tempDirectoryPath);

			// --- ASSERT (Ověření výsledků) ---
			Assert.True(result.IsSuccess);
			Assert.Single(result.NewFiles); // Očekáváme přesně 1 nový soubor
			Assert.Equal("dummy.txt", result.NewFiles[0].RelativePath);
			Assert.Equal(1, result.NewFiles[0].Version); // První verze musí být 1
		}

		[Fact]
		public async Task AnalyzeDirectoryAsync_ZmenenySoubor_ZvedaVerziOOndu()
		{
			// --- ARRANGE (Příprava dat) ---
			var testFilePath = Path.Combine(_tempDirectoryPath, "zmena.txt");
			await File.WriteAllTextAsync(testFilePath, "Novy upraveny obsah souboru");

			// Simulujeme, že soubor už v historii existoval, ale měl jiný hash a verzi 1
			var falesnaHistorie = new Dictionary<string, Dictionary<string, FileRecord>>
			{
				{
					_tempDirectoryPath, new Dictionary<string, FileRecord>
					{
						{ "zmena.txt", new FileRecord("zmena.txt", "STARY_NEPLATNY_HASH_123", 1) }
					}
				}
			};

			_stateRepoMock.Setup(r => r.LoadStateAsync()).ReturnsAsync(falesnaHistorie);

			var service = new DirectoryAnalyzerService(_stateRepoMock.Object, _loggerMock.Object);

			// --- ACT (Provedení akce) ---
			var result = await service.AnalyzeDirectoryAsync(_tempDirectoryPath);

			// --- ASSERT (Ověření výsledků) ---
			Assert.Single(result.ChangedFiles);
			Assert.Equal("zmena.txt", result.ChangedFiles[0].RelativePath);
			Assert.Equal(2, result.ChangedFiles[0].Version); // Verze se musela inkrementovat na 2

			// Ověříme, že služba na konci správně zavolala uložení nového stavu
			_stateRepoMock.Verify(r => r.SaveStateAsync(It.IsAny<Dictionary<string, Dictionary<string, FileRecord>>>()), Times.Once);
		}
	}
}