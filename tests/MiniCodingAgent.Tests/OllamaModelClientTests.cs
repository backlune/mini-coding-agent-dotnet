using System.Net;
using System.Text.Json.Nodes;
using MiniCodingAgent.Models;

namespace MiniCodingAgent.Tests;

public sealed class OllamaModelClientTests
{
    [Fact]
    public void Posts_expected_payload_to_generate_endpoint()
    {
        var handler = new StubHttpHandler("{\"response\":\"<final>ok</final>\"}");
        using var client = new OllamaModelClient(
            new OllamaOptions("qwen3.5:4b", "http://127.0.0.1:11434", Temperature: 0.2, TopP: 0.9, Timeout: TimeSpan.FromSeconds(30)),
            handler);

        var result = client.Complete("hello", 42);

        Assert.Equal("<final>ok</final>", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("http://127.0.0.1:11434/api/generate", handler.LastRequest!.RequestUri!.ToString());

        var body = JsonNode.Parse(handler.LastRequestBody!)!.AsObject();
        Assert.Equal("qwen3.5:4b", body["model"]!.GetValue<string>());
        Assert.Equal("hello", body["prompt"]!.GetValue<string>());
        Assert.False(body["stream"]!.GetValue<bool>());
        Assert.False(body["raw"]!.GetValue<bool>());
        Assert.False(body["think"]!.GetValue<bool>());
        Assert.Equal(42, body["options"]!["num_predict"]!.GetValue<int>());
    }

    [Fact]
    public void Surfaces_http_errors_with_body()
    {
        var handler = new StubHttpHandler("bad model", HttpStatusCode.InternalServerError);
        using var client = new OllamaModelClient(
            new OllamaOptions("qwen3.5:4b", "http://127.0.0.1:11434", 0.2, 0.9, TimeSpan.FromSeconds(5)),
            handler);

        var ex = Assert.Throws<InvalidOperationException>(() => client.Complete("x", 1));
        Assert.Contains("HTTP 500", ex.Message);
        Assert.Contains("bad model", ex.Message);
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _status;

        public StubHttpHandler(string responseBody, HttpStatusCode status = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _status = status;
        }

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseBody),
            };
        }
    }
}
