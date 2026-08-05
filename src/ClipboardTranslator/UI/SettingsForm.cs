using System.Net.Http.Headers;
using System.Text.Json;
using ClipboardTranslator.Api;
using ClipboardTranslator.Hotkeys;
using ClipboardTranslator.Settings;

namespace ClipboardTranslator.UI;

/// <summary>
/// 设置窗口：BaseURL / API Key / 模型 / 热键 / 超时 / 最大长度 / 阈值 / 测试连接。
/// 每个分组为"标签在上、输入框在下"的纵向排布；保存时仅保存并提示，不关闭窗口。
/// </summary>
public sealed class SettingsForm : Form
{
    private const int Margin = 28;
    private const int FieldWidth = 480;
    private const int LabelHeight = 24;
    private const int ControlHeight = 30;
    private const int GroupGap = 26;

    private readonly AppSettings _settings;
    private readonly Action _saveCallback;
    private readonly TextBox _txtBaseUrl;
    private readonly TextBox _txtApiKey;
    private readonly CheckBox _chkShowKey;
    private readonly TextBox _txtModel;
    private readonly HotkeyTextBox _hotkeyBox;
    private readonly NumericUpDown _numTimeout;
    private readonly NumericUpDown _numMaxChars;
    private readonly NumericUpDown _numThreshold;
    private readonly Button _btnTest;
    private readonly Label _lblTestResult;
    private readonly Label _lblSaveStatus;

