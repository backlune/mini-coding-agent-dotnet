using System.Net;
using System.Text.Json.Nodes;
using MiniCodingAgent.Models;

namespace MiniCodingAgent.Tests;

public sealed class LmStudioModelClientTests
{
    [Fact]
    public void Posts_expected_payload_to_chat_completions_endpoint()
    {
        var handler = new StubHttpHandler(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"<final>ok</final>\"}}]}");
        using var client = new LmStudioModelClient(
            new LmStudioOptions("qwen3.5:4b", "http://127.0.0.1:1234", Temperature: 0.2, TopP: 0.9, Timeout: TimeSpan.FromSeconds(30)),
            handler);

        var result = client.Complete("hello", 42);

        Assert.Equal("<final>ok</final>", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("http://127.0.0.1:1234/v1/chat/completions", handler.LastRequest!.RequestUri!.ToString());

        var body = JsonNode.Parse(handler.LastRequestBody!)!.AsObject();
        Assert.Equal("qwen3.5:4b", body["model"]!.GetValue<string>());
        Assert.False(body["stream"]!.GetValue<bool>());
        Assert.Equal(42, body["max_tokens"]!.GetValue<int>());
        Assert.Equal(0.2, body["temperature"]!.GetValue<double>());
        Assert.Equal(0.9, body["top_p"]!.GetValue<double>());

        var messages = body["messages"]!.AsArray();
        Assert.Single(messages);
        Assert.Equal("user", messages[0]!["role"]!.GetValue<string>());
        Assert.Equal("hello", messages[0]!["content"]!.GetValue<string>());
    }

    [Fact]
    public void Trims_trailing_slash_from_host()
    {
        var handler = new StubHttpHandler("{\"choices\":[{\"message\":{\"content\":\"x\"}}]}");
        using var client = new LmStudioModelClient(
            new LmStudioOptions("m", "http://127.0.0.1:1234/", 0.2, 0.9, TimeSpan.FromSeconds(5)),
            handler);

        client.Complete("x", 1);

        Assert.Equal("http://127.0.0.1:1234/v1/chat/completions", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public void Returns_empty_string_when_choices_missing()
    {
        var handler = new StubHttpHandler("{\"choices\":[]}");
        using var client = new LmStudioModelClient(
            new LmStudioOptions("m", "http://127.0.0.1:1234", 0.2, 0.9, TimeSpan.FromSeconds(5)),
            handler);

        Assert.Equal(string.Empty, client.Complete("x", 1));
    }

    [Fact]
    public void Surfaces_http_errors_with_body()
    {
        var handler = new StubHttpHandler("model not loaded", HttpStatusCode.InternalServerError);
        using var client = new LmStudioModelClient(
            new LmStudioOptions("qwen3.5:4b", "http://127.0.0.1:1234", 0.2, 0.9, TimeSpan.FromSeconds(5)),
            handler);

        var ex = Assert.Throws<InvalidOperationException>(() => client.Complete("x", 1));
        Assert.Contains("HTTP 500", ex.Message);
        Assert.Contains("model not loaded", ex.Message);
    }

    [Fact]
    public void Surfaces_api_error_object_in_body()
    {
        var handler = new StubHttpHandler("{\"error\":{\"message\":\"no model loaded\"}}");
        using var client = new LmStudioModelClient(
            new LmStudioOptions("m", "http://127.0.0.1:1234", 0.2, 0.9, TimeSpan.FromSeconds(5)),
            handler);

        var ex = Assert.Throws<InvalidOperationException>(() => client.Complete("x", 1));
        Assert.Contains("no model loaded", ex.Message);
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
