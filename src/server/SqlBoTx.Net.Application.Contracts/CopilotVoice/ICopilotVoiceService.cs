using SqlBoTx.Net.Application.Contracts.CopilotVoice.Dtos;

namespace SqlBoTx.Net.Application.Contracts.CopilotVoice
{
    public interface ICopilotVoiceService
    {
        Task<CopilotVoiceTranscriptionResultDto> TranscribeAsync(TranscribeCopilotVoiceDto input, CancellationToken cancellationToken = default);
    }
}
