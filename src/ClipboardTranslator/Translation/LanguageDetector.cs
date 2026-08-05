namespace ClipboardTranslator.Translation;

public enum TranslationDirection
{
    ChineseToEnglish,
    EnglishToChinese,
}

public static class LanguageDetector
{
    /// <summary>
    /// 本地判断翻译方向：统计中文字符与英文字母，按阈值判定。
    /// 返回 null 表示未检测到自然语言。
    /// </summary>
    public static TranslationDirection? Detect(string text, double chineseThreshold = 0.1)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        int chineseCount = 0;
        int englishCount = 0;

        foreach (char c in text)
        {
            if (IsChinese(c))
                chineseCount++;
            else if (IsEnglishLetter(c))
                englishCount++;
        }

        int total = chineseCount + englishCount;
        if (total == 0)
            return null;

        double ratio = (double)chineseCount / total;
        if (ratio >= chineseThreshold)
            return TranslationDirection.ChineseToEnglish;

        // 英文为主：若整段无空格且形似路径/URL，判定为无自然语言
        if (LooksLikePathOrUrl(text))
            return null;

        return TranslationDirection.EnglishToChinese;
    }

    /// <summary>
    /// 路径/URL 通常整段无空格，且含 \ / : . @ 等分隔符，按需求 8.4.4 视为无自然语言。
    /// </summary>
    private static bool LooksLikePathOrUrl(string text)
    {
        if (text.Length > 200)
            return false;

        bool hasWhitespace = text.Any(char.IsWhiteSpace);
        if (hasWhitespace)
            return false;

        bool hasPathMarker = text.Contains('\\') || text.Contains('/') || text.Contains(':')
            || text.Contains('@') || text.Contains("http");
        return hasPathMarker;
    }

    private static bool IsChinese(char c)
    {
        // CJK 统一汉字 + 扩展A
        return (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF);
    }

    private static bool IsEnglishLetter(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }
}
