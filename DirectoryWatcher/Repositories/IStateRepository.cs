using System.Collections.Generic;
using System.Threading.Tasks;
using DirectoryWatcher.Models;

namespace DirectoryWatcher.Repositories
{
	public interface IStateRepository
	{
		Task<Dictionary<string, Dictionary<string, FileRecord>>> LoadStateAsync();
		Task SaveStateAsync(Dictionary<string, Dictionary<string, FileRecord>> state);
	}
}