using System.Runtime.InteropServices;

namespace ClipboardTranslator.Clipboard;

using Forms = System.Windows.Forms;

/// <summary>
/// 安全读取剪贴板纯文本。仅在用户触发时调用�?/// </summary>
public sealed class ClipboardService
{
    public enum ReadResult
    {
        Success,
        Empty,
        NotText,
        Unavailable,
    }

    public (ReadResult Result, string? Text) TryGetText()
    {
        const int maxAttempts = 5;
        const int retryDelayMs = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var (result, text) = TryGetTextOnce();
            if (result != ReadResult.Unavailable || attempt == maxAttempts - 1)
                return (result, text);

            Thread.Sleep(retryDelayMs);
        }

        return (ReadResult.Unavailable, null);
    }

    private (ReadResult Result, string? Text) TryGetTextOnce()
    {
        try
        {
            if (!Forms.Clipboard.ContainsText(Forms.TextDataFormat.UnicodeText))
            {
                // 区分空剪贴板与非文本内容
                if (Forms.Clipboard.ContainsData(Forms.DataFormats.Bitmap) ||
                    Forms.Clipboard.ContainsData(Forms.DataFormats.FileDrop) ||
                    Forms.Clipboard.ContainsData(Forms.DataFormats.Html))
                {
                    return (ReadResult.NotText, null);
                }
                return (ReadResult.Empty, null);
            }

            string? text = Forms.Clipboard.GetText(Forms.TextDataFormat.UnicodeText);
            if (string.IsNullOrEmpty(text))
                return (ReadResult.Empty, null);

            return (ReadResult.Success, text);
        }
        catch (ExternalException)
        {
            return (ReadResult.Unavailable, null);
        }
        catch (Exception)
        {
            return (ReadResult.Unavailable, null);
        }
    }

    public bool TrySetText(string text)
    {
        try
        {
            Forms.Clipboard.SetText(text);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
