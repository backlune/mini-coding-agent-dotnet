using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MiniCodingAgent.Models;

/// <summary>
/// Options accepted by <see cref="LmStudioModelClient"/>. Mirrors
/// <see cref="OllamaOptions"/> so the CLI layer can treat both backends
/// symmetrically.
/// </summary>
public sealed record LmStudioOptions(
    string Model,
    string Host,
    double Temperature,
    double TopP,
    TimeSpan Timeout);

/// <summary>
/// HTTP client that talks to LM Studio's OpenAI-compatible
/// <c>/v1/chat/completions</c> endpoint. The agent only needs a single prompt
/// completion, so we wrap the prompt as a lone user message and pull the reply
/// out of <c>choices[0].message.content</c>.
/// </summary>
public sealed class LmStudioModelClient : IModelClient, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly LmStudioOptions _options;

    public LmStudioModelClient(LmStudioOptions options, HttpMessageHandler? handler = null)
    {
        _options = options;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.Timeout = options.Timeout;
        _ownsHttp = true;
    }

    public string Complete(string prompt, int maxNewTokens)
    {
        var payload = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages = new[]
            {
                new ChatMessage { Role = "user", Content = prompt },
            },
            Stream = false,
            MaxTokens = maxNewTokens,
            Temperature = _options.Temperature,
            TopP = _options.TopP,
        };

        var url = _options.Host.TrimEnd('/') + "/v1/chat/completions";

        HttpResponseMessage response;
        try
        {
            response = _http.PostAsJsonAsync(url, payload, SerializerOptions).GetAwaiter().GetResult();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Could not reach LM Studio.\n" +
                "Make sure the LM Studio local server is running and a model is loaded.\n" +
                $"Host: {_options.Host}\n" +
                $"Model: {_options.Model}",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException(
                $"LM Studio request timed out after {_options.Timeout.TotalSeconds:0}s.",
                ex);
        }

        using (response)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"LM Studio request failed with HTTP {(int)response.StatusCode}: {body}");
            }

            var node = JsonNode.Parse(body) as JsonObject
                ?? throw new InvalidOperationException($"LM Studio returned non-object response: {body}");

            if (node["error"] is JsonNode err)
            {
                var errText = err is JsonValue v && v.TryGetValue(out string? s)
                    ? s
                    : err["message"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(errText))
                {
                    throw new InvalidOperationException($"LM Studio error: {errText}");
                }
            }

            var choices = node["choices"] as JsonArray;
            if (choices is null || choices.Count == 0)
            {
                return string.Empty;
            }

            var message = choices[0]?["message"] as JsonObject;
            return message?["content"]?.GetValue<string>() ?? string.Empty;
        }
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("messages")] public IReadOnlyList<ChatMessage> Messages { get; set; } = Array.Empty<ChatMessage>();
        [JsonPropertyName("stream")] public bool Stream { get; set; }
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
        [JsonPropertyName("top_p")] public double TopP { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }
}
