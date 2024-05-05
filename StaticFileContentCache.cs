using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Cosmo.Http;

internal sealed class StaticFileContentCache : IAsyncDisposable
{
    private static readonly Dictionary<string, string> _mimeTypes =
        new()
        {
            { ".html", "text/html" },
            { ".css", "text/css" },
            { ".js", "text/javascript" },
            { ".jpg", "" },
            { ".gif", "" },
            { ".ico", "image/x-icon" }
        };

    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly FileSystemMonitor _fsm;
    private readonly Channel<string> _fileAccumulator;
    private Task? _runTask;
    private CancellationTokenSource _cts;
    private bool _disposed;

    public readonly record struct CacheEntry(byte[] Content, string ContentType);

    public StaticFileContentCache(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Path = path;

        _cache = [];
        _fileAccumulator = Channel.CreateUnbounded<string>();
        _fsm = new FileSystemMonitor(path, _fileAccumulator.Writer);
        _cts = new();
    }

    public string Path { get; private set; }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await StopAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _fsm.Dispose();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var combinedToken = CancellationTokenSource
            .CreateLinkedTokenSource(_cts.Token, cancellationToken)
            .Token;
        var tcs = new TaskCompletionSource();
        _runTask = tcs.Task;

        try
        {
            AddFilesFromPathToAccumulator(combinedToken);

            _fsm.Start();

            var reader = _fileAccumulator.Reader;

            while (await reader.WaitToReadAsync(combinedToken).ConfigureAwait(false))
            {
                _ = reader.TryRead(out var path);

                await PutFileIntoCacheAsync(path, combinedToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _fsm.Stop();
            tcs.SetResult();
        }
    }

    private Task StopAsync()
    {
        _cts.Cancel();

        return _runTask is not null ? _runTask : Task.CompletedTask;
    }

    private void AddFilesFromPathToAccumulator(CancellationToken cancellationToken)
    {
        var files = Directory.EnumerateFiles(
            path: Path,
            searchPattern: "*",
            searchOption: SearchOption.AllDirectories
        );

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            _fileAccumulator.Writer.TryWrite(file);
        }
    }

    private async Task PutFileIntoCacheAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);

        if (_mimeTypes.TryGetValue(fileInfo.Extension, out var contentType) is false)
            return;

        var fileBuffer = new byte[fileInfo.Length]; // for now
        await using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
        );

        await fileStream.ReadAsync(fileBuffer, cancellationToken).ConfigureAwait(false);

        _cache.TryAdd(filePath, new CacheEntry(fileBuffer, contentType));
    }

    public bool TryGet(string path, out CacheEntry entry) => _cache.TryGetValue(path, out entry);
}
