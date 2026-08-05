using System.Windows.Forms;
using ClipboardTranslator.Api;
using ClipboardTranslator.Clipboard;
using ClipboardTranslator.Hotkeys;
using ClipboardTranslator.Resources;
using ClipboardTranslator.Settings;
using ClipboardTranslator.Translation;

namespace ClipboardTranslator;

/// <summary>
/// 隐藏消息窗口：用于接收全局热键 WM_HOTKEY 消息。
/// </summary>
internal sealed class HiddenHotkeyWindow : Form
{
    public event EventHandler? HotkeyPressed;

    public HiddenHotkeyWindow()
    {
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Minimized;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.WndProc(ref m);
    }
}

/// <summary>
/// 托盘 ApplicationContext：负责生命周期、托盘、热键和翻译流程。
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly NotifyIcon _trayIcon;
    private readonly HiddenHotkeyWindow _hotkeyWindow;
    private readonly HotkeyService _hotkeyService;
    private readonly TranslationService _translationService;
    private readonly ClipboardService _clipboardService;
    private readonly HttpClient _httpClient;

    private readonly ToolStripMenuItem _translateMenu;
    private readonly ToolStripMenuItem _settingsMenu;
    private readonly ToolStripMenuItem _pauseMenu;
    private readonly ToolStripMenuItem _autoCopyMenu;
    private readonly ToolStripMenuItem _aboutMenu;
    private readonly ToolStripMenuItem _exitMenu;

    private bool _hotkeyPaused;
    private bool _isTranslating;
    private DateTime _lastTriggerUtc = DateTime.MinValue;
    private int _requestSeq;
    private CancellationTokenSource? _activeCts;

    public TrayApplicationContext(AppSettings settings)
    {
        _settings = settings;
        _clipboardService = new ClipboardService();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)),
        };
        _translationService = new TranslationService(settings, new DeepSeekClient(_httpClient));

        _hotkeyWindow = new HiddenHotkeyWindow();
        _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

        _hotkeyService = new HotkeyService(_hotkeyWindow.Handle);

        _translateMenu = new ToolStripMenuItem("翻译当前剪贴板");
        _translateMenu.Click += (_, _) => TriggerTranslation();

        _settingsMenu = new ToolStripMenuItem("设置");
        _settingsMenu.Click += (_, _) => OpenSettings();

        _pauseMenu = new ToolStripMenuItem("暂停热键");
        _pauseMenu.Click += (_, _) => TogglePause();

        _autoCopyMenu = new ToolStripMenuItem("自动复制结果");
        _autoCopyMenu.CheckOnClick = true;
        _autoCopyMenu.Checked = settings.AutoCopyResult;
        _autoCopyMenu.CheckedChanged += (_, _) =>
        {
            _settings.AutoCopyResult = _autoCopyMenu.Checked;
            SettingsService.Save(_settings);
        };

        _aboutMenu = new ToolStripMenuItem("关于");
        _aboutMenu.Click += (_, _) => ShowAbout();

        _exitMenu = new ToolStripMenuItem("退出");
        _exitMenu.Click += (_, _) => ExitApplication();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_translateMenu);
        menu.Items.Add(_settingsMenu);
        menu.Items.Add(_pauseMenu);
        menu.Items.Add(_autoCopyMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_aboutMenu);
        menu.Items.Add(_exitMenu);

        _trayIcon = new NotifyIcon
        {
            Icon = TrayIconFactory.Create(),
            Text = "剪贴板中英翻译助手",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.BalloonTipClicked += OnBalloonClicked;

        // 注册热键
        if (!_hotkeyService.TryRegister(settings.Hotkey, out string? error))
        {
            ShowBalloon("热键注册失败", error!, ToolTipIcon.Warning);
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        if (_hotkeyPaused)
            return;

        // 防抖：300ms 内重复触发只处理一次
        var now = DateTime.UtcNow;
        if ((now - _lastTriggerUtc).TotalMilliseconds < _settings.DebounceMilliseconds)
            return;
        _lastTriggerUtc = now;

        TriggerTranslation();
    }

    private void TriggerTranslation()
    {
        if (_isTranslating)
        {
            // 取消旧请求，执行新请求
            _activeCts?.Cancel();
            _activeCts?.Dispose();
        }

        // 剪贴板读取必须在 UI 线程（STA + 消息泵），不可放入后台线程
        var (readResult, text) = _clipboardService.TryGetText();
        switch (readResult)
        {
            case ClipboardService.ReadResult.Empty:
                ShowBalloon("翻译失败", "剪贴板中没有可翻译的文本。", ToolTipIcon.Warning);
                return;
            case ClipboardService.ReadResult.NotText:
                ShowBalloon("翻译失败", "当前剪贴板内容不是文本。", ToolTipIcon.Warning);
                return;
            case ClipboardService.ReadResult.Unavailable:
                ShowBalloon("翻译失败", "剪贴板暂时被其他程序占用，请稍后重试。", ToolTipIcon.Warning);
                return;
        }

        string source = text!;
        _isTranslating = true;
        int seq = ++_requestSeq;
        var cts = new CancellationTokenSource();
        _activeCts = cts;

        var token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                var outcome = await _translationService.TranslateTextAsync(source, token).ConfigureAwait(false);
                if (seq != _requestSeq)
                    return; // 旧请求，丢弃

                if (outcome.Cancelled)
                    return;

                if (!outcome.Success)
                {
                    ShowBalloonOnUi("翻译失败", outcome.ErrorMessage ?? "未知错误", ToolTipIcon.Error);
                    return;
                }

                ShowResultOnUi(outcome.Direction!.Value, outcome.ResultText!);
            }
            catch (Exception)
            {
                ShowBalloonOnUi("翻译失败", "发生未知错误。", ToolTipIcon.Error);
            }
            finally
            {
                if (seq == _requestSeq)
                {
                    _activeCts = null;
                    _isTranslating = false;
                }
                cts.Dispose();
            }
        }, token);
    }

    private void ShowResultOnUi(TranslationDirection direction, string text)
    {
        if (_hotkeyWindow.InvokeRequired)
        {
            _hotkeyWindow.BeginInvoke(() => ShowResultOnUi(direction, text));
            return;
        }

        string title = direction == TranslationDirection.ChineseToEnglish ? "中 → 英" : "英 → 中";

        if (_settings.AutoCopyResult && _clipboardService.TrySetText(text))
        {
            ShowBalloon(title, $"{text}\n（已自动复制）", ToolTipIcon.Info);
            return;
        }

        if (text.Length > 180)
        {
            ShowBalloon(title, $"{text[..180]}…\n翻译完成，共 {text.Length} 个字符。点击通知复制完整结果。", ToolTipIcon.Info);
        }
        else
        {
            ShowBalloon(title, text, ToolTipIcon.Info);
        }
    }

    private void OnBalloonClicked(object? sender, EventArgs e)
    {
        // 长结果在通知中不完整时，此处复制完整结果
        // 阶段1：简单复制；完整结果窗口留待阶段3
        // 由于本阶段通知只显示摘要，点击时复制完整结果到剪贴板
        // （真正需要时由后续阶段实现 ResultForm）
        _trayIcon.ShowBalloonTip(1500);
    }

    private void ShowBalloonOnUi(string title, string message, ToolTipIcon icon)
    {
        if (_hotkeyWindow.InvokeRequired)
        {
            _hotkeyWindow.BeginInvoke(() => ShowBalloon(title, message, icon));
            return;
        }
        ShowBalloon(title, message, icon);
    }

    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        const int maxLength = 255;
        string body = message.Length > maxLength ? message[..maxLength] : message;
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = body;
        _trayIcon.BalloonTipIcon = icon;
        _trayIcon.ShowBalloonTip(4000);
    }

    private void TogglePause()
    {
        _hotkeyPaused = !_hotkeyPaused;
        _pauseMenu.Text = _hotkeyPaused ? "恢复热键" : "暂停热键";
    }

    private HotkeyConfig? _hotkeyBeforeSettings;

    private void OpenSettings()
    {
        _hotkeyBeforeSettings = new HotkeyConfig
        {
            Ctrl = _settings.Hotkey.Ctrl,
            Alt = _settings.Hotkey.Alt,
            Shift = _settings.Hotkey.Shift,
            Key = _settings.Hotkey.Key,
        };
        var form = new UI.SettingsForm(_settings, OnSettingsSaved);
        form.Show();
    }

    /// <summary>
    /// 设置窗口点击保存时回调：热键变更先注销-注册-失败回滚，再持久化。
    /// </summary>
    private void OnSettingsSaved()
    {
        var oldHotkey = _hotkeyBeforeSettings ?? _settings.Hotkey;

        // 热键变更：先注销旧热键，尝试注册新热键，失败则回滚
        bool hotkeyChanged = !HotkeyEqual(oldHotkey, _settings.Hotkey);
        if (hotkeyChanged)
        {
            _hotkeyService.Unregister();
            if (_hotkeyService.TryRegister(_settings.Hotkey, out string? hotkeyError))
            {
                ShowBalloon("热键已更新", $"新热键：{_settings.Hotkey}", ToolTipIcon.Info);
            }
            else
            {
                // 注册失败：恢复旧热键
                _hotkeyService.TryRegister(oldHotkey, out _);
                _settings.Hotkey = oldHotkey;
                ShowBalloon("热键更新失败", hotkeyError!, ToolTipIcon.Warning);
            }
        }

        SettingsService.Save(_settings);
    }

    private static bool HotkeyEqual(HotkeyConfig a, HotkeyConfig b)
    {
        return a.Ctrl == b.Ctrl && a.Alt == b.Alt && a.Shift == b.Shift && a.Key == b.Key;
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "ClipboardTranslator V1.0\n\n" +
            "复制文本后按 Ctrl + Alt + Shift + T 即可翻译。\n" +
            "翻译内容将发送到您配置的 LLM 服务。",
            "关于",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ExitApplication()
    {
        _activeCts?.Cancel();
        _hotkeyService.Unregister();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _httpClient.Dispose();
        _hotkeyWindow.Close();
        _hotkeyWindow.Dispose();
        ExitThread();
    }
}
