using Cosmo.Http;

ConfigurationProvider.Load();

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
    }
};

var mimeTypes = ConfigurationProvider.GetConfigurationSection("mime_types");

var server = new HttpServer(
    endpoint: "127.0.0.1",
    8080,
    routes,
    "wwwroot/",
    mimeTypes);

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    server.Shutdown();
};

AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    server.Shutdown();
};

await server.RunAsync();
