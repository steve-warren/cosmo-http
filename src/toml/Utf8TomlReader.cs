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

    public readonly TomlTokenType TokenType { get => _tokenType; }

    public bool GetBoolean() { throw new NotImplementedException(); }

    public int GetInt32() => throw new NotImplementedException();

    public long GetInt64() => throw new NotImplementedException();

    public readonly string? GetString() => Encoding.UTF8.GetString(ValueSpan);

    public void Skip() => throw new NotImplementedException();

    public bool Read()
    {
        ValueSpan = default;

        if (_tokenType == TomlTokenType.StartTableHeader)
        {
            ConsumeTableHeader();
            return true;
        }

        else if (_tokenType == TomlTokenType.TableHeader)
        {
            EndTableHeader();
            return true;
        }

        else if (_tokenType == TomlTokenType.EndTableHeader)
        {
            StartTable();
            ConsumeNewLine();
            goto ReadMarker;
        }

        ReadMarker:
        var marker = _buffer[_consumed];

        if (marker == TomlConstants.OpenBracket)
        {
            StartTableHeader();
            return true;
        }

        else if (marker == TomlConstants.Hash)
        {
            StartComment();
            return true;
        }

        else if (marker == TomlConstants.LineFeed)
        {
            ConsumeNewLine();
            goto ReadMarker;
        }

        else if (
            (marker >= 48 && marker <= 57) || 
            (marker >= 65 && marker <= 90) || 
            (marker >= 97 && marker <= 122) ||
            marker == 95 || marker == 45)
        {
            ConsumeKey();
            return true;
        }

        return false;
    }

    public bool ValueTextEquals(string? text)
    {
        return ValueTextEquals(text.AsSpan());
    }

    public bool ValueTextEquals(ReadOnlySpan<char> text) => throw new NotImplementedException();

    public bool ValueTextEquals(ReadOnlySpan<byte> utf8Text) => throw new NotImplementedException();

    public void CopyString(Span<char> destination) => throw new NotImplementedException();

    public void CopyString(Span<byte> utf8Destination) => throw new NotImplementedException();

    private void ConsumeNewLine()
    {
        var index = _buffer[_consumed..].IndexOf(TomlConstants.LineFeed);
        _consumed += index == -1
                    ? 0
                    : index + 1;
        ValueSpan = default;
    }

    private void ConsumeKey()
    {
        throw new NotImplementedException();
    }

    private void ConsumeTableHeader()
    {
        _tokenType = TomlTokenType.TableHeader;
        var slice = _buffer[_consumed..];
        var closedBracketIndex = slice.IndexOf(TomlConstants.ClosedBracket);
        var header = closedBracketIndex != -1
                        ? slice[..closedBracketIndex]
                        : throw new TomlException();

        _consumed += closedBracketIndex - _consumed + 1;
        ValueSpan = header;
    }

    private void StartTableHeader()
    {
        _tokenType = TomlTokenType.StartTableHeader;
        ValueSpan = _buffer.Slice(_consumed, 1);
        _consumed++;
    }

    private void EndTableHeader()
    {
        _tokenType = TomlTokenType.EndTableHeader;
        ValueSpan = _buffer.Slice(_consumed, 1);
        _consumed++;
    }

    private void StartTable()
    {
        _tokenType = TomlTokenType.Table;
    }

    private void StartComment()
    {
        _tokenType = TomlTokenType.Comment;
        ValueSpan = _buffer.Slice(_consumed, 1);
        _consumed++;
    }
}

[Serializable]
internal class TomlException : Exception
{
    public TomlException()
    {
    }

    public TomlException(string? message) : base(message)
    {
    }

    public TomlException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}