using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;
using pc.Models;

namespace pc.Services;

public sealed class OpenAiCompatibleClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> TranscribeAsync(string audioFilePath, AppSettings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException("Audio file was not found.", audioFilePath);
        }

        using var timeout = CreateTimeout(settings, cancellationToken);
        var dataUri = await CreateAudioDataUriAsync(audioFilePath, timeout.Token);
        var client = CreateChatClient(settings.AsrModel, settings.AsrApiKey, settings.AsrBaseUrl);

        var asrOptions = new Dictionary<string, object?>
        {
            ["enable_itn"] = settings.AsrEnableItn
        };

        if (!string.IsNullOrWhiteSpace(settings.Language) &&
            !string.Equals(settings.Language, "auto", StringComparison.OrdinalIgnoreCase))
        {
            asrOptions["language"] = settings.Language.Trim();
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = settings.AsrModel,
            ["messages"] = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_audio",
                            input_audio = new
                            {
                                data = dataUri
                            }
                        }
                    }
                }
            },
            ["stream"] = false,
            // OpenAI Python's extra_body fields are sent as top-level JSON fields.
            ["asr_options"] = asrOptions
        };

        var body = await CompleteChatWithProtocolAsync(client, payload, timeout.Token);
        return ExtractMessageContent(body).Trim();
    }

    public async Task<string> CleanupTextAsync(string rawText, AppSettings settings, CancellationToken cancellationToken)
    {
        if (!settings.EnableTextCleanup || string.IsNullOrWhiteSpace(rawText))
        {
            return rawText;
        }

        using var timeout = CreateTimeout(settings, cancellationToken);
        var client = CreateChatClient(settings.LmModel, settings.LmApiKey, settings.LmBaseUrl);

        var payload = new
        {
            model = settings.LmModel,
            temperature = settings.LmTemperature,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = AppSettings.DefaultCleanupPrompt
                },
                new
                {
                    role = "user",
                    content = $"原始语音识别文本：\n{rawText}"
                }
            }
        };

        var body = await CompleteChatWithProtocolAsync(client, payload, timeout.Token);
        var content = ExtractMessageContent(body).Trim();
        return string.IsNullOrWhiteSpace(content) ? rawText : content;
    }

    public async Task<string> TestConnectionAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        using var timeout = CreateTimeout(settings, cancellationToken);
        var client = CreateChatClient(settings.LmModel, settings.LmApiKey, settings.LmBaseUrl);

        var chatMessages = new List<ChatMessage>() {new UserChatMessage("ping") };
        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 500,
            Temperature = 0.1f,
        };

        try
        {
            var chatResult = await client.CompleteChatAsync(chatMessages, chatOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"OpenAI SDK HTTP : {ex.Message}", ex);
        }

        //await CompleteChatWithProtocolAsync(client, payload, timeout.Token);
        return "连接成功";
    }

    private static ChatClient CreateChatClient(string model, string apiKey, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("请先填写 API Key。");
        }

        var options = new OpenAIClientOptions
        {
            Endpoint = NormalizeEndpoint(baseUrl)
        };

        return new ChatClient(model.Trim(), new ApiKeyCredential(apiKey.Trim()), options);
    }

    private static async Task<string> CompleteChatWithProtocolAsync(ChatClient client, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                BinaryData input = BinaryData.FromBytes(bytes);
                using BinaryContent content = BinaryContent.Create(input);
                ClientResult result = client.CompleteChat(content);
                return result.GetRawResponse().Content.ToString();
            }
            catch (ClientResultException ex)
            {
                throw new InvalidOperationException($"OpenAI SDK HTTP {ex.Status}: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    private static string ExtractMessageContent(string body)
    {
        using var document = JsonDocument.Parse(body);
        var choices = document.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var message = choices[0].GetProperty("message");
        if (message.TryGetProperty("content", out var content))
        {
            return ExtractContentText(content);
        }

        return string.Empty;
    }

    private static string ExtractContentText(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return content.ToString();
        }

        var builder = new StringBuilder();
        foreach (var part in content.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.String)
            {
                builder.Append(part.GetString());
                continue;
            }

            if (part.ValueKind == JsonValueKind.Object &&
                part.TryGetProperty("text", out var text))
            {
                builder.Append(text.GetString());
            }
        }

        return builder.ToString();
    }

    private static async Task<string> CreateAudioDataUriAsync(string audioFilePath, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(audioFilePath, cancellationToken);
        var mimeType = GetAudioMimeType(audioFilePath);
        return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string GetAudioMimeType(string audioFilePath)
    {
        return Path.GetExtension(audioFilePath).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".webm" => "audio/webm",
            ".wav" => "audio/wav",
            _ => "application/octet-stream"
        };
    }

    private static CancellationTokenSource CreateTimeout(AppSettings settings, CancellationToken cancellationToken)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 300)));
        return timeout;
    }

    private static Uri NormalizeEndpoint(string baseUrl)
    {
        var normalized = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://api.openai.com/v1"
            : baseUrl.Trim();

        return new Uri(normalized);
    }
}
