using Microsoft.Extensions.Configuration;
using SqlBoTx.Net.Application.Contracts.Auth;
using SqlBoTx.Net.Application.Contracts.Oss;
using SqlBoTx.Net.Application.Contracts.Oss.Dtos;
using SqlBoTx.Net.Share.Exceptions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SqlBoTx.Net.Application.Oss;

public class AliYunOssService : IAliYunOssService
{
    private static readonly HttpClient HttpClient = new();
    private readonly AliYunOssSettings _settings;
    private readonly ICurrentUserService _currentUser;

    public AliYunOssService(IConfiguration configuration, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _settings = configuration.GetSection("AliYunOss").Get<AliYunOssSettings>()
            ?? throw new InvalidOperationException("AliYunOss configuration section is missing.");
    }

    public async Task<OssUploadTokenDto> GenerateUploadTokenAsync(OssUploadTokenRequestDto input)
    {
        ValidateSettings();
        var contentType = ValidateContentType(input.ContentType);
        ValidateFileSize(input.FileSize);
        var directory = CreateUserDirectory();
        var objectKey = CreateObjectKey(directory, contentType);
        var stsToken = await AssumeRoleAsync(objectKey);
        var host = $"https://{_settings.BucketName}.{NormalizeEndpoint(_settings.Endpoint)}";

        return new OssUploadTokenDto
        {
            Region = _settings.Region,
            Endpoint = NormalizeEndpoint(_settings.Endpoint),
            Bucket = _settings.BucketName,
            AccessKeyId = stsToken.AccessKeyId,
            AccessKeySecret = stsToken.AccessKeySecret,
            SecurityToken = stsToken.SecurityToken,
            Expiration = stsToken.Expiration,
            ObjectKey = objectKey,
            CdnHost = GetCdnHost(host),
            MaxFileSizeBytes = _settings.MaxFileSizeBytes,
            AllowedContentTypes = _settings.AllowedContentTypes
        };
    }

    private async Task<StsToken> AssumeRoleAsync(string objectKey)
    {
        var accessKeyId = GetRequiredEnvironmentVariable("Ali_ACCESS_KEY_ID");
        var accessKeySecret = GetRequiredEnvironmentVariable("Ali_ACCESS_KEY_SECRET");
        var policy = BuildStsPolicy(objectKey);
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessKeyId"] = accessKeyId,
            ["Action"] = "AssumeRole",
            ["DurationSeconds"] = NormalizeTokenDuration().ToString(),
            ["Format"] = "JSON",
            ["Policy"] = policy,
            ["RoleArn"] = _settings.RoleArn,
            ["RoleSessionName"] = $"blog-upload-{Guid.NewGuid():N}",
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureNonce"] = Guid.NewGuid().ToString("N"),
            ["SignatureVersion"] = "1.0",
            ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            ["Version"] = "2015-04-01"
        };

