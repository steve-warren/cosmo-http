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
}

internal class TomlReader { }

internal enum TomlTokenType : byte
{
    None = 0,
    Table = 1,
    Key = 2,
    String = 3,
    Integer = 4,
    Boolean = 5
}
