using Classes.Enums;

namespace Classes.Statistics;

public class AttackLog
{
    public required int AttackId { get; init; }
    public required string AttackedIp { get; init; }
    public required AttackResult Result { get; init; }
    public required float AttackValue { get; init; }
    public required float DefenceValue { get; init; }
}