        parameters["Signature"] = SignStsRequest(parameters, accessKeySecret);
        using var response = await HttpClient.PostAsync(NormalizeStsEndpoint(_settings.StsEndpoint), new FormUrlEncodedContent(parameters));
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new ApplicationException($"Aliyun STS AssumeRole failed: {content}");
        }

        var result = JsonSerializer.Deserialize<AssumeRoleResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result?.Credentials == null)
        {
            throw new ApplicationException("Aliyun STS AssumeRole response missing credentials.");
        }

        return result.Credentials;
    }

    private string BuildStsPolicy(string objectKey)
    {
        var policy = new
        {
            Version = "1",
            Statement = new[]
            {
                new
                {
                    Effect = "Allow",
                    Action = new[] { "oss:PutObject" },
                    Resource = new[] { $"acs:oss:*:*:{_settings.BucketName}/{_settings.UploadRootDirectory.Trim().Trim('/')}/*" }
                }
            }
        };

        return JsonSerializer.Serialize(policy);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Endpoint)) throw new ApplicationException("AliYunOss Endpoint is missing.");
        if (string.IsNullOrWhiteSpace(_settings.BucketName)) throw new ApplicationException("AliYunOss BucketName is missing.");
        if (string.IsNullOrWhiteSpace(_settings.Region)) throw new ApplicationException("AliYunOss Region is missing.");
        if (string.IsNullOrWhiteSpace(_settings.RoleArn)) throw new ApplicationException("AliYunOss RoleArn is missing.");
        if (string.IsNullOrWhiteSpace(_settings.StsEndpoint)) throw new ApplicationException("AliYunOss StsEndpoint is missing.");
        if (string.IsNullOrWhiteSpace(_settings.UploadRootDirectory)) throw new ApplicationException("AliYunOss UploadRootDirectory is missing.");
    }

    private string ValidateContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new BusinessException("OSS_CONTENT_TYPE_REQUIRED", "文件类型不能为空。");
        }

        var normalized = contentType.Trim().ToLowerInvariant();
        if (!_settings.AllowedContentTypes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException("OSS_CONTENT_TYPE_NOT_ALLOWED", "不支持的文件类型。");
        }

        return normalized;
    }

    private void ValidateFileSize(long? fileSize)
    {
        if (fileSize is null or <= 0)
        {
            throw new BusinessException("OSS_FILE_EMPTY", "文件不能为空。");
        }

        if (fileSize.Value > _settings.MaxFileSizeBytes)
        {
            throw new BusinessException("OSS_FILE_TOO_LARGE", "文件大小超过限制。");
        }
    }

    private string CreateUserDirectory()
    {
        var userId = _currentUser.UserId;
        if (userId == null)
        {
            throw new BusinessException("OSS_USER_REQUIRED", "未获取到当前登录用户。");
        }

        var root = _settings.UploadRootDirectory.Trim().Trim('/');
        return $"{root}/{userId.Value}";
    }

    private static string CreateObjectKey(string directory, string contentType)
    {
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/avif" => ".avif",
            "audio/webm" => ".webm",
            "audio/ogg" => ".ogg",
            "audio/mp4" => ".m4a",
            "audio/mpeg" => ".mp3",
            "audio/wav" => ".wav",
            "audio/x-m4a" => ".m4a",
            "audio/aac" => ".aac",
            _ => throw new BusinessException("OSS_CONTENT_TYPE_NOT_ALLOWED", "不支持的文件类型。")
        };

        return $"{directory}/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}{extension}";
    }

    private int NormalizeTokenDuration()
    {
        return Math.Clamp(_settings.TokenExpirationSeconds, 900, 3600);
    }

    private string GetCdnHost(string host)
    {
        return !string.IsNullOrWhiteSpace(_settings.CdnDomain)
            ? _settings.CdnDomain.TrimEnd('/')
            : host;
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        return endpoint.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
    }

    private static string NormalizeStsEndpoint(string endpoint)
    {
        var normalized = endpoint.Trim().TrimEnd('/');
        return normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"https://{normalized}";
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationException($"Environment variable {name} is missing.");
        }

        return value;
    }

    private static string SignStsRequest(SortedDictionary<string, string> parameters, string accessKeySecret)
    {
        var canonicalizedQueryString = BuildQueryString(parameters);
        var stringToSign = $"POST&{PercentEncode("/")}&{PercentEncode(canonicalizedQueryString)}";
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKeySecret + "&"));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
    }

    private static string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        return string.Join("&", parameters.Select(p => $"{PercentEncode(p.Key)}={PercentEncode(p.Value)}"));
    }

    private static string PercentEncode(string value)
    {
        return Uri.EscapeDataString(value)
            .Replace("+", "%20", StringComparison.Ordinal)
            .Replace("*", "%2A", StringComparison.Ordinal)
            .Replace("%7E", "~", StringComparison.Ordinal);
    }

    private class AssumeRoleResponse
    {
        public StsToken? Credentials { get; set; }
    }

    private class StsToken
    {
        public string AccessKeyId { get; set; } = string.Empty;
        public string AccessKeySecret { get; set; } = string.Empty;
        public string SecurityToken { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
    }
}
