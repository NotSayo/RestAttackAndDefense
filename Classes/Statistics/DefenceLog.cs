﻿using Classes.Enums;

namespace Classes.Statistics;

public class DefenseLog
{
    public required int AttackId { get; init; }
    public required string AttackedByIp { get; init; }
    public required string AttackedByName { get; init; }
    public required AttackResult Result { get; init; }
    public required float AttackValue { get; init; }
    public required float DefenseValue { get; init; }
}