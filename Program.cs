using Cosmo.Http;

using var cts = new CancellationTokenSource();
var hostCancellationToken = cts.Token;

var contentCache = new StaticFileContentCache("wwwroot/");
await contentCache.LoadCacheAsync();

var routes = new Dictionary<string, Action<HttpRequest, HttpResponse>>
{
    {
        "/",
        (request, response) =>
        {
            response.Content = "hello, world!"u8.ToArray();
            response.ContentType = "text/plain";
        }
    },
    {
        "/status",
        (request, response) =>
        {
            response.Content = """{"here" : ["is", "some", "json"]}"""u8.ToArray();
            response.ContentType = "application/json";
        }
    },
    {
        "/index.html",
        (request, response) =>
        {
            response.Content =
                "<html><head><title>index.html</title><body><h1>hello, world!</body></html>"u8.ToArray();
            response.ContentType = "text/html";
        }
    },
    {
        "/*",
        (request, response) =>
        {
            contentCache.TryGet(request.Uri[1..], out var entry);

            response.Content = entry.Content ?? [];
            response.ContentType = entry.ContentType ?? "";
        }
    }
};

var server = new HttpServer(endpoint: "127.0.0.1", 8080, routes);

await server.RunAsync();
