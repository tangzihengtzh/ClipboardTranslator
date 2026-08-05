using ClipboardTranslator.Translation;

namespace ClipboardTranslator.Tests;

public class TranslationJsonParserTests
{
    [Fact]
    public void Parses_Valid_Json()
    {
        bool ok = TranslationJsonParser.TryParse("{\"text\":\"Device initialized.\"}", out string? text);
        Assert.True(ok);
        Assert.Equal("Device initialized.", text);
    }

    [Fact]
    public void Missing_Field_Fails()
    {
        bool ok = TranslationJsonParser.TryParse("{\"other\":\"x\"}", out _);
        Assert.False(ok);
    }

    [Fact]
    public void Empty_String_Value_Fails_Is_Empty()
    {
        bool ok = TranslationJsonParser.TryParse("{\"text\":\"\"}", out string? text);
        Assert.True(ok);
        Assert.Equal("", text);
    }

    [Fact]
    public void Strips_Markdown_Fence()
    {
        const string raw = "```json\n{\"text\":\"你好世界\"}\n```";
        bool ok = TranslationJsonParser.TryParse(raw, out string? text);
        Assert.True(ok);
        Assert.Equal("你好世界", text);
    }

    [Fact]
    public void Invalid_Json_Fails()
    {
        bool ok = TranslationJsonParser.TryParse("not json at all", out _);
        Assert.False(ok);
    }

    [Fact]
    public void Not_Object_Fails()
    {
        bool ok = TranslationJsonParser.TryParse("[1,2,3]", out _);
        Assert.False(ok);
    }

    [Fact]
    public void Null_Text_Field_Fails()
    {
        bool ok = TranslationJsonParser.TryParse("{\"text\":null}", out _);
        Assert.False(ok);
    }
}
