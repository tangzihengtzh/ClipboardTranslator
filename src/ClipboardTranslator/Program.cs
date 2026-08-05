using System.Diagnostics;
using System.Windows.Forms;
using ClipboardTranslator.App;
using ClipboardTranslator.Settings;

namespace ClipboardTranslator;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => MessageBox.Show(
            $"发生未处理的异常，程序继续运行。\n{e.Exception.Message}", "ClipboardTranslator",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                MessageBox.Show($"发生严重异常：\n{ex.Message}", "ClipboardTranslator",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        using var singleInstance = new SingleInstanceManager();
        if (!singleInstance.IsFirstInstance)
        {
            singleInstance.ActivateExisting();
            return;
        }

        var settings = SettingsService.Load();

        Application.Run(new TrayApplicationContext(settings));
    }
}
