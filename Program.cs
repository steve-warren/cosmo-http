using System.Net;
using System.Net.Sockets;
using System.Text;

var localhost = IPAddress.Parse("127.0.0.1");
var endpoint = new IPEndPoint(localhost, 8080);

using var cts = new CancellationTokenSource();
var hostCancellationToken = cts.Token;

Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("\nShutting down...");
    cts.Cancel();

    e.Cancel = true;
};

AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    if (!cts.IsCancellationRequested)
        cts.Cancel();
};

using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
socket.Bind(endpoint);
socket.Listen();

Console.WriteLine($"Listening on http://{endpoint.Address}:{endpoint.Port}");

while (!hostCancellationToken.IsCancellationRequested)
{
    try
    {
        var handlerSocket = await socket.AcceptAsync(hostCancellationToken);

        var buffer = new byte[1024];

        var received = await handlerSocket.ReceiveAsync(
            buffer,
            SocketFlags.None,
            hostCancellationToken
        );

        Console.WriteLine(received);

        Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, received));

        await handlerSocket.DisconnectAsync(reuseSocket: true, hostCancellationToken);
    }
    catch (OperationCanceledException) { }
}

Console.WriteLine("Server stopped.");
