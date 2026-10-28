using System.Threading;

namespace MusicDesktop.Core;

public sealed class SingleInstance : IDisposable
{
    private const string MutexName = "Global\\MusicDesktop.SingleInstance.v1";
    private readonly Mutex _mutex;
    public bool IsFirst { get; }

    public SingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out var created);
        IsFirst = created;
    }

    public void Dispose()
    {
        try { if (IsFirst) _mutex.ReleaseMutex(); } catch { }
        _mutex.Dispose();
    }
}
