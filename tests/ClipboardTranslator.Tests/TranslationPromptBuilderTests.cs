using ClipboardTranslator.Translation;

namespace ClipboardTranslator.Tests;

public class TranslationPromptBuilderTests
{
    [Fact]
    public void ChineseToEnglish_Uses_English_SystemPrompt()
    {
        var messages = TranslationPromptBuilder.BuildMessages(TranslationDirection.ChineseToEnglish, "你好");
        Assert.Equal(2, messages.Count);
        Assert.Equal("system", messages[0].Role);
        Assert.Contains("Translate to English", messages[0].Content);
        Assert.Equal("user", messages[1].Role);
        Assert.Equal("你好", messages[1].Content);
    }

    [Fact]
    public void EnglishToChinese_Uses_Chinese_SystemPrompt()
    {
        var messages = TranslationPromptBuilder.BuildMessages(TranslationDirection.EnglishToChinese, "hello");
        Assert.Contains("Translate to Chinese", messages[0].Content);
    }

    [Fact]
    public void SystemPrompt_Requires_JsonOnly_And_PreserveFormatting()
    {
        var prompt = TranslationPromptBuilder.SystemPromptChineseToEnglish;
        Assert.Contains("JSON only", prompt);
        Assert.Contains("Preserve formatting", prompt);
        Assert.DoesNotContain("markdown", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
