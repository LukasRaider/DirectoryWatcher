namespace DirectoryWatcher.Models
{
	public record FileRecord(string RelativePath, string Hash, int Version);
}