using System.Collections.Concurrent;

namespace Cosmo.Http;

internal sealed class StaticFileContentCache
{
    private Dictionary<string, CacheEntry> _cache = [];

    public readonly record struct CacheEntry(byte[] Content, string ContentType);

    public StaticFileContentCache(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Path = path;
    }

    public string Path { get; private set; }

    public async Task LoadCacheAsync()
    {
        var cache = new ConcurrentDictionary<string, CacheEntry>();
        Dictionary<string, string> extensions = new()
        {
            { ".html", "text/html" },
            { ".css", "text/css" },
            { ".js", "text/javascript" },
            { ".jpg", "" },
            { ".gif", "" },
            { ".ico", "image/x-icon" }
        };

        var files = Directory.EnumerateFiles(
            path: Path,
            searchPattern: "*",
            searchOption: SearchOption.AllDirectories
        );

        await Parallel
            .ForEachAsync(
                files,
                async (filePath, cancellationToken) =>
                {
                    var fileInfo = new FileInfo(filePath);

                    if (extensions.TryGetValue(fileInfo.Extension,
                        out var contentType) is false)
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

                    await fileStream.ReadAsync(fileBuffer, cancellationToken);

                    cache.TryAdd(filePath, new CacheEntry(fileBuffer, contentType));
                }
            )
            .ConfigureAwait(false);

        _cache = cache.ToDictionary();
    }

    public bool TryGet(string path, out CacheEntry entry) => _cache.TryGetValue(path, out entry);
}
