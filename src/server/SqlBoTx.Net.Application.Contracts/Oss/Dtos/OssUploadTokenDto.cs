namespace SqlBoTx.Net.Application.Contracts.Oss.Dtos;

public class OssUploadTokenDto
{
    public string Region { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string SecurityToken { get; set; } = string.Empty;
    public string Expiration { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string CdnHost { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; }
    public IReadOnlyCollection<string> AllowedContentTypes { get; set; } = [];
}
