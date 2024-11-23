using Classes.Enums;

namespace Classes.Statistics;

public class EnemyClient
{
    public required string IpAddress { get; set; }
    public string Port { get; set; } = "1337";
    public required int Points { get; set; }
    public required int Attack { get; set; }
    public required int Defense { get; set; }
    public required ServerState State {get; set;}
}