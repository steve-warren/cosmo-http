namespace Cosmo.Http;

internal sealed class HttpServerConfiguration
{
    public string? IpAddress { get; set; }
    public int? Port { get; set; }

    public Dictionary<string, string> MimeTypes { get; set; } = [];
}

internal static class ConfigurationProvider
{
    private static Dictionary<string, Dictionary<string, string>> _config = [];

    public static Dictionary<string, string> GetConfigurationSection(string name)
    {
        if (_config.TryGetValue(name, out var section))
            return section;

        return [];
    }

    public static void Load()
    {
        // no need for async here
        var toml = File.ReadAllBytes("host.toml").AsSpan();

        var reader = new Utf8TomlReader(toml);

        var config = new Dictionary<string, Dictionary<string, string>>();

        Dictionary<string, string> table = [];
        string key = string.Empty;

        while(reader.Read())
        {
            if (reader.TokenType == TomlTokenType.TableHeader)
            {
                table = [];
                config.Add(reader.GetString()
                ?? throw new InvalidOperationException(), table);
            }

            else if (reader.TokenType == TomlTokenType.Key)
            {
                key = reader.GetString()!;
                table.Add(key, string.Empty);
            }

            else if (reader.TokenType == TomlTokenType.Value)
                table[key] = reader.GetString()!;
        }

        _config = config;
    }
}
