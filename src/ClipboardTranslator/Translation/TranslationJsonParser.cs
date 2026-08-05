using System.Text.Json;

namespace ClipboardTranslator.Translation;

public static class TranslationJsonParser
{
    /// <summary>
    /// 从模型返回内容中解析 {"text":"..."}，容忍 Markdown 代码块包裹。
    /// </summary>
    public static bool TryParse(string raw, out string? text)
    {
        text = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string candidate = raw.Trim();

        // 移除 ```json ... ``` 代码块标记
        candidate = StripMarkdownFence(candidate);

        try
        {
            using var doc = JsonDocument.Parse(candidate);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            if (!doc.RootElement.TryGetProperty("text", out var prop))
                return false;
            if (prop.ValueKind != JsonValueKind.String)
                return false;
            text = prop.GetString();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string StripMarkdownFence(string input)
    {
        string trimmed = input.Trim();
        if (!trimmed.StartsWith("```"))
            return input;

        // 去掉开头的 ```[json] 行
        int firstLineEnd = trimmed.IndexOf('\n');
        if (firstLineEnd < 0)
            return input;

        string rest = trimmed[(firstLineEnd + 1)..];

        // 去掉结尾的 ```
        int lastFence = rest.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence >= 0)
            rest = rest[..lastFence];

        return rest.Trim();
    }
}
