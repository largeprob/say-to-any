using System;
using System.Collections.Generic;
using System.Text;

namespace SqlBoTx.Net.Application.Contracts.Oss.Dtos
{
    public class AliYunOssSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string? CdnDomain { get; set; }
        public string RoleArn { get; set; } = string.Empty;
        public string StsEndpoint { get; set; } = "https://sts.aliyuncs.com";
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
        public int TokenExpirationSeconds { get; set; } = 900;
        public string UploadRootDirectory { get; set; } = "blog";
        public string[] AllowedContentTypes { get; set; } =
        [
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif",
            "image/avif",
            "audio/webm",
            "audio/ogg",
            "audio/mp4",
            "audio/mpeg",
            "audio/wav",
            "audio/x-m4a",
            "audio/aac"
        ];
    }
}
