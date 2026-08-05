using System.Text.Json.Serialization;
using ClipboardTranslator.Hotkeys;

namespace ClipboardTranslator.Settings;

public sealed class AppSettings
{
    public string BaseUrl { get; set; } = "https://api.deepseek.com";

    /// <summary>
    /// API Key（内存中的明文，仅存在于进程运行时，不写入配置文件）。
    /// </summary>
    [JsonIgnore]
    public string? ApiKey { get; set; }

    /// <summary>
    /// API Key 的 DPAPI 加密结果（Base64），唯一落盘形式。
    /// </summary>
    [JsonPropertyName("apiKeyEncrypted")]
    public string? ApiKeyEncrypted { get; set; }

    public string Model { get; set; } = "deepseek-chat";
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxCharacters { get; set; } = 8000;
    public double ChineseDetectionThreshold { get; set; } = 0.1;
    public HotkeyConfig Hotkey { get; set; } = new();
    public int DebounceMilliseconds { get; set; } = 300;
    public bool AutoCopyResult { get; set; } = false;
}
