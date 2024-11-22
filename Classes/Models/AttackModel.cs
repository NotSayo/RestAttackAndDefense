using System.Text.Json.Serialization;

namespace Classes.Models;

public class AttackModel
{
    [JsonPropertyName("Attack")]
    public float Attack { get; set; }
}