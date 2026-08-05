namespace ClipboardTranslator.Hotkeys;

public sealed class HotkeyService
{
    private readonly IntPtr _windowHandle;

    public HotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public bool TryRegister(HotkeyConfig config, out string? errorMessage)
    {
        if (!NativeMethods.RegisterHotKey(_windowHandle, NativeMethods.HOTKEY_ID, config.ToModifiers(), config.Key))
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            errorMessage = error == 1409
                ? $"当前热键组合 {config} 已被其他程序占用。"
                : $"热键 {config} 注册失败（错误码 {error}）。";
            return false;
        }
        errorMessage = null;
        return true;
    }

    public void Unregister()
    {
        NativeMethods.UnregisterHotKey(_windowHandle, NativeMethods.HOTKEY_ID);
    }
}
