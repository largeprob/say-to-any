namespace SqlBoTx.Net.Application.Contracts.Oss.Dtos;

public class OssPostPolicyDto
{
    public string Host { get; set; } = string.Empty;
    public string Dir { get; set; } = string.Empty;
    public string Policy { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string OSSAccessKeyId { get; set; } = string.Empty;
    public string Expire { get; set; } = string.Empty;
    public string Credential { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string CdnHost { get; set; } = string.Empty;
}
