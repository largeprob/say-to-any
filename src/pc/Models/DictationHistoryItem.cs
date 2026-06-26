using System.IO;

namespace pc.Models;

public sealed class DictationHistoryItem
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public string RawText { get; set; } = string.Empty;

    public string FinalText { get; set; } = string.Empty;

    public string AudioFilePath { get; set; } = string.Empty;

    public string CreatedAtText => CreatedAt.LocalDateTime.ToString("MM-dd HH:mm");

    public string AudioFileName => string.IsNullOrWhiteSpace(AudioFilePath)
        ? $"dictation-{CreatedAt.LocalDateTime:yyyyMMdd-HHmmss}.wav"
        : Path.GetFileName(AudioFilePath);

    public string AudioStateText => HasAudioFile ? "音频已保存" : "音频不可用";

    public bool HasAudioFile => !string.IsNullOrWhiteSpace(AudioFilePath) && File.Exists(AudioFilePath);
}
