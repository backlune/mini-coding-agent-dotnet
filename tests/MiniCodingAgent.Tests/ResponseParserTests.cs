using MiniCodingAgent.Agent;

namespace MiniCodingAgent.Tests;

public sealed class ResponseParserTests
{
    [Fact]
    public void Parses_json_tool_call()
    {
        var parsed = ResponseParser.Parse("<tool>{\"name\":\"list_files\",\"args\":{\"path\":\".\"}}</tool>");

        Assert.Equal(ParsedResponseKind.Tool, parsed.Kind);
        Assert.Equal("list_files", parsed.ToolName);
        Assert.Equal(".", parsed.ToolArgs!["path"]!.GetValue<string>());
    }

    [Fact]
    public void Retry_on_non_object_args()
    {
        var parsed = ResponseParser.Parse("<tool>{\"name\":\"read_file\",\"args\":\"bad\"}</tool>");
        Assert.Equal(ParsedResponseKind.Retry, parsed.Kind);
    }

    [Fact]
    public void Parses_xml_write_file_with_nested_content()
    {
        var parsed = ResponseParser.Parse(
            "<tool name=\"write_file\" path=\"hello.py\"><content>print(\"hi\")\n</content></tool>");

        Assert.Equal(ParsedResponseKind.Tool, parsed.Kind);
        Assert.Equal("write_file", parsed.ToolName);
        Assert.Equal("hello.py", parsed.ToolArgs!["path"]!.GetValue<string>());
        Assert.Equal("print(\"hi\")\n", parsed.ToolArgs!["content"]!.GetValue<string>());
    }

    [Fact]
    public void Final_answer_parsed_from_tag()
    {
        var parsed = ResponseParser.Parse("<final>All done.</final>");
        Assert.Equal(ParsedResponseKind.Final, parsed.Kind);
        Assert.Equal("All done.", parsed.Final);
    }

    [Fact]
    public void Empty_input_becomes_retry()
    {
        var parsed = ResponseParser.Parse(string.Empty);
        Assert.Equal(ParsedResponseKind.Retry, parsed.Kind);
    }

    [Fact]
    public void Bare_text_treated_as_final()
    {
        var parsed = ResponseParser.Parse("just a sentence");
        Assert.Equal(ParsedResponseKind.Final, parsed.Kind);
        Assert.Equal("just a sentence", parsed.Final);
    }
}
