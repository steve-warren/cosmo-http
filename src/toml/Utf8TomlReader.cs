using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Cosmo.Http;

public ref struct Utf8TomlReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _consumed;
    private TomlTokenType _tokenType;

    public Utf8TomlReader(ReadOnlySpan<byte> tomlData)
    {
        _buffer = tomlData;

        ValueSpan = [];
    }

    public ReadOnlySpan<byte> ValueSpan { get; private set; }

    public readonly TomlTokenType TokenType
    {
        get => _tokenType;
    }

    public readonly bool GetBoolean()
    {
        throw new NotImplementedException();
    }

    public readonly int GetInt32() => throw new NotImplementedException();

    public readonly long GetInt64() => throw new NotImplementedException();

    public readonly string? GetString() => Encoding.UTF8.GetString(ValueSpan);

    public readonly void Skip() => throw new NotImplementedException();

    public bool Read()
    {
        ValueSpan = default;

    ReadMarker:

        if (_consumed >= _buffer.Length)
            return false;

        var marker = _buffer[_consumed];

        if (marker == TomlConstants.OpenBracket)
        {
            ConsumeTableHeader();
            return true;
        }
        else if (marker == TomlConstants.Hash)
        {
            ConsumeComment();
            return true;
        }
        else if (marker == TomlConstants.LineFeed)
        {
            ConsumeNewLine();
            goto ReadMarker;
        }
        else if (marker == TomlConstants.CarriageReturn)
        {
            ConsumeNewLine();
            goto ReadMarker;
        }
        else if (
            (marker >= TomlConstants.AsciiLowercaseA && marker <= TomlConstants.AsciiLowercaseZ)
            || (marker >= TomlConstants.AsciiUppercaseA && marker <= TomlConstants.AsciiUppercaseZ)
            || (marker >= TomlConstants.AsciiZero && marker <= TomlConstants.AsciiNine)
            || marker == TomlConstants.Underscore
            || marker == TomlConstants.Dash
        )
        {
            ConsumeKey();
            return true;
        }
        else if (marker == TomlConstants.KeyValueSeparator)
        {
            ConsumeValue();
            return true;
        }

        return false;
    }

    public readonly bool ValueTextEquals(string? text)
    {
        return ValueTextEquals(text.AsSpan());
    }

    public readonly bool ValueTextEquals(ReadOnlySpan<char> text) => throw new NotImplementedException();

    public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text) => throw new NotImplementedException();

    public void CopyString(Span<char> destination) => throw new NotImplementedException();

    public void CopyString(Span<byte> utf8Destination) => throw new NotImplementedException();

    private void ConsumeNewLine()
    {
        var index = _buffer[_consumed..].IndexOf(TomlConstants.LineFeed);
        _consumed += index == -1 ? 0 : index + 1;
        ValueSpan = default;
    }

    private void ConsumeWhitespace()
    {
        var slice = _buffer[_consumed..];
        var paddingLeftClampIndex = ClampStart(slice, TomlConstants.Space);
        _consumed += paddingLeftClampIndex;
    }

    private void ConsumeKey()
    {
        _tokenType = TomlTokenType.Key;
        var slice = _buffer[_consumed..];
        var spaceIndex = slice.IndexOf(TomlConstants.Space);
        var key = spaceIndex != -1 ? slice[..spaceIndex] : throw new TomlException();

        _consumed += spaceIndex + 1;
        ValueSpan = key;
    }

    private void ConsumeValue()
    {
        _tokenType = TomlTokenType.Value;
        ++_consumed;
        ConsumeWhitespace();

        var slice = _buffer[_consumed..];
        var clrfIndex = slice.IndexOfAny(
            TomlConstants.CarriageReturn,
            TomlConstants.LineFeed);

        ValueSpan = clrfIndex != -1 ? slice[..clrfIndex] : throw new TomlException();

        _consumed += clrfIndex + 1;
    }

    private void ConsumeTableHeader()
    {
        _tokenType = TomlTokenType.TableHeader;
        var slice = _buffer[_consumed..];
        var closedBracketIndex = slice.IndexOf(TomlConstants.ClosedBracket);
        var header =
            closedBracketIndex != -1 ? slice[1..closedBracketIndex] : throw new TomlException();

        _consumed += closedBracketIndex + 1;
        ValueSpan = header;
    }

    private void ConsumeComment()
    {
        _tokenType = TomlTokenType.Comment;

        ++_consumed;

        ConsumeWhitespace();

        var slice = _buffer[_consumed..];
        var clrfIndex = slice.IndexOfAny(
            TomlConstants.CarriageReturn,
            TomlConstants.LineFeed);

        ValueSpan = clrfIndex != -1 ? slice[..clrfIndex] : throw new TomlException();

        _consumed += clrfIndex + 1;
    }

    private static int ClampStart(ReadOnlySpan<byte> span, byte marker)
    {
        for(var start = 0; start < span.Length; start++)
        {
            if (span[start] != marker)
                return start;
        }

        return 0;
    }
}

[Serializable]
internal class TomlException : Exception
{
    public TomlException() { }

    public TomlException(string? message)
        : base(message) { }

    public TomlException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
