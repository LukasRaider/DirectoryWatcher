using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectoryWatcher.Models;
using DirectoryWatcher.Repositories;
using DirectoryWatcher.Services;
using DirectoryWatcher.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DirectoryWatcher.Tests
{
	// IDisposable, ať po sobě po každém testu uklidíme zbytkové soubory
	public class DirectoryAnalyzerServiceTests : IDisposable
	{
		private readonly Mock<IStateRepository> _stateRepoMock;
		private readonly Mock<ILogger<DirectoryAnalyzerService>> _loggerMock;
		private readonly string _tempDirectoryPath;

		// Nové mockování, čisté mocky a generování unikátní složku
		public DirectoryAnalyzerServiceTests()
		{
			_stateRepoMock = new Mock<IStateRepository>();
			_loggerMock = new Mock<ILogger<DirectoryAnalyzerService>>();

			_tempDirectoryPath = Path.Combine(Path.GetTempPath(), "WatcherTest_" + Guid.NewGuid().ToString());
			Directory.CreateDirectory(_tempDirectoryPath);
		}

		// Čistění po testech i v případě neuspěchu
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
			// 1. Příprava: Přidání do sandboxu úplně nový soubor
			var testFilePath = Path.Combine(_tempDirectoryPath, "dummy.txt");
			await File.WriteAllTextAsync(testFilePath, "Ahoj, toto je testovací obsah.");

			// Simulace, že databáze je prázdný a bez složky
			_stateRepoMock.Setup(r => r.LoadStateAsync())
				.ReturnsAsync(new Dictionary<string, Dictionary<string, FileRecord>>());

			var service = new DirectoryAnalyzerService(_stateRepoMock.Object, _loggerMock.Object);

			// 2. Akce:  Spustění analýzy adresáře
			var result = await service.AnalyzeDirectoryAsync(_tempDirectoryPath);

			// 3. Kontrola: Musí to projít a nový soubor musí dostat automaticky startovací verzi 1
			Assert.True(result.IsSuccess);
			Assert.Single(result.NewFiles);
			Assert.Equal("dummy.txt", result.NewFiles[0].RelativePath);
			Assert.Equal(1, result.NewFiles[0].Version);
		}

		[Fact]
		public async Task AnalyzeDirectoryAsync_ZmenenySoubor_ZvedaVerziOJednu()
		{
			// 1. Příprava: Vytvoření souboru na disku s novým obsahem
			var testFilePath = Path.Combine(_tempDirectoryPath, "zmena.txt");
			await File.WriteAllTextAsync(testFilePath, "Novy upraveny obsah souboru");

			// Simulace, že v historii už soubor máme, ale se starým hashem a verzí 1
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

			// 2. Akce: Necháme službu zanalyzovat změnu
			var result = await service.AnalyzeDirectoryAsync(_tempDirectoryPath);

			// 3. Kontrola: Služba musí detekovat změnu a inkrementovat verzi na 2
			Assert.Single(result.ChangedFiles);
			Assert.Equal("zmena.txt", result.ChangedFiles[0].RelativePath);
			Assert.Equal(2, result.ChangedFiles[0].Version);

			// Jištění, že služba na konci poslala nová data k uložení do repozitáře
			_stateRepoMock.Verify(r => r.SaveStateAsync(It.IsAny<Dictionary<string, Dictionary<string, FileRecord>>>()), Times.Once);
		}

		[Fact]
		public async Task AnalyzeDirectoryAsync_NalezenNovySouborBezMoqu_UloziDataDoFakeRepozitare()
		{
			// 1. Příprava: Fake repositář z paměti
			var fakeRepo = new FakeStateRepository();

			var testFilePath = Path.Combine(_tempDirectoryPath, "novacek.txt");
			await File.WriteAllTextAsync(testFilePath, "Obsah testovaný přes ručně psaný fake.");

			// Injection fake soubor
			var service = new DirectoryAnalyzerService(fakeRepo, _loggerMock.Object);

			// 2. Akce:  Analýzu složky
			var result = await service.AnalyzeDirectoryAsync(_tempDirectoryPath);

			// 3. Kontrola: Ověření, zda náš FakeStateRepository v paměti skutečně zachytil a uložil data
			Assert.True(result.IsSuccess);
			Assert.NotEmpty(fakeRepo.SavedState);
		}

		[Fact]
		public async Task AnalyzeDirectoryAsync_SlozkaNeexistuje_VyhodiDirectoryNotFoundException()
		{
			// 1. Příprava: Inicializace a zadání neexistující cesty
			var service = new DirectoryAnalyzerService(_stateRepoMock.Object, _loggerMock.Object);
			string neplatnaCesta = Path.Combine(_tempDirectoryPath, "TohleFaktNaDiskuNenajdes_404");

			// 2. Akce: Volání metody
			var result = await service.AnalyzeDirectoryAsync(neplatnaCesta);

			// 3. Kontrola: Ověření, že metoda vrátila objekt s příznakem neúspěchu
			Assert.False(result.IsSuccess);
		}
	}
}