namespace SqlBoTx.Net.Application.Contracts.CopilotVoice.Dtos
{
    public class CopilotVoiceTranscriptionResultDto
    {
        public string TaskId { get; set; } = string.Empty;

        public string TaskStatus { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string AudioUrl { get; set; } = string.Empty;

        public string? ConversationId { get; set; }

        public int? DurationMs { get; set; }
    }
}
