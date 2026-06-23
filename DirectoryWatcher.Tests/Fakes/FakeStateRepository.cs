using DirectoryWatcher.Models;
using DirectoryWatcher.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DirectoryWatcher.Tests.Fakes
{
	public class FakeStateRepository : IStateRepository
	{
		// Paměť v RAM, která nám při testování simuluje reálný JSON soubor na disku
		public Dictionary<string, Dictionary<string, FileRecord>> SavedState { get; set; } = new();

		public Task<Dictionary<string, Dictionary<string, FileRecord>>> LoadStateAsync()
		{
			// Vracení aktuální stav zabalený do Tasku, aby to splňovalo asynchronní kontrakt
			return Task.FromResult(SavedState);
		}

		public Task SaveStateAsync(Dictionary<string, Dictionary<string, FileRecord>> state)
		{
			//  Přepsání stavu v paměti
			SavedState = state;
			return Task.CompletedTask;
		}
	}
}