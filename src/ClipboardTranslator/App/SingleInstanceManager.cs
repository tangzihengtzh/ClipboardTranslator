using System.Threading;

namespace ClipboardTranslator.App;

/// <summary>
/// 单实例控制：同一用户会话内只允许一个实例运行。
/// </summary>
internal sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "ClipboardTranslator_SingleInstance_Mutex";
    private const string EventName = "ClipboardTranslator_SingleInstance_Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activationEvent;

    public bool IsFirstInstance { get; }

    public SingleInstanceManager()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        IsFirstInstance = createdNew;

        if (!createdNew)
        {
            _mutex.Close();
            _mutex = new Mutex(false, MutexName);
        }

        _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
    }

    /// <summary>
    /// 通知已有实例激活（显示托盘提示）。
    /// </summary>
    public void ActivateExisting()
    {
        try
        {
            EventWaitHandle.OpenExisting(EventName).Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
    }

    /// <summary>
    /// 后台线程监听激活信号，用于唤醒已有实例。
    /// </summary>
    public void StartActivationListener(Action onActivated)
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                _activationEvent.WaitOne();
                onActivated();
            }
        })
        { IsBackground = true, Name = "ClipboardTranslator.ActivationListener" };
        thread.Start();
    }

    public void Dispose()
    {
        _activationEvent.Dispose();
        _mutex.Dispose();
    }
}
