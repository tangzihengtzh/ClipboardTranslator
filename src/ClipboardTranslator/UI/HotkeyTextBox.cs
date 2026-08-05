using System.Text;
using System.Windows.Forms;
using ClipboardTranslator.Hotkeys;

namespace ClipboardTranslator.UI;

/// <summary>
/// 热键捕获控件：点击后按下组合键即可录入，显示与实际值一致。
/// </summary>
public sealed class HotkeyTextBox : TextBox
{
    private bool _capturing;

    public HotkeyConfig Value { get; private set; } = new();

    public HotkeyTextBox()
    {
        ReadOnly = true;
        Cursor = Cursors.Hand;
        BackColor = SystemColors.Window;
        TabStop = false;
        TextAlign = HorizontalAlignment.Center;
        UpdateText();
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        if (!_capturing)
        {
            _capturing = true;
            Text = "请按下组合键…";
        }
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        if (_capturing)
        {
            _capturing = false;
            UpdateText();
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_capturing)
            return base.ProcessCmdKey(ref msg, keyData);

        Keys key = keyData & Keys.KeyCode;
        bool ctrl = (keyData & Keys.Control) != 0;
        bool alt = (keyData & Keys.Alt) != 0;
        bool shift = (keyData & Keys.Shift) != 0;

        // 单独按修饰键时不录入
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin
            or Keys.LControlKey or Keys.RControlKey or Keys.LShiftKey or Keys.RShiftKey or Keys.LMenu or Keys.RMenu)
            return true;

        Value = new HotkeyConfig { Ctrl = ctrl, Alt = alt, Shift = shift, Key = (uint)key };
        _capturing = false;
        UpdateText();
        NotifyValueChanged();
        return true;
    }

    public void SetValue(HotkeyConfig config)
    {
        Value = config;
        UpdateText();
    }

    private void UpdateText()
    {
        Text = _capturing ? "请按下组合键…" : Value.ToString();
    }

    private void NotifyValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? ValueChanged;
}
