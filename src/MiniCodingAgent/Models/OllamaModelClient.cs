using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MiniCodingAgent.Models;

/// <summary>
/// Options accepted by <see cref="OllamaModelClient"/>. Kept as a record so
/// the CLI layer can construct it declaratively from parsed arguments.
/// </summary>
public sealed record OllamaOptions(
    string Model,
    string Host,
    double Temperature,
    double TopP,
    TimeSpan Timeout);

/// <summary>
/// HTTP client that talks to Ollama's <c>/api/generate</c> endpoint.
/// Uses <see cref="HttpClient"/> rather than raw sockets, and accepts an
/// injected handler so tests can stub the HTTP layer without hitting the wire.
/// </summary>
public sealed class OllamaModelClient : IModelClient, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly OllamaOptions _options;

    public OllamaModelClient(OllamaOptions options, HttpMessageHandler? handler = null)
    {
        _options = options;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.Timeout = options.Timeout;
        _ownsHttp = true;
    }

    public string Complete(string prompt, int maxNewTokens)
    {
        var payload = new OllamaGenerateRequest
        {
            Model = _options.Model,
            Prompt = prompt,
            Stream = false,
            Raw = false,
            Think = false,
            Options = new OllamaSamplingOptions
            {
                NumPredict = maxNewTokens,
                Temperature = _options.Temperature,
                TopP = _options.TopP,
            },
        };

        var url = _options.Host.TrimEnd('/') + "/api/generate";

        HttpResponseMessage response;
        try
        {
            response = _http.PostAsJsonAsync(url, payload, SerializerOptions).GetAwaiter().GetResult();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Could not reach Ollama.\n" +
                "Make sure `ollama serve` is running and the model is available.\n" +
                $"Host: {_options.Host}\n" +
                $"Model: {_options.Model}",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException(
                $"Ollama request timed out after {_options.Timeout.TotalSeconds:0}s.",
                ex);
        }

        using (response)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Ollama request failed with HTTP {(int)response.StatusCode}: {body}");
            }

            var node = JsonNode.Parse(body) as JsonObject
                ?? throw new InvalidOperationException($"Ollama returned non-object response: {body}");

            if (node["error"] is JsonValue err && err.TryGetValue(out string? errText) && !string.IsNullOrEmpty(errText))
            {
                throw new InvalidOperationException($"Ollama error: {errText}");
            }

            return node["response"]?.GetValue<string>() ?? string.Empty;
        }
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }

    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("prompt")] public string Prompt { get; set; } = string.Empty;
        [JsonPropertyName("stream")] public bool Stream { get; set; }
        [JsonPropertyName("raw")] public bool Raw { get; set; }
        [JsonPropertyName("think")] public bool Think { get; set; }
        [JsonPropertyName("options")] public OllamaSamplingOptions Options { get; set; } = new();
    }

    private sealed class OllamaSamplingOptions
    {
        [JsonPropertyName("num_predict")] public int NumPredict { get; set; }
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
        [JsonPropertyName("top_p")] public double TopP { get; set; }
    }
}
