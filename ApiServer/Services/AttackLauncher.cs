using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using ApiServer.Controllers;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;
using Microsoft.Extensions.Hosting;

namespace BlazorWebassembly.Services;

public class AttackLauncher(AttackManagerService attackManagerService, GameController controller) : BackgroundService
{
    private readonly Random _rng = new Random();
    private readonly HttpClient _client = new HttpClient();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken);
        while (true)
        {
            while(controller.Statistics.State != ServerState.running || attackManagerService.Strategy == AttackStrategy.WaitItOut)
            {
                await Task.Delay(100, stoppingToken);
            }

            var availableHosts = attackManagerService.TargetClasification.Where(s =>  s.Key.State == ServerState.running).ToList();
            var target = availableHosts.FirstOrDefault(s => s.Value == TargetType.Win).Key;
            if (target is not null)
            {
                await LaunchAttack(target);
                await Task.Delay(200, stoppingToken);
                continue;
            }

            if (attackManagerService.Strategy == AttackStrategy.WinOrAdvantage)
            {
                target = availableHosts.FirstOrDefault(s => (int) s.Value >= (int)TargetType.Advantage).Key;
                if (target is not null)
                {
                    await LaunchAttack(target);
                    await Task.Delay(200, stoppingToken);
                    continue;
                }
            }

            if (attackManagerService.Strategy == AttackStrategy.UpToNeutral)
            {
                target = availableHosts.FirstOrDefault(s => (int) s.Value >= (int)TargetType.Neutral).Key;
                if (target is not null)
                {
                    await LaunchAttack(target);
                    await Task.Delay(200, stoppingToken);
                    continue;
                }
            }

            if (attackManagerService.Strategy == AttackStrategy.AnyButLoss)
            {
                target = availableHosts.FirstOrDefault(s => (int) s.Value >= (int)TargetType.Disadvantage).Key;
                if (target is not null)
                {
                    await LaunchAttack(target);
                    await Task.Delay(200, stoppingToken);
                    continue;
                }
            }


            await Task.Delay(500, stoppingToken);

        }
    }

    private async Task LaunchAttack(EnemyClient client)
    {
        var attackValue = controller.Statistics.Attack * ((_rng.NextDouble() / 5 - 0.1) + 1);
        var request = new HttpRequestMessage(HttpMethod.Post, $"http://{client.IpAddress}:1337/hacking-attempt")
        {
            Content = JsonContent.Create(new LaunchAttackModel() { Attack = attackValue })
        };
        request.Headers.Add("Attacker", controller.DisplayName);
        HttpResponseMessage response;
        try
        {
             response = await _client.SendAsync(request);
        } catch (HttpRequestException)
        {
            attackManagerService.TargetClasification[client] = TargetType.None;
            return;
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<AttackResultModel>();
            AttackLog log = new AttackLog()
            {
                AttackedIp = client.IpAddress,
                AttackValue = attackValue,
                Result = result!.HackingResult == "Hacked" ? AttackResult.Hacked : AttackResult.Defended
            };
            attackManagerService.AddAttackLog(log);
        }
        attackManagerService.TargetClasification[client] = TargetType.None;

    }

    public override void Dispose()
    {
        _client.Dispose();
    }

}