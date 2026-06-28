using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SqlBoTx.Net.Application.Contracts.CopilotVoice.Dtos
{
    public class TranscribeCopilotVoiceDto
    {
        [DisplayName("音频地址")]
        [Required(ErrorMessage = "{0}不能为空")]
        public string? AudioUrl { get; set; }

        [DisplayName("会话ID")]
        public string? ConversationId { get; set; }

        [DisplayName("录音时长")]
        public int? DurationMs { get; set; }
    }
}
