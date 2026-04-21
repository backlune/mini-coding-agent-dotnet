using MiniCodingAgent.Tests.Helpers;
using MiniCodingAgent.Ui;

namespace MiniCodingAgent.Tests;

public sealed class WelcomeBannerTests
{
    [Fact]
    public void Keeps_box_shape_for_long_paths()
    {
        using var tmp = new TempDirectory();
        var deep = tmp.Combine("very", "long", "path", "for", "the", "mini", "agent", "welcome", "screen");
        Directory.CreateDirectory(deep);
        var agent = TestHarness.BuildAgent(deep, Array.Empty<string>());

        var welcome = WelcomeBanner.Build(agent, model: "qwen3.5:4b", host: "http://127.0.0.1:11434", terminalWidth: 80);
        var lines = welcome.Split('\n', StringSplitOptions.None)
            .Select(line => line.TrimEnd('\r'))
            .ToArray();

        Assert.True(lines.Length >= 5);
        Assert.Single(lines.Select(line => line.Length).Distinct());
        Assert.Contains("...", welcome);
        Assert.Contains("O   O", welcome);
        Assert.DoesNotContain("MINI-CODING-AGENT", welcome);
        Assert.Contains("MINI CODING AGENT", welcome);
    }
}
