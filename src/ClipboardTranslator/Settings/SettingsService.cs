using System.Text.Json;

namespace ClipboardTranslator.Settings;

public static class SettingsService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClipboardTranslator");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string ConfigFilePath => ConfigPath;

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new AppSettings();

            string json = File.ReadAllText(ConfigPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            // 迁移：兼容旧版明文 apiKey 字段（V1.0 阶段1 遗留）
            if (string.IsNullOrEmpty(settings.ApiKeyEncrypted))
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("apiKey", out var legacy) && legacy.ValueKind == JsonValueKind.String)
                {
                    string legacyKey = legacy.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(legacyKey))
                    {
                        settings.ApiKey = legacyKey;
                        settings.ApiKeyEncrypted = SecretStorageService.Encrypt(legacyKey);
                        Save(settings);
                    }
                }
            }

            if (!string.IsNullOrEmpty(settings.ApiKeyEncrypted))
            {
                settings.ApiKey = SecretStorageService.Decrypt(settings.ApiKeyEncrypted);
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        // 明文 Key 仅用于进程内，落盘前加密；空 Key 时清空加密字段
        if (!string.IsNullOrEmpty(settings.ApiKey))
            settings.ApiKeyEncrypted = SecretStorageService.Encrypt(settings.ApiKey);
        else
            settings.ApiKeyEncrypted = null;

        Directory.CreateDirectory(ConfigDir);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
