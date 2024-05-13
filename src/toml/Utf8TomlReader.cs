using System.Buffers.Text;
using System.Text;

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

    public readonly TomlTokenType TokenType => _tokenType;

    public readonly bool GetBoolean()
    {
        _ = Utf8Parser.TryParse(ValueSpan, out bool value, out _);

        return value;
    }

    public readonly int GetInt32()
    {
        _ = Utf8Parser.TryParse(ValueSpan, out int value, out _);

        return value;
    }

    public readonly long GetInt64()
    {
        _ = Utf8Parser.TryParse(ValueSpan, out long value, out _);

        return value;
    }

    public readonly string? GetString() => Encoding.UTF8.GetString(ValueSpan);

    public bool Read()
    {
        ValueSpan = default;

        ReadMarker:

        if (_consumed >= _buffer.Length)
            return false;

        switch (_buffer[_consumed])
        {
            case TomlConstants.LineFeed:
                ConsumeNewLine();
                goto ReadMarker;
            case TomlConstants.CarriageReturn:
                ConsumeNewLine();
                goto ReadMarker;
            case TomlConstants.OpenBracket:
                ConsumeTableHeader();
                return true;
            case TomlConstants.Hash:
                ConsumeComment();
                return true;
            case >= TomlConstants.AsciiLowercaseA
            and <= TomlConstants.AsciiLowercaseZ:
            case >= TomlConstants.AsciiUppercaseA
            and <= TomlConstants.AsciiUppercaseZ:
            case >= TomlConstants.AsciiZero
            and <= TomlConstants.AsciiNine:
            case TomlConstants.Underscore:
            case TomlConstants.Dash:
            case TomlConstants.Quote:
                ConsumeKey();
                return true;
            case TomlConstants.KeyValueSeparator:
                ConsumeValue();
                return true;
        }

        return false;
    }

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
        var clrfIndex = slice.IndexOfAny(TomlConstants.CarriageReturn, TomlConstants.LineFeed);

        var valueSlice = clrfIndex != -1 ? slice[..clrfIndex] : throw new TomlException();

        if (valueSlice[0] == TomlConstants.Quote && valueSlice[^1] == TomlConstants.Quote)
            ValueSpan = valueSlice[1..^1];
        else
            ValueSpan = valueSlice;

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
        var clrfIndex = slice.IndexOfAny(TomlConstants.CarriageReturn, TomlConstants.LineFeed);

        ValueSpan = clrfIndex != -1 ? slice[..clrfIndex] : throw new TomlException();

        _consumed += clrfIndex + 1;
    }

    private static int ClampStart(ReadOnlySpan<byte> span, byte marker)
    {
        for (var start = 0; start < span.Length; start++)
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
