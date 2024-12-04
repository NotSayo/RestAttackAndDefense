using System.Text.Json.Serialization;
using Classes.Enums;

namespace Classes.Models;

public class EnemyStatusModel
{
    [JsonPropertyName("points")]
    public int Points { get; set; }
    [JsonPropertyName("attack")]
    public int Attack { get; set; }
    [JsonPropertyName("defense")]
    public int Defense { get; set; }
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

