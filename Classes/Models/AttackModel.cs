using System.Text.Json.Serialization;

namespace Classes.Models;

public class AttackModel
{
    [JsonPropertyName("Attack")]
    public double Attack { get; set; }
}