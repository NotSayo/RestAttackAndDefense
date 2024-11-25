using System.Collections.Concurrent;
using ApiServer.Controllers;
using Classes.Enums;
using Classes.Statistics;

namespace BlazorWebassembly.Services;

public class AttackManagerService
{
    public AttackStrategy Strategy { get; set; } = AttackStrategy.AnyButLoss;

    public ConcurrentDictionary<EnemyClient, TargetType> TargetClasification { get; set; } = new();

    private GameController _controller { get; set; }
    private CancellationToken _stoppingToken { get; set; }

    public AttackManagerService(GameController controller, IHostApplicationLifetime lifetime)
    {
        _controller = controller;
        _stoppingToken = lifetime.ApplicationStopping;
        AnalyzeEnemies();

        controller.EnemyClientsChanged += (sender, e) => AnalyzeEnemies();
    }

    public void UpdateStrategy(AttackStrategy strategy)
    {
        Strategy = strategy;
    }


    public void AddAttackLog(AttackLog log)
    {
        _=_controller.AddAttackLog(log);
    }

    public void AnalyzeEnemies()
    {
        if (_controller.EnemyClients.Count is 0)
            return;
        TargetClasification = new ConcurrentDictionary<EnemyClient, TargetType>();
        foreach (var client in _controller.EnemyClients.Values)
        {
            TargetType finalTargetType = TargetType.None;
            if (client.Defense * 1.1 < _controller.Statistics.Attack * 0.9)
                finalTargetType = TargetType.Win;
            else if(client.Defense * 0.9 > _controller.Statistics.Attack * 1.1)
                finalTargetType = TargetType.Loss;
            else if (client.Defense < _controller.Statistics.Attack)
                finalTargetType = TargetType.Advantage;
            else if(client.Defense == _controller.Statistics.Attack)
                finalTargetType = TargetType.Neutral;
            else if(client.Defense > _controller.Statistics.Attack)
                finalTargetType = TargetType.Disadvantage;

            TargetClasification[client] = finalTargetType;
        }
    }
}