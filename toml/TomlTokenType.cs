namespace Cosmo.Http;

internal enum TomlTokenType : byte
{
    None = 0,
    TableHeader = 1,
    Key = 2,
    String = 3,
    Integer = 4,
    Float = 5,
    Boolean = 6,
    OffsetDateTime = 7,
    LocalDateTime = 8,
    LocalDate = 9,
    LocalTime = 10,
    Array = 11,
    StartTable = 12,
    EndTable = 13,
    Comment = 14
}
