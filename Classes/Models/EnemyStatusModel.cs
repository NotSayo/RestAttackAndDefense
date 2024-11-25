using Classes.Enums;

namespace Classes.Models;

public class EnemyStatusModel
{
    public int Points { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public string? State { get; set; }
}

