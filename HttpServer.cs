using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

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
    private readonly Channel<Task> _pendingRequests;
    private readonly Dictionary<string, Action<HttpRequest, HttpResponse>> _routes;

    public HttpServer(
        string endpoint,
        int port,
        Dictionary<string, Action<HttpRequest, HttpResponse>> routes
    )
    {
        _cts = new();
        _routes = routes;
        _pendingRequests = Channel.CreateUnbounded<Task>();

        Endpoint = endpoint;
        Port = port;
    }

    public string Endpoint { get; private set; }
    public int Port { get; private set; }

    public async Task RunAsync()
    {
        Console.WriteLine("Starting server...");

        using var serverSocket = Bind(Endpoint, Port);

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            SignalShutdown();
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            SignalShutdown();
        };

        var requestHandlerTask = HandleIncomingRequestsAsync(serverSocket);
        var requestCompletionTask = CompleteIncomingRequestTasksAsync();

        await Task.WhenAll([requestHandlerTask, requestCompletionTask]);

        Console.WriteLine("Server stopped.");
    }

    public void Shutdown() => SignalShutdown();

    private async Task HandleIncomingRequestsAsync(Socket serverSocket)
    {
        var writer = _pendingRequests.Writer;

        while (true)
        {
            try
            {
                var clientSocket = await serverSocket.AcceptAsync(_cts.Token);

                var receiveTask = ReceiveAsync(clientSocket, _cts.Token);

                writer.TryWrite(receiveTask);
            }
            catch (OperationCanceledException)
            {
                writer.Complete();
                break;
            }
            catch (Exception ex)
            {
                writer.Complete(ex);
                throw;
            }
        }
    }

    private async Task CompleteIncomingRequestTasksAsync()
    {
        var reader = _pendingRequests.Reader;

        try
        {
            while (await reader.WaitToReadAsync(_cts.Token))
            {
                if (reader.TryRead(out var receiveTask))
                {
                    if (!receiveTask.IsCompleted)
                        await receiveTask;
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    private static Socket Bind(string endpoint, int port)
    {
        var localhost = IPAddress.Parse(endpoint);
        var ipEndpoint = new IPEndPoint(localhost, port);

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(ipEndpoint);
        socket.Listen();

        Console.WriteLine($"Listening on http://{ipEndpoint.Address}:{ipEndpoint.Port}");

        return socket;
    }

    private async Task ReceiveAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        using (clientSocket)
        {
            var buffer = new byte[1024];

            var received = await clientSocket.ReceiveAsync(
                buffer,
                SocketFlags.None,
                cancellationToken
            );

            if (received == 0)
                return;

            var httpRequest = ParseRequest(new ArraySegment<byte>(buffer, 0, received));
            Console.WriteLine($"route {httpRequest.Uri}");

            if (_routes.TryGetValue(httpRequest.Uri, out var uriHandler))
            {
                var httpResponse = new HttpResponse();

                uriHandler(httpRequest, httpResponse);

                var response = Encoding.UTF8.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type:{httpResponse.ContentType}\r\nContent-Length: {httpResponse.Content.Length}\r\n\r\n"
                );

                await clientSocket.SendAsync(response, cancellationToken);
                await clientSocket.SendAsync(httpResponse.Content);
            }
            else if (_routes.TryGetValue("/*", out var wildcardHandler))
            {
                var httpResponse = new HttpResponse();

                wildcardHandler(httpRequest, httpResponse);

                var response = Encoding.UTF8.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type:{httpResponse.ContentType}\r\nContent-Length: {httpResponse.Content.Length}\r\n\r\n"
                );

                await clientSocket.SendAsync(response, cancellationToken);
                await clientSocket.SendAsync(httpResponse.Content);
            }
            else
            {
                await clientSocket.SendAsync(
                    Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\n"),
                    cancellationToken
                );
            }
        }
    }

    private void SignalShutdown()
    {
        if (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("\nShutting down...");
            _cts.Cancel();
        }
    }

    private static HttpRequest ParseRequest(ArraySegment<byte> requestLineSegment)
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