    public SettingsForm(AppSettings settings, Action saveCallback)
    {
        _settings = settings;
        _saveCallback = saveCallback;
        Text = "ClipboardTranslator 设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 10f);
        ClientSize = new Size(Margin + FieldWidth + Margin, 900);
        AutoScaleMode = AutoScaleMode.Dpi;

        int y = Margin;

        y = AddField("LLM 服务地址（Base URL）", y, settings.BaseUrl, out var baseBox);
        _txtBaseUrl = baseBox;
        y += 8;
        y = AddHint("示例：https://api.deepseek.com", y);
        y += GroupGap;

        y = AddField("API Key", y, settings.ApiKey ?? string.Empty, out var keyBox, password: true);
        _txtApiKey = keyBox;
        y += 8;
        _chkShowKey = new CheckBox { Text = "显示 API Key", AutoSize = true, Location = new Point(Margin, y), Padding = new Padding(2, 6, 0, 0) };
        _chkShowKey.CheckedChanged += (_, _) => _txtApiKey.UseSystemPasswordChar = !_chkShowKey.Checked;
        Controls.Add(_chkShowKey);
        y += 40;
        y += GroupGap;

        y = AddField("模型名称（Model）", y, settings.Model, out var modelBox);
        _txtModel = modelBox;
        y += 8;
        y = AddHint("示例：deepseek-chat", y);
        y += GroupGap;

        // 热键
        y = AddLabel("翻译热键", y);
        _hotkeyBox = new HotkeyTextBox { Location = new Point(Margin, y), Width = FieldWidth, Height = ControlHeight };
        _hotkeyBox.SetValue(settings.Hotkey);
        Controls.Add(_hotkeyBox);
        y += 8;
        y = AddHint("点击后按下组合键录入，需含至少一个修饰键（Ctrl/Alt/Shift）。", y);
        y += GroupGap;

        // 超时
        y = AddLabel("请求超时（秒）", y);
        _numTimeout = new NumericUpDown { Location = new Point(Margin, y), Width = 140, Height = ControlHeight, Minimum = 1, Maximum = 120, Value = settings.TimeoutSeconds };
        Controls.Add(_numTimeout);
        y += ControlHeight;
        y += GroupGap;

        // 最大长度
        y = AddLabel("最大文本长度（字符）", y);
        _numMaxChars = new NumericUpDown { Location = new Point(Margin, y), Width = 160, Height = ControlHeight, Minimum = 1000, Maximum = 20000, Increment = 500, Value = settings.MaxCharacters };
        Controls.Add(_numMaxChars);
        y += ControlHeight;
        y += GroupGap;

        // 中文阈值
        y = AddLabel("中文识别阈值（百分比 %）", y);
        _numThreshold = new NumericUpDown { Location = new Point(Margin, y), Width = 120, Height = ControlHeight, Minimum = 1, Maximum = 100, Value = (decimal)(settings.ChineseDetectionThreshold * 100) };
        Controls.Add(_numThreshold);
        y += ControlHeight;
        y += GroupGap;

        // 测试连接
        y = AddLabel("测试连接", y);
        _btnTest = new Button { Text = "测试连接", Location = new Point(Margin, y), Width = 130, Height = ControlHeight };
        _btnTest.Click += (_, _) => _ = TestConnectionAsync();
        Controls.Add(_btnTest);
        _lblTestResult = new Label { Text = "", Location = new Point(Margin + 145, y + 4), AutoSize = true, ForeColor = Color.Gray };
        Controls.Add(_lblTestResult);
        y += ControlHeight;
        y += GroupGap;

        // 保存
        _lblSaveStatus = new Label { Text = "", Location = new Point(Margin, y + 4), AutoSize = true, ForeColor = Color.Green };
        Controls.Add(_lblSaveStatus);
        var btnSave = new Button { Text = "保存", Location = new Point(Margin + 150, y), Width = 130, Height = 36 };
        btnSave.Click += (_, _) => OnSave();
        Controls.Add(btnSave);

        var btnClose = new Button { Text = "关闭", Location = new Point(Margin + 300, y), Width = 130, Height = 36 };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);
        AcceptButton = btnSave;
        CancelButton = btnClose;
        ClientSize = new Size(Margin + FieldWidth + Margin, y + 60);
    }

    private int AddLabel(string text, int y)
    {
        var label = new Label
        {
            Text = text,
            Location = new Point(Margin, y),
            AutoSize = true,
            Height = LabelHeight,
        };
        Controls.Add(label);
        return y + LabelHeight;
    }

    private int AddField(string labelText, int y, string value, out TextBox textBox, bool password = false)
    {
        y = AddLabel(labelText, y);
        textBox = new TextBox { Location = new Point(Margin, y), Width = FieldWidth, Height = ControlHeight, Text = value ?? string.Empty };
        if (password)
            textBox.UseSystemPasswordChar = true;
        Controls.Add(textBox);
        return y + ControlHeight;
    }

    private int AddHint(string text, int y)
    {
        var hint = new Label
        {
            Text = text,
            Location = new Point(Margin, y),
            AutoSize = true,
            MaximumSize = new Size(FieldWidth, 0),
            ForeColor = Color.Gray,
        };
        Controls.Add(hint);
        return y + hint.PreferredHeight;
    }

    private void OnSave()
    {
        if (string.IsNullOrWhiteSpace(_txtBaseUrl.Text))
        {
            MessageBox.Show("请填写 LLM 服务地址。", "ClipboardTranslator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_txtModel.Text))
        {
            MessageBox.Show("请填写模型名称。", "ClipboardTranslator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_hotkeyBox.Value.Ctrl == false && _hotkeyBox.Value.Alt == false && _hotkeyBox.Value.Shift == false)
        {
            MessageBox.Show("热键至少需要一个修饰键（Ctrl/Alt/Shift）。", "ClipboardTranslator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _settings.BaseUrl = _txtBaseUrl.Text.Trim();
        _settings.ApiKey = _txtApiKey.Text; // 明文仅在进程内，落盘时加密
        _settings.Model = _txtModel.Text.Trim();
        _settings.Hotkey = _hotkeyBox.Value;
        _settings.TimeoutSeconds = (int)_numTimeout.Value;
        _settings.MaxCharacters = (int)_numMaxChars.Value;
        _settings.ChineseDetectionThreshold = (double)_numThreshold.Value / 100.0;

        _saveCallback();

        _lblSaveStatus.Text = $"已保存：{DateTime.Now:HH:mm:ss}";
    }

    private async Task TestConnectionAsync()
    {
        _btnTest.Enabled = false;
        _lblTestResult.Text = "测试中…";
        _lblTestResult.ForeColor = Color.Gray;

        try
        {
            string baseUrl = _txtBaseUrl.Text.Trim();
            string apiKey = _txtApiKey.Text;
            string model = _txtModel.Text.Trim();
            int timeout = (int)_numTimeout.Value;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeout) };
            var request = new ChatCompletionRequest
            {
                Model = model,
                Messages = new List<ChatMessage> { new ChatMessage { Role = "user", Content = "请回复 OK" } },
                Temperature = 0,
            };
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + "/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

            using var response = await http.SendAsync(httpRequest);
            int status = (int)response.StatusCode;

            if (status == 200)
            {
                _lblTestResult.Text = "连接成功";
                _lblTestResult.ForeColor = Color.Green;
            }
            else
            {
                _lblTestResult.Text = status switch
                {
                    400 => "400：请求参数错误",
                    401 => "401：API Key 无效",
                    403 => "403：请求被拒绝",
                    429 => "429：请求过于频繁",
                    _ => $"HTTP {status}",
                };
                _lblTestResult.ForeColor = Color.Red;
            }
        }
        catch (TaskCanceledException)
        {
            _lblTestResult.Text = "请求超时";
            _lblTestResult.ForeColor = Color.Red;
        }
        catch (Exception)
        {
            _lblTestResult.Text = "连接失败";
            _lblTestResult.ForeColor = Color.Red;
        }
        finally
        {
            _btnTest.Enabled = true;
        }
    }
}
