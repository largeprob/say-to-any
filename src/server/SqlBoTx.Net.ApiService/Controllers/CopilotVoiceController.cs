using Microsoft.AspNetCore.Mvc;
using SqlBoTx.Net.Application.Contracts.CopilotVoice;
using SqlBoTx.Net.Application.Contracts.CopilotVoice.Dtos;
using SqlBoTx.Net.Core.Controller;

namespace SqlBoTx.Net.ApiService.Controllers
{
    /// <summary>
    /// AI Copilot语音录入
    /// </summary>
    [Route("[controller]")]
    [Tags("AI Copilot语音录入")]
    [Consumes("application/json")]
    public class CopilotVoiceController(ICopilotVoiceService copilotVoiceService) : LarApi
    {
        /// <summary>
        /// 语音转文字
        /// </summary>
        [HttpPost("transcribe")]
        [ProducesResponseType(200, Type = typeof(CopilotVoiceTranscriptionResultDto))]
        public async Task<IActionResult> Transcribe([FromBody] TranscribeCopilotVoiceDto input, CancellationToken cancellationToken)
        {
            return Ok(await copilotVoiceService.TranscribeAsync(input, cancellationToken));
        }
    }
}
