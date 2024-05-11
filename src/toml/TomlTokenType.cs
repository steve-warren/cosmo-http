namespace Cosmo.Http;

public enum TomlTokenType : byte
{
    None = 0,
    TableHeader = 1,
    Key = 2,
    Value = 3,
    Array = 11,
    Comment = 14,
    Table = 15
}
