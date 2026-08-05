using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ClipboardTranslator.Resources;

/// <summary>
/// 运行时生成占位托盘图标：深蓝色圆形背景 + 白色双语 "中A" 简写。
/// </summary>
public static class TrayIconFactory
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var bgBrush = new LinearGradientBrush(
                new Rectangle(0, 0, 64, 64),
                Color.FromArgb(0, 120, 215),
                Color.FromArgb(0, 80, 160),
                45f);
            g.FillEllipse(bgBrush, 2, 2, 60, 60);

            using var font = new Font("Segoe UI", 22f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(Color.White);
            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            g.DrawString("中A", font, textBrush, new RectangleF(0, 0, 64, 64), sf);
        }

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(hIcon);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
