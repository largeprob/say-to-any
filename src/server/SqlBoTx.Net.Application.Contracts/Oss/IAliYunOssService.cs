using SqlBoTx.Net.Application.Contracts.Oss.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlBoTx.Net.Application.Contracts.Oss
{
    public interface IAliYunOssService
    {
        Task<OssUploadTokenDto> GenerateUploadTokenAsync(OssUploadTokenRequestDto input);
    }
}
