using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SqlBoTx.Net.Application.Contracts.CopilotVoice;
using SqlBoTx.Net.Application.Contracts.CopilotVoice.Dtos;
using SqlBoTx.Net.Share.Exceptions;

namespace SqlBoTx.Net.Application.CopilotVoice
{
    public class CopilotVoiceService(IHttpClientFactory httpFactory, IConfiguration configuration) : ICopilotVoiceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<CopilotVoiceTranscriptionResultDto> TranscribeAsync(TranscribeCopilotVoiceDto input, CancellationToken cancellationToken = default)
        {
            var apiKey = GetApiKey();
            var client = httpFactory.CreateClient("dashscope");
            var taskId = await SubmitAsync(client, apiKey, input.AudioUrl!, cancellationToken);
            using var finalResult = await PollAsync(client, apiKey, taskId, cancellationToken);
            var text = await ExtractTranscriptTextAsync(client, finalResult, cancellationToken);

            return new CopilotVoiceTranscriptionResultDto
            {
                TaskId = taskId,
                TaskStatus = GetString(finalResult.RootElement, "output", "task_status") ?? "SUCCEEDED",
                Text = text,
                AudioUrl = input.AudioUrl!,
                ConversationId = input.ConversationId,
                DurationMs = input.DurationMs
            };
        }

        private async Task<string> SubmitAsync(HttpClient client, string apiKey, string audioUrl, CancellationToken cancellationToken)
        {
            var payload = new
            {
                model = configuration["DashScope:AsrModel"] ?? "qwen3-asr-flash-filetrans",
                input = new
                {
                    file_url = audioUrl
                },
                parameters = new
                {
                    channel_id = new[] { 0 },
                    enable_itn = false,
                    enable_words = true
                }
            };

            using var request = CreateRequest(HttpMethod.Post, GetSubmitUrl(), apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await ReadOrThrowAsync(response, "语音转文字任务提交失败", cancellationToken);
            using var document = JsonDocument.Parse(body);
            var taskId = GetString(document.RootElement, "output", "task_id");

            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new BusinessException("COPILOT_VOICE_SUBMIT", $"语音转文字任务未返回task_id：{body}");
            }

            return taskId;
        }

        private async Task<JsonDocument> PollAsync(HttpClient client, string apiKey, string taskId, CancellationToken cancellationToken)
        {
            var maxWaitMs = GetInt("DashScope:AsrMaxWaitMs", 120_000);
            var pollIntervalMs = GetInt("DashScope:AsrPollIntervalMs", 2_000);
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(maxWaitMs);

            while (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(pollIntervalMs, cancellationToken);

                using var request = CreateRequest(HttpMethod.Get, $"{GetTaskQueryUrl().TrimEnd('/')}/{taskId}", apiKey);
                using var response = await client.SendAsync(request, cancellationToken);
                var body = await ReadOrThrowAsync(response, "语音转文字任务查询失败", cancellationToken);
                var document = JsonDocument.Parse(body);
                var status = GetString(document.RootElement, "output", "task_status");

                if (string.Equals(status, "SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }

                if (string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
                {
                    using (document)
                    {
                        throw new BusinessException("COPILOT_VOICE_FAILED", $"语音转文字任务失败：{body}");
                    }
                }

                document.Dispose();
            }

            throw new BusinessException("COPILOT_VOICE_TIMEOUT", "语音转文字任务超时，请稍后重试。");
        }

        private async Task<string> ExtractTranscriptTextAsync(HttpClient client, JsonDocument finalResult, CancellationToken cancellationToken)
        {
            var direct = ExtractTranscriptText(finalResult.RootElement);
            if (!string.IsNullOrWhiteSpace(direct))
            {
                return direct;
            }

            foreach (var url in FindStringProperties(finalResult.RootElement, "transcription_url").Distinct())
            {
                try
                {
                    using var response = await client.GetAsync(url, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var transcription = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    var text = ExtractTranscriptText(transcription.RootElement);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
                catch
                {
                    // Ignore malformed or inaccessible auxiliary result URLs and fall back to empty text.
                }
            }

            return string.Empty;
        }

        private static string ExtractTranscriptText(JsonElement root)
        {
            var direct =
                GetString(root, "output", "text") ??
                GetString(root, "output", "transcription") ??
                GetString(root, "output", "result", "text") ??
                GetString(root, "text") ??
                GetString(root, "transcription") ??
                GetString(root, "transcript");

            if (!string.IsNullOrWhiteSpace(direct))
            {
                return direct.Trim();
            }

            var transcriptText = ExtractTextFromNamedArrays(root, "transcripts");
            if (!string.IsNullOrWhiteSpace(transcriptText))
            {
                return transcriptText;
            }

            var sentenceText = ExtractTextFromNamedArrays(root, "sentences");
            if (!string.IsNullOrWhiteSpace(sentenceText))
            {
                return sentenceText;
            }

            var resultText = ExtractTextFromNamedArrays(root, "results");
            return resultText.Trim();
        }

        private static string ExtractTextFromNamedArrays(JsonElement root, string propertyName)
        {
            var segments = new List<string>();
            foreach (var array in FindArrayProperties(root, propertyName))
            {
                foreach (var item in array.EnumerateArray())
                {
                    var text =
                        GetString(item, "text") ??
                        GetString(item, "transcript") ??
                        GetString(item, "transcription");

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        segments.Add(text.Trim());
                    }
                }
            }

            return string.Join(" ", segments);
        }

        private static IEnumerable<JsonElement> FindArrayProperties(JsonElement root, string propertyName)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        yield return property.Value;
                    }

                    foreach (var child in FindArrayProperties(property.Value, propertyName))
                    {
                        yield return child;
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    foreach (var child in FindArrayProperties(item, propertyName))
                    {
                        yield return child;
                    }
                }
            }
        }

        private static IEnumerable<string> FindStringProperties(JsonElement root, string propertyName)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            yield return value;
                        }
                    }

                    foreach (var child in FindStringProperties(property.Value, propertyName))
                    {
                        yield return child;
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    foreach (var child in FindStringProperties(item, propertyName))
                    {
                        yield return child;
                    }
                }
            }
        }

        private static string? GetString(JsonElement root, params string[] path)
        {
            var current = root;
            foreach (var segment in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                {
                    return null;
                }
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string apiKey)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("X-DashScope-Async", "enable");
            return request;
        }

        private static async Task<string> ReadOrThrowAsync(HttpResponseMessage response, string message, CancellationToken cancellationToken)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new BusinessException("COPILOT_VOICE_DASHSCOPE", $"{message}（{(int)response.StatusCode}）：{body}");
            }

            return body;
        }

        private string GetApiKey()
        {
            var apiKey =
                Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ??
                configuration["DashScope:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new BusinessException("COPILOT_VOICE_API_KEY", "未配置DASHSCOPE_API_KEY或DashScope:ApiKey。");
            }

            return apiKey;
        }

        private string GetSubmitUrl()
        {
            return configuration["DashScope:AsrSubmitUrl"]
                   ?? "https://dashscope.aliyuncs.com/api/v1/services/audio/asr/transcription";
        }

        private string GetTaskQueryUrl()
        {
            return configuration["DashScope:TaskQueryUrl"]
                   ?? "https://dashscope.aliyuncs.com/api/v1/tasks";
        }

        private int GetInt(string key, int defaultValue)
        {
            return int.TryParse(configuration[key], out var value) ? value : defaultValue;
        }
    }
}
