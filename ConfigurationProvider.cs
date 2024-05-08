using System.Text;

namespace Cosmo.Http;

internal sealed class HttpServerConfiguration
{
    public string? IpAddress { get; set; }
    public int? Port { get; set; }

    public Dictionary<string, string> MimeTypes { get; set; } = [];
}

internal static class ConfigurationProvider
{
    public static Task<HttpServerConfiguration> LoadAsync()
    {
        return Task.FromResult<HttpServerConfiguration>(new());
    }

    public static void Load()
    {
        var toml = Encoding.UTF8.GetBytes("#comment 1\r\n#comment 2\r\n");

        var reader = new Utf8TomlReader(toml);

        while(reader.Read())
        {
        }
    }
}
