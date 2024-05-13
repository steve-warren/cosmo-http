namespace Cosmo.Http;

internal sealed class HttpServerConfiguration
{
    public string? IpAddress { get; set; }
    public int? Port { get; set; }

    public Dictionary<string, string> MimeTypes { get; set; } = [];
}

internal sealed record ConfigurationSection(string Name, Dictionary<string, string> Config)
{
    public string? GetString(string key)
    {
        return Config[key];
    }

    public int GetInt32(string key)
    {
        return int.Parse(Config[key]);
    }

    public Dictionary<string, string> ToDictionary()
    {
        return Config;
    }
}

internal static class ConfigurationProvider
{
    private static Dictionary<string, ConfigurationSection> _config = [];

    public static ConfigurationSection GetConfigurationSection(string name)
    {
        if (_config.TryGetValue(name, out var section))
            return section;

        return new(string.Empty, []);
    }

    public static void Load()
    {
        // no need for async here
        var toml = File.ReadAllBytes("host.toml").AsSpan();

        var reader = new Utf8TomlReader(toml);

        var config = new Dictionary<string, ConfigurationSection>();

        Dictionary<string, string> table = [];
        string key = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == TomlTokenType.TableHeader)
            {
                table = [];
                config.Add(
                    reader.GetString() ?? throw new InvalidOperationException(),
                    new ConfigurationSection(reader.GetString(), table)
                );
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
