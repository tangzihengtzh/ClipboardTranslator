namespace ClipboardTranslator.Hotkeys;

public sealed class HotkeyConfig
{
    public bool Ctrl { get; set; } = true;
    public bool Alt { get; set; } = true;
    public bool Shift { get; set; } = true;
    public uint Key { get; set; } = 0x54; // 默认 'T'

    public uint ToModifiers()
    {
        uint mods = NativeMethods.MOD_NOREPEAT;
        if (Ctrl) mods |= NativeMethods.MOD_CONTROL;
        if (Alt) mods |= NativeMethods.MOD_ALT;
        if (Shift) mods |= NativeMethods.MOD_SHIFT;
        return mods;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(DescribeKey(Key));
        return string.Join(" + ", parts);
    }

    private static string DescribeKey(uint vk)
    {
        if (vk >= 'A' && vk <= 'Z') return ((char)vk).ToString();
        if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
        if (vk >= 0x70 && vk <= 0x87) return $"F{vk - 0x6F}"; // F1..F24
        if (vk == 0x20) return "Space";
        if (vk == 0x6B) return "+";
        if (vk == 0x6D) return "-";
        if (vk == 0xBC) return ",";
        if (vk == 0xBE) return ".";
        if (vk == 0xBF) return "/";
        if (vk == 0xBA) return ";";
        if (vk == 0xDE) return "'";
        if (vk == 0xDB) return "[";
        if (vk == 0xDD) return "]";
        if (vk == 0xDC) return "\\";
        if (vk == 0xBD) return "-";
        if (vk == 0xBB) return "=";
        if (vk == 0x27) return "`";
        if (vk == 0x09) return "Tab";
        if (vk == 0x1B) return "Esc";
        if (vk == 0x2D) return "Insert";
        if (vk == 0x2E) return "Delete";
        if (vk == 0x21) return "PgUp";
        if (vk == 0x22) return "PgDn";
        if (vk == 0x24) return "Home";
        if (vk == 0x23) return "End";
        return $"0x{vk:X2}";
    }
}
