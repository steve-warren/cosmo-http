using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cosmo.Http;

public record HttpRequest(string Verb, string Uri, string Version);

public class HttpResponse
{
    public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];
}

public sealed class HttpServer
{
    private readonly CancellationTokenSource _cts;
    private readonly Dictionary<string, Action<HttpRequest, HttpResponse>> _routes;
    private readonly StaticFileContentCache _contentCache;
    private Task? _runTask;

    public HttpServer(
        string endpoint,
        int port,
        Dictionary<string, Action<HttpRequest, HttpResponse>> routes,
        string staticContentPath,
        Dictionary<string, string> mimeTypes
    )
    {
        _cts = new();
        _routes = routes;

        Endpoint = endpoint;
        Port = port;

        _contentCache = new StaticFileContentCache(staticContentPath, mimeTypes);
    }

    public string Endpoint { get; private set; }
    public int Port { get; private set; }

    public async Task RunAsync()
    {
        var tcs = new TaskCompletionSource();
        _runTask = _runTask is null
            ? tcs.Task
            : throw new InvalidOperationException("Server already running.");

        Console.WriteLine("Starting server...");
        using var listenerSocket = BindListenerSocket(Endpoint, Port);

        var requestHandlerTask = HandleIncomingRequestsAsync(listenerSocket);
        var contentCacheTask = _contentCache.RunAsync(_cts.Token);

        await Task.WhenAll([requestHandlerTask, contentCacheTask])
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        tcs.SetResult();

        Console.WriteLine("Server stopped.");
    }

    public void Shutdown()
    {
        if (_cts.IsCancellationRequested) return;
        Console.WriteLine("\nShutting down...");
        _cts.Cancel();
    }

    public ValueTask ShutdownAndWaitAsync()
    {
        Shutdown();

        return _runTask is null
            ? throw new InvalidOperationException("Server hasn't started.")
            : new ValueTask(_runTask);
    }

    private async Task HandleIncomingRequestsAsync(Socket serverSocket)
    {
        while (true)
        {
            try
            {
                using var requestSocket = await serverSocket.AcceptAsync(_cts.Token).ConfigureAwait(false);

                _ = ReceiveAsync(requestSocket, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static Socket BindListenerSocket(string endpoint, int port)
    {
        const int SOL_SOCKET = 0xffff;        // Socket level
        const int SO_REUSEPORT = 0x0200;      // Platform-specific value

        var localhost = IPAddress.Parse(endpoint);
        var ipEndpoint = new IPEndPoint(localhost, port);

        var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.SetRawSocketOption(SOL_SOCKET, SO_REUSEPORT, BitConverter.GetBytes(1));
        listener.Bind(ipEndpoint);
        listener.Listen();

        Console.WriteLine($"Listening on http://{ipEndpoint.Address}:{ipEndpoint.Port}");

        return listener;
    }

    private async Task ReceiveAsync(Socket requestSocket, CancellationToken cancellationToken)
    {
        try
        {
            requestSocket.NoDelay = true;
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            var memory = buffer.AsMemory(0, 1024);
            
            var received = await requestSocket
                .ReceiveAsync(memory, SocketFlags.None, cancellationToken)
                .ConfigureAwait(false);

            if (received == 0)
                return;

            var httpRequest = ParseRequest(memory[..received].Span);

            //Console.WriteLine($"route {httpRequest.Uri}");

            if (_routes.TryGetValue(httpRequest.Uri, out var uriHandler))
            {
                var httpResponse = new HttpResponse();

                uriHandler(httpRequest, httpResponse);

                var response = Encoding.UTF8.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type:{httpResponse.ContentType}\r\nContent-Length: {httpResponse.Content.Length}\r\n\r\n"
                );

                await requestSocket.SendAsync(response, cancellationToken)
                    .ConfigureAwait(false);
                await requestSocket.SendAsync(httpResponse.Content)
                    .ConfigureAwait(false);

                //Console.WriteLine("200 OK");
            }
            else if (_contentCache.TryGet(httpRequest.Uri[1..],
                         out var cacheEntry))
            {
                var httpResponse = new HttpResponse
                {
                    Content = cacheEntry.Content,
                    ContentType = cacheEntry.ContentType
                };

                var response = Encoding.UTF8.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type:{httpResponse.ContentType}\r\nContent-Length: {httpResponse.Content.Length}\r\n\r\n"
                );

                await requestSocket.SendAsync(response, cancellationToken)
                    .ConfigureAwait(false);
                await requestSocket.SendAsync(httpResponse.Content)
                    .ConfigureAwait(false);

                //Console.WriteLine("200 OK");
            }
            else
            {
                await requestSocket
                    .SendAsync(
                        "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\n"u8
                            .ToArray(),
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                Console.WriteLine("404 Not Found");
            }
        }

        catch (SocketException ex) when (
            ex.SocketErrorCode is SocketError.OperationAborted or SocketError.ConnectionReset or SocketError.Shutdown)
        {
            // normal case during load tests
            return;
        }

        catch (Exception ex)
        {
            Console.WriteLine("Unhandled exception while handling socket: " + ex);
        }
    }

    private static HttpRequest ParseRequest(ReadOnlySpan<byte> requestLineSegment)
    {
        var requestBody = Encoding.UTF8.GetString(requestLineSegment).AsSpan();

        //GET / HTTP/1.1\r\n
        const string crlf = "\r\n";

        var requestLine = requestBody[..requestBody.IndexOf(crlf)];
        var httpVerb = requestLine[..requestLine.IndexOf(' ')];

        var uri = requestLine.Slice(
            requestLine.IndexOf(' ') + 1,
            requestLine.LastIndexOf(' ') - requestLine.IndexOf(' ') - 1
        );

        // todo - avoid string alloc
        return new HttpRequest(new string(httpVerb), new string(uri), "HTTP/1.1");
    }
}
