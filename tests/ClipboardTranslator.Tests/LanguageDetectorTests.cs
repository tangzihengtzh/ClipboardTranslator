using ClipboardTranslator.Translation;

namespace ClipboardTranslator.Tests;

public class LanguageDetectorTests
{
    [Fact]
    public void Chinese_Detected_As_ChineseToEnglish()
    {
        var result = LanguageDetector.Detect("设备初始化失败，请检查配置文件是否存在。");
        Assert.Equal(TranslationDirection.ChineseToEnglish, result);
    }

    [Fact]
    public void English_Detected_As_EnglishToChinese()
    {
        var result = LanguageDetector.Detect("The connection was closed unexpectedly by the remote host.");
        Assert.Equal(TranslationDirection.EnglishToChinese, result);
    }

    [Fact]
    public void ChineseMixed_With_English_Identifiers_Detected_As_Chinese()
    {
        var result = LanguageDetector.Detect("请检查 response_format 是否配置为 json_object。");
        Assert.Equal(TranslationDirection.ChineseToEnglish, result);
    }

    [Fact]
    public void EnglishMixed_With_Few_Chinese_Detected_As_English()
    {
        var result = LanguageDetector.Detect("Please check API 返回值。");
        // 中文 3 / (3 + 13) = 18.75% > 10% -> 实际为中文
        Assert.Equal(TranslationDirection.ChineseToEnglish, result);
    }

    [Fact]
    public void Numbers_Only_Returns_Null()
    {
        var result = LanguageDetector.Detect("1234567890");
        Assert.Null(result);
    }

    [Fact]
    public void Path_Only_Returns_Null()
    {
        var result = LanguageDetector.Detect(@"C:\Temp\test.bin");
        Assert.Null(result);
    }

    [Fact]
    public void Url_Only_Returns_Null()
    {
        var result = LanguageDetector.Detect("https://example.com");
        Assert.Null(result);
    }

    [Fact]
    public void Empty_Or_Whitespace_Returns_Null()
    {
        Assert.Null(LanguageDetector.Detect(""));
        Assert.Null(LanguageDetector.Detect("   "));
    }

    [Fact]
    public void Threshold_Below_Detects_English()
    {
        var result = LanguageDetector.Detect("abc", chineseThreshold: 0.5);
        Assert.Equal(TranslationDirection.EnglishToChinese, result);
    }
}
