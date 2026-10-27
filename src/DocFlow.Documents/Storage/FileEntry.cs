namespace DocFlow.Documents;

public sealed record FileEntry(string FullPath, string Name, long Size, DateTime LastWriteUtc);
