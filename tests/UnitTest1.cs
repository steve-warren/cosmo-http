using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Cosmo.Http;
using FluentAssertions;

namespace tests;

public class TomlUnitTests
{
    [Fact]
    public void Test1()
    {
        var toml = "[table_name]\r\nkey1 = value1\r\nkey2 = value2\r\n"u8.ToArray();
        var reader = new Utf8TomlReader(toml);

        while (reader.Read())
        {
            var text = reader.GetString();
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}