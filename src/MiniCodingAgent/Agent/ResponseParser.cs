using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MiniCodingAgent.Agent;

/// <summary>
/// Parses raw model output into a <see cref="ParsedResponse"/>. Accepts both
/// compact JSON form (<c>&lt;tool&gt;{...}&lt;/tool&gt;</c>) and the verbose
/// XML form (<c>&lt;tool name="write_file" path="x"&gt;&lt;content&gt;...&lt;/content&gt;&lt;/tool&gt;</c>).
/// </summary>
public static class ResponseParser
{
    private static readonly Regex XmlToolRegex = new(
        @"<tool(?<attrs>[^>]*)>(?<body>.*?)</tool>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AttrRegex = new(
        @"([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?:""(?<dq>[^""]*)""|'(?<sq>[^']*)')",
        RegexOptions.Compiled);

    private static readonly string[] XmlChildTags =
    {
        "content",
        "old_text",
        "new_text",
        "command",
        "task",
        "pattern",
        "path",
    };

    public static ParsedResponse Parse(string? raw)
    {
        var text = raw ?? string.Empty;

        var hasJsonTool = text.Contains("<tool>", StringComparison.Ordinal);
        var hasXmlTool = text.Contains("<tool ", StringComparison.Ordinal);
        var hasFinal = text.Contains("<final>", StringComparison.Ordinal);
        var toolIndex = text.IndexOf("<tool>", StringComparison.Ordinal);
        var xmlToolIndex = text.IndexOf("<tool ", StringComparison.Ordinal);
        var finalIndex = text.IndexOf("<final>", StringComparison.Ordinal);

        if (hasJsonTool && (!hasFinal || toolIndex < finalIndex))
        {
            var body = ExtractBetween(text, "<tool>", "</tool>");
            JsonNode? payload;
            try
            {
                payload = JsonNode.Parse(body);
            }
            catch (JsonException)
            {
                return ParsedResponse.ForRetry(RetryNotice("model returned malformed tool JSON"));
            }

            if (payload is not JsonObject obj)
            {
                return ParsedResponse.ForRetry(RetryNotice("tool payload must be a JSON object"));
            }

            var name = obj["name"]?.GetValue<string?>()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return ParsedResponse.ForRetry(RetryNotice("tool payload is missing a tool name"));
            }

            if (obj["args"] is null)
            {
                obj["args"] = new JsonObject();
            }
            else if (obj["args"] is not JsonObject)
            {
                return ParsedResponse.ForRetry(RetryNotice());
            }

            var argsCopy = (JsonObject)obj["args"]!.DeepClone();
            return ParsedResponse.ForTool(name, argsCopy);
        }

        if (hasXmlTool && (!hasFinal || xmlToolIndex < finalIndex))
        {
            var xmlParsed = TryParseXmlTool(text);
            if (xmlParsed is { } parsed)
            {
                return parsed;
            }
            return ParsedResponse.ForRetry(RetryNotice());
        }

        if (hasFinal)
        {
            var inner = ExtractBetween(text, "<final>", "</final>").Trim();
            return string.IsNullOrEmpty(inner)
                ? ParsedResponse.ForRetry(RetryNotice("model returned an empty <final> answer"))
                : ParsedResponse.ForFinal(inner);
        }

        var trimmed = text.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? ParsedResponse.ForRetry(RetryNotice("model returned an empty response"))
            : ParsedResponse.ForFinal(trimmed);
    }

    public static string RetryNotice(string? problem = null)
    {
        var prefix = problem is null
            ? "Runtime notice: model returned malformed tool output"
            : $"Runtime notice: {problem}";
        return prefix + ". Reply with a valid <tool> call or a non-empty <final> answer. " +
               "For multi-line files, prefer <tool name=\"write_file\" path=\"file.py\"><content>...</content></tool>.";
    }

    private static ParsedResponse? TryParseXmlTool(string text)
    {
        var match = XmlToolRegex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var attrs = ParseAttrs(match.Groups["attrs"].Value);
        if (!attrs.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        attrs.Remove("name");

        var body = match.Groups["body"].Value;
        var args = new JsonObject();
        foreach (var kvp in attrs)
        {
            args[kvp.Key] = kvp.Value;
        }
        foreach (var tag in XmlChildTags)
        {
            if (body.Contains($"<{tag}>", StringComparison.Ordinal))
            {
                args[tag] = ExtractRaw(body, tag);
            }
        }

        var bodyTrimmed = body.Trim('\n', '\r');
        if (name == "write_file" && args["content"] is null && bodyTrimmed.Length > 0)
        {
            args["content"] = bodyTrimmed;
        }
        if (name == "delegate" && args["task"] is null && bodyTrimmed.Length > 0)
        {
            args["task"] = bodyTrimmed.Trim();
        }

        return ParsedResponse.ForTool(name.Trim(), args);
    }

    private static Dictionary<string, string> ParseAttrs(string text)
    {
        var attrs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in AttrRegex.Matches(text))
        {
            var dq = match.Groups["dq"];
            var sq = match.Groups["sq"];
            var value = dq.Success ? dq.Value : sq.Value;
            attrs[match.Groups[1].Value] = value;
        }
        return attrs;
    }

    private static string ExtractBetween(string text, string startTag, string endTag)
    {
        var start = text.IndexOf(startTag, StringComparison.Ordinal);
        if (start == -1)
        {
            return text;
        }
        start += startTag.Length;
        var end = text.IndexOf(endTag, start, StringComparison.Ordinal);
        return end == -1 ? text[start..] : text[start..end];
    }

    private static string ExtractRaw(string text, string tag)
    {
        var startTag = $"<{tag}>";
        var endTag = $"</{tag}>";
        var start = text.IndexOf(startTag, StringComparison.Ordinal);
        if (start == -1)
        {
            return text;
        }
        start += startTag.Length;
        var end = text.IndexOf(endTag, start, StringComparison.Ordinal);
        return end == -1 ? text[start..] : text[start..end];
    }
}
