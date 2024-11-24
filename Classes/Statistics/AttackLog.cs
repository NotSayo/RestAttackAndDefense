using Classes.Enums;

namespace Classes.Statistics;

public class AttackLog
{
    public int AttackId { get; set; }
    public required string AttackedIp { get; init; }
    public required AttackResult Result { get; init; }
    public required double AttackValue { get; init; }
}