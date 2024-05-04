using Cosmo.Http;

using var cts = new CancellationTokenSource();
var hostCancellationToken = cts.Token;

var routes = new Dictionary<string, Action<HttpRequest, HttpResponse>>
{
    {
        "/",
        (request, response) =>
        {
            response.Content = "hello, world!";
            response.ContentType = "text/plain";
        }
    },
    {
        "/status",
        (request, response) =>
        {
            response.Content = """{"here" : ["is", "some", "json"]}""";
            response.ContentType = "application/json";
        }
    },
    {
        "/index.html",
        (request, response) =>
        {
            response.Content =
                "<html><head><title>index.html</title><body><h1>hello, world!</body></html>";
            response.ContentType = "text/html";
        }
    }
};

var server = new HttpServer(endpoint: "127.0.0.1", 8080, routes);

await server.RunAsync();
