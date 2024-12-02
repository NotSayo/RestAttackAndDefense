using System.Collections.Concurrent;
using System.Net;
using ApiServer.Controllers;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;

namespace ApiServer.Services;

public class AttackLauncher(AttackManagerService attackManagerService, GameController controller,
    IHostApplicationLifetime lifetime, ILogger<AttackLauncher> logger) : BackgroundService
{
    private List<string> Tasks = new List<string>();
    private readonly Random _rng = new Random();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken = controller.StoppingToken;
        controller.Statistics.State = ServerState.disabled;
        await Task.Delay(100, stoppingToken);
        controller.Statistics.State = ServerState.running;
        logger.LogInformation("Attacker Started");

        while (!stoppingToken.IsCancellationRequested)
        {
            while(controller.Statistics.State != ServerState.running || attackManagerService.Strategy == AttackStrategy.WaitItOut)
            {
            }

            var availableHosts = attackManagerService.TargetClasification.Where(s =>  s.Key.State == ServerState.running).ToList();
            if (attackManagerService.Strategy == AttackStrategy.WinOnly)
            {
                var targets = availableHosts.Where(s => (int) s.Value >= (int)TargetType.Win);
                foreach (var myTarget in targets)
                {
                    if (Tasks.Contains(myTarget.Key.IpAddress))
                        continue;
                    Tasks.Add(myTarget.Key.IpAddress);
                    _ = LaunchAttack(myTarget.Key, stoppingToken);
                }
            }

            if (attackManagerService.Strategy == AttackStrategy.WinOrAdvantage)
            {
                var targets = availableHosts.Where(s => (int) s.Value >= (int)TargetType.Advantage);
                foreach (var myTarget in targets)
                {
                    if (Tasks.Contains(myTarget.Key.IpAddress))
                        continue;
                    Tasks.Add(myTarget.Key.IpAddress);
                    _ = LaunchAttack(myTarget.Key, stoppingToken);
                }
            }

            if (attackManagerService.Strategy == AttackStrategy.UpToNeutral)
            {
                var targets = availableHosts.Where(s => (int) s.Value >= (int)TargetType.Neutral);
                foreach (var myTarget in targets)
                {
                    if (Tasks.Contains(myTarget.Key.IpAddress))
                        continue;
                    Tasks.Add(myTarget.Key.IpAddress);
                    _ = LaunchAttack(myTarget.Key, stoppingToken);
                }
            }

            if (attackManagerService.Strategy == AttackStrategy.AnyButLoss)
            {
                var targets = availableHosts.Where(s => (int) s.Value >= (int)TargetType.Disadvantage);
                foreach (var myTarget in targets)
                {
                    if (Tasks.Contains(myTarget.Key.IpAddress))
                        continue;
                    Tasks.Add(myTarget.Key.IpAddress);
                    _ = LaunchAttack(myTarget.Key, stoppingToken);
                }
            }
        }
    }

    private async Task LaunchAttack(EnemyClient client, CancellationToken stoppingToken)
    {
        using HttpClient _client = new HttpClient();
        var attackValue = controller.Statistics.Attack * ((_rng.NextDouble() / 5 - 0.1) + 1);
        var request = new HttpRequestMessage(HttpMethod.Post, $"http://{client.IpAddress}:1337/hacking-attempt")
        {
            Content = JsonContent.Create(new LaunchAttackModel() { Attack = attackValue })
        };
        request.Headers.Add("Attacker", controller.DisplayName);
        HttpResponseMessage response;
        try
        {
             response = await _client.SendAsync(request, stoppingToken);
        } catch (HttpRequestException)
        {
            attackManagerService.TargetClasification[client] = TargetType.None;
            return;
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<AttackResultModel>(cancellationToken: stoppingToken);
            AttackLog log = new AttackLog()
            {
                AttackedIp = client.IpAddress,
                AttackValue = attackValue,
                Result = result!.HackingResult == "Hacked" ? AttackResult.Hacked : AttackResult.Defended
            };
            logger.LogInformation($"Attack on {client.IpAddress} was {log.Result.ToString()}");
            attackManagerService.AddAttackLog(log);
        }
        attackManagerService.TargetClasification[client] = TargetType.None;
        Tasks.Remove(client.IpAddress);
    }

    public override void Dispose()
    {
    }

}