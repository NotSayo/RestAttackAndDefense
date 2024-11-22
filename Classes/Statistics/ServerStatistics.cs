using Classes.Enums;

namespace Classes.Statistics;

public class ServerStatistics
{
    public required int Points { get; set; }
    public required int Attack { get; set; }
    public required int Defense { get; set; }
    public required ServerState State {get; set;}
}