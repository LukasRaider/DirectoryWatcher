using System.Threading.Tasks;
using DirectoryWatcher.Models;

namespace DirectoryWatcher.Services
{
	public interface IDirectoryAnalyzerService
	{
		Task<AnalysisResult> AnalyzeDirectoryAsync(string directoryPath);
	}
}