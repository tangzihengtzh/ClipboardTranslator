using ClipboardTranslator.Api;

namespace ClipboardTranslator.Translation;

public static class TranslationPromptBuilder
{
    public const string SystemPromptChineseToEnglish =
        "Translate to English. Preserve formatting, code, names, paths, URLs and numbers. " +
        "Return JSON only: {\"text\":\"...\"}. Do not explain. Do not include the original text.";

    public const string SystemPromptEnglishToChinese =
        "Translate to Chinese. Preserve formatting, code, names, paths, URLs and numbers. " +
        "Return JSON only: {\"text\":\"...\"}. Do not explain. Do not include the original text.";

    public static string BuildSystemPrompt(TranslationDirection direction)
    {
        return direction == TranslationDirection.ChineseToEnglish
            ? SystemPromptChineseToEnglish
            : SystemPromptEnglishToChinese;
    }

    public static List<ChatMessage> BuildMessages(TranslationDirection direction, string sourceText)
    {
        return new List<ChatMessage>
        {
            new ChatMessage { Role = "system", Content = BuildSystemPrompt(direction) },
            new ChatMessage { Role = "user", Content = sourceText },
        };
    }
}
