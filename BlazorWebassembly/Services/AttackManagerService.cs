using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Hosting;

namespace BlazorWebassembly.Services;

public class AttackManagerService
{
    public event EventHandler<AttackLog>? AttackLogAdded;
    public List<AttackLog> AttackLogs { get; set; } = new List<AttackLog>();
    public ServerStatistics Statistics { get; set; }
    public AttackStrategy Strategy { get; set; } = AttackStrategy.AnyButLoss;

    public List<EnemyClient> OtherClients { get; set; } = new();
    public ConcurrentDictionary<EnemyClient, TargetType> TargetClasification { get; set; } = new();

    public CancellationTokenSource StoppingToken { get; set; }
    public AttackManagerService()
    {
        StoppingToken = new CancellationTokenSource();
        AnalyzeEnemies();
    }

    public void UpdateStrategy(AttackStrategy strategy)
    {
        Strategy = strategy;
    }

    public void UpdateStatistics(ServerStatistics statistics)
    {
        Statistics = statistics;
    }

    public void AddAttackLog(AttackLog log)
    {
        AttackLogs.Add(log);
        AttackLogAdded?.Invoke(this, log);
    }

    public void UpdateEnemyClients(List<EnemyClient> clients)
    {
        OtherClients = clients;
        AnalyzeEnemies();
    }

    public void AnalyzeEnemies()
    {
        if (OtherClients.Count is 0)
            return;
        TargetClasification = new ConcurrentDictionary<EnemyClient, TargetType>();
        foreach (var client in OtherClients)
        {
            TargetType finalTargetType = TargetType.None;
            if (client.Defense * 1.1 < Statistics.Attack * 0.9)
                finalTargetType = TargetType.Win;
            else if(client.Defense * 0.9 > Statistics.Attack * 1.1)
                finalTargetType = TargetType.Loss;
            else if (client.Defense < Statistics.Attack)
                finalTargetType = TargetType.Advantage;
            else if(client.Defense == Statistics.Attack)
                finalTargetType = TargetType.Neutral;
            else if(client.Defense > Statistics.Attack)
                finalTargetType = TargetType.Disadvantage;

            TargetClasification[client] = finalTargetType;
        }
    }
}