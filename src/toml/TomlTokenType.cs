namespace Cosmo.Http;

public enum TomlTokenType : byte
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
    StartTableHeader = 12,
    EndTableHeader = 13,
    Comment = 14,
    Table = 15
}
