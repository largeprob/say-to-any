using Microsoft.AspNetCore.Mvc;
using SqlBoTx.Net.Application.Contracts.Oss;
using SqlBoTx.Net.Application.Contracts.Oss.Dtos;
using SqlBoTx.Net.Core.Controller;

namespace SqlBoTx.Net.ApiService.Controllers
{
    [Route("[controller]")]
    [Tags("OSS上传")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class OssController : LarApi
    {
        private readonly IAliYunOssService _ossService;

        public OssController(IAliYunOssService ossService)
        {
            _ossService = ossService;
        }

        /// <summary>
        /// 获取OSS直传临时凭证
        /// </summary>
        [HttpPost("upload-token")]
        [ProducesResponseType(typeof(OssUploadTokenDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUploadToken([FromBody] OssUploadTokenRequestDto input)
        {
            var result = await _ossService.GenerateUploadTokenAsync(input);
            return Ok(result);
        }
    }
}
