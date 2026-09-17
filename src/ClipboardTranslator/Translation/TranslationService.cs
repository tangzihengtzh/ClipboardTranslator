using ClipboardTranslator.Api;
using ClipboardTranslator.Clipboard;
using ClipboardTranslator.Settings;

namespace ClipboardTranslator.Translation;

public sealed class TranslationOutcome
{
    public bool Success { get; init; }
    public TranslationDirection? Direction { get; init; }
    public string? ResultText { get; init; }
    public string? ErrorMessage { get; init; }
    public bool Cancelled { get; init; }

    public static TranslationOutcome Ok(TranslationDirection direction, string text) => new()
    {
        Success = true,
        Direction = direction,
        ResultText = text,
    };

    public static TranslationOutcome Fail(string errorMessage) => new()
    {
        ErrorMessage = errorMessage,
    };

    public static TranslationOutcome Cancel() => new()
    {
        Cancelled = true,
    };
}

/// <summary>
/// 串联翻译流程：校验 -> 判断语言 -> 生成提示词 -> 调用 API -> 解析结果。
/// 注意：剪贴板读取必须在 UI 线程（STA）完成，本服务只接收已读取的文本。
/// </summary>
public sealed class TranslationService
{
    private readonly AppSettings _settings;
    private readonly DeepSeekClient _client;

    public TranslationService(AppSettings settings, DeepSeekClient client)
    {
        _settings = settings;
        _client = client;
    }

    public async Task<TranslationOutcome> TranslateTextAsync(string sourceText, CancellationToken cancellationToken)
    {
        string source = sourceText.Trim();
        if (source.Length == 0)
            return TranslationOutcome.Fail("剪贴板中没有可翻译的文本。");

        if (source.Length > _settings.MaxCharacters)
            return TranslationOutcome.Fail($"剪贴板文本过长，当前限制为 {_settings.MaxCharacters} 个字符。");

        var direction = LanguageDetector.Detect(source, _settings.ChineseDetectionThreshold);
        if (direction is null)
            return TranslationOutcome.Fail("未检测到需要翻译的自然语言文本。");

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return TranslationOutcome.Fail("请先配置 DeepSeek API Key。");

        if (string.IsNullOrWhiteSpace(_settings.Model))
            return TranslationOutcome.Fail("请先配置模型名称。");

        var messages = TranslationPromptBuilder.BuildMessages(direction.Value, source);
        var request = new ChatCompletionRequest
        {
            Model = _settings.Model,
            Messages = messages,
            Temperature = 0,
            ResponseFormat = new ResponseFormat { Type = "json_object" },
        };

        string raw = await CallWithRetryAsync(request, cancellationToken).ConfigureAwait(false);

        if (!TranslationJsonParser.TryParse(raw, out string? translated) || string.IsNullOrWhiteSpace(translated))
        {
            // 重试一次，附加更严格的系统提示
            if (!cancellationToken.IsCancellationRequested)
            {
                string strictSystemPrompt = TranslationPromptBuilder.BuildSystemPrompt(direction.Value) +
                    " Return valid JSON only: {\"text\":\"...\"}";
                var strictMessages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "system", Content = strictSystemPrompt },
                    new ChatMessage { Role = "user", Content = source },
                };
                var strictRequest = new ChatCompletionRequest
                {
                    Model = _settings.Model,
                    Messages = strictMessages,
                    Temperature = 0,
                    ResponseFormat = new ResponseFormat { Type = "json_object" },
                };
                raw = await CallWithRetryAsync(strictRequest, cancellationToken).ConfigureAwait(false);
                if (TranslationJsonParser.TryParse(raw, out string? retryTranslated) && !string.IsNullOrWhiteSpace(retryTranslated))
                    translated = retryTranslated;
            }

            if (string.IsNullOrWhiteSpace(translated))
                return TranslationOutcome.Fail("翻译结果格式异常。");
        }

        return TranslationOutcome.Ok(direction.Value, translated.Trim());
    }

    private async Task<string> CallWithRetryAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _client.SendChatAsync(_settings.BaseUrl, _settings.ApiKey!, request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.Retryable && !cancellationToken.IsCancellationRequested)
        {
            // HTTP 5xx 自动重试一次
            return await _client.SendChatAsync(_settings.BaseUrl, _settings.ApiKey!, request, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
