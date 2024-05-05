using System.Threading.Channels;

namespace Cosmo.Http;

internal sealed class FileSystemMonitor : IDisposable
{
    private readonly ChannelWriter<string> _accumulator;
    private readonly FileSystemWatcher _fsw;

    public FileSystemMonitor(string path, ChannelWriter<string> accumulator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(accumulator);

        _accumulator = accumulator;
        _fsw = new(path) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite };
    }

    public void Start()
    {
        _fsw.Changed += OnFileSystemNotify;
        _fsw.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _fsw.Changed -= OnFileSystemNotify;
        _fsw.EnableRaisingEvents = false;
    }

    public void Dispose()
    {
        _fsw.Dispose();
    }

    private void OnFileSystemNotify(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed)
            _accumulator.TryWrite(e.FullPath);
    }
}
