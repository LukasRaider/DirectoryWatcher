using System.Collections.Generic;

namespace DirectoryWatcher.Models
{
	public class AnalysisResult
	{
		public string AnalyzedDirectory { get; set; } = string.Empty;
		public List<FileRecord> NewFiles { get; set; } = new();
		public List<FileRecord> ChangedFiles { get; set; } = new();
		public List<string> DeletedFiles { get; set; } = new();
		public string? ErrorMessage { get; set; }

		public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
	}
}