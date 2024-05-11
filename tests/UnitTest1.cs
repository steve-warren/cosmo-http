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
        var toml = "# comment\r\n[table_name]\r\nkey1 = value1\r\nkey2 = value2\r\n"u8.ToArray();
        var reader = new Utf8TomlReader(toml);

        int count = 10;

        while (reader.Read()  && count-- > 0)
        {
            var text = reader.GetString();
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}