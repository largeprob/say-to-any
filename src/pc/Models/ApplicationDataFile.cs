namespace pc.Models;

public sealed class ApplicationDataFile
{
    public AppSettings Settings { get; set; } = new();

    public List<DictationHistoryItem> History { get; set; } = [];
}
