namespace Cosmo.Http;

internal ref struct Utf8TomlReader
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

    public readonly TomlTokenType TokenType { get; }

    public bool GetBoolean() { throw new NotImplementedException(); }

    public int GetInt32() => throw new NotImplementedException();

    public long GetInt64() => throw new NotImplementedException();

    public string GetComment() => throw new NotImplementedException();

    public string? GetString() => throw new NotImplementedException();

    public void Skip() => throw new NotImplementedException();

    public bool Read()
    {
        ValueSpan = default;

        var first = _buffer[_consumed];

        if (_tokenType == TomlTokenType.None)
        {
            if (first == TomlConstants.OpenBracket)
            {
                _tokenType = TomlTokenType.StartTable;
                ValueSpan = _buffer.Slice(start: _consumed, length: 1);
                _consumed++;
                return true;
            }
            else if (first == TomlConstants.Hash)
            {
                _tokenType = TomlTokenType.Comment;
                ValueSpan = _buffer.Slice(start: _consumed, length: 1);
                _consumed++;
                return true;
            }

            return false;
        }

        // comments
        if (_tokenType == TomlTokenType.Comment)
        {
            // avoid bound check?
            var lineSeparator = FindLineSeparator(_buffer);

            ValueSpan = _buffer.Slice(_consumed, lineSeparator - 1);

            _consumed += lineSeparator;
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

    private bool ConsumeComment()
    {
        ReadOnlySpan<byte> buffer = _buffer[(_consumed + 1)..];

        return true;
    }

    private static int FindLineSeparator(ReadOnlySpan<byte> buffer)
    {
        return buffer.IndexOfAny(TomlConstants.CarriageReturn, TomlConstants.LineFeed);
    }
}
