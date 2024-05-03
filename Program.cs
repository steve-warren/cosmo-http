using System.Net;
using System.Net.Sockets;
using System.Text;

using var cts = new CancellationTokenSource();
var hostCancellationToken = cts.Token;

var routes = MapRoutes();

var serverSocket = StartServer(endpoint: "127.0.0.1", port: 8080);

while (true)
{
    try
    {
        var clientSocket = await serverSocket.AcceptAsync(hostCancellationToken);

        ReceiveAsync(clientSocket, hostCancellationToken);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.WriteLine("Server stopped.");

async void ReceiveAsync(Socket clientSocket, CancellationToken cancellationToken)
{
    using (clientSocket)
    {
        var buffer = new byte[1024];

        var received = await clientSocket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);

        var httpRequest = ParseRequest(new ArraySegment<byte>(buffer, 0, received));

        if (routes.TryGetValue(httpRequest.Uri, out var responseHandler))
        {
            Console.WriteLine($"route {httpRequest.Uri}");

            var responseBody = Encoding.UTF8.GetBytes(responseHandler());

            var response =
                $"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=UTF-8\r\nContent-Length: {responseBody.Length}\r\n\r\n";

            await clientSocket.SendAsync(Encoding.UTF8.GetBytes(response));
            await clientSocket.SendAsync(responseBody);
        }
        else
            await clientSocket.SendAsync(
                Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\n"),
                cancellationToken
            );

        clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
    }
}

Socket StartServer(string endpoint, int port)
{
    var localhost = IPAddress.Parse(endpoint);
    var ipEndpoint = new IPEndPoint(localhost, port);

    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;

        ShutdownServer();
    };

    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
    {
        ShutdownServer();
    };

    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(ipEndpoint);
    socket.Listen();

    Console.WriteLine($"Listening on http://{ipEndpoint.Address}:{ipEndpoint.Port}");

    return socket;
}

void ShutdownServer()
{
    if (!cts.IsCancellationRequested)
    {
        Console.WriteLine("\nShutting down...");
        cts.Cancel();
    }
}

Dictionary<string, Func<string>> MapRoutes()
{
    var routes = new Dictionary<string, Func<string>>
    {
        { "/", () => "home" },
        { "/about", () => "about" },
        { "/status", () => DateTimeOffset.Now.ToString() }
    };

    return routes;
}

HttpRequest ParseRequest(ArraySegment<byte> requestLineSegment)
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

record HttpRequest(string Verb, string Uri, string Version);
