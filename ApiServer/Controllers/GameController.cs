﻿using System.Collections.Concurrent;
using ApiServer.Hubs;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;


namespace ApiServer.Controllers;

public class GameController : IDisposable
{
    private readonly ILogger<GameController> _logger;
    public event EventHandler<ServerStatistics>? StatisticsChanged;
    public event EventHandler<DefenseLog>? DefenseLogAdded;
    public event EventHandler<AttackLog>? AttackLogAdded;
    public event EventHandler<List<EnemyClient>>? EnemyClientsChanged;
    public ServerStatistics Statistics { get; set; }
    public readonly IOptions<GameSettings> Options;
    private readonly IHubContext<ClientHub> _clientHub;

    public List<DefenseLog> DefenseLogs { get; private set; }
    public List<AttackLog> AttackLogs { get; private set; }
    public ConcurrentDictionary<string, EnemyClient> EnemyClients { get; } = new();
    public List<string> IpAddresses { get; set; }
    private readonly Random _rng;
    public readonly CancellationToken StoppingToken;
    private bool IsAttackDisabled { get; set; } = false;
    public string DisplayName { get; set; }

    // SemaphoreSlim to lock the critical section
    private readonly SemaphoreSlim _attackLock = new SemaphoreSlim(1, 1);

    public void UpdateStatistics(Action<ServerStatistics> updateAction)
    {
        updateAction(Statistics);
        StatisticsChanged?.Invoke(this, Statistics);
    }

    public void UpdateEnemyClient(KeyValuePair<string, EnemyClient> client)
    {
        EnemyClients[client.Key] = client.Value;
        EnemyClientsChanged?.Invoke(this, EnemyClients.Values.ToList());
    }

    public GameController(IOptions<GameSettings> options, IOptions<List<string>> addressList, IOptions<NameModel> displayName,
        IHostApplicationLifetime lifetime, ILogger<GameController> logger, IHubContext<ClientHub> hub)
    {
        _logger = logger;
        StoppingToken = lifetime.ApplicationStopping;
        Options = options;
        DisplayName = displayName.Value.DisplayName;
        _clientHub = hub;

        _rng = new Random();
        DefenseLogs = new List<DefenseLog>();
        AttackLogs = new List<AttackLog>();
        IpAddresses = addressList.Value;

        StatisticsChanged += async (sender, e) => { await _clientHub.Clients.All.SendAsync("ReceiveStatistics", e, StoppingToken); };

        Statistics = new ServerStatistics
        {
            Points = Options.Value.StartingPoints,
            Attack = Options.Value.StartingAttackValue,
            Defense = Options.Value.StartingDefenseValue,
            State = ServerState.running
        };
        StatisticsChanged.Invoke(this, Statistics);
    }

    // Use SemaphoreSlim to lock the critical section
    public async Task<IResult> ReceiveAttack(HttpContext context, [FromHeader(Name="Attacker")] string Name, AttackModel attackModel)
    {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Name))
            return Results.BadRequest("Name and attack value must be provided");
        if (Statistics.State != ServerState.running)
            return Results.StatusCode(statusCode: 503);

        _logger.LogInformation($"Received attack from {context.Connection.RemoteIpAddress} with attack value {attackModel.Attack}");

        // Acquire the semaphore before processing
        await _attackLock.WaitAsync();

        try
        {
            double newDefenseValue = Statistics.Defense * ((_rng.NextDouble() / 5 - 0.1) + 1);

            if (attackModel.Attack > newDefenseValue)
            {
                _ = AddDefenseLog(context, Name, AttackResult.Hacked, attackModel, (float)newDefenseValue);
                return Results.Ok(new AttackResultModel() { HackingResult = AttackResult.Hacked.ToString() });
            }

            _ = AddDefenseLog(context, Name, AttackResult.Defended, attackModel, (float)newDefenseValue);
            return Results.Ok(new AttackResultModel() { HackingResult = DefenseLogs.Last().Result.ToString() });
        }
        finally
        {
            // Release the semaphore after processing
            _attackLock.Release();
        }
    }

    public async Task AddDefenseLog(HttpContext context, string name, AttackResult result, AttackModel model, float newDefenseValue)
    {
        DefenseLogs.Add(new DefenseLog()
        {
            AttackId = DefenseLogs.Count + 1,
            AttackedByIp = context.Connection.RemoteIpAddress is not null ? context.Connection.RemoteIpAddress.ToString() : "Unknown",
            AttackedByName = name,
            Result = result,
            AttackValue = (float)model.Attack,
            DefenseValue = newDefenseValue
        });
        await _clientHub.Clients.All.SendAsync("ReceiveDefenseLogs", DefenseLogs, StoppingToken);
        DefenseLogAdded?.Invoke(this, DefenseLogs.Last());
    }

    public async Task AddAttackLog(AttackLog log)
    {
        log.AttackId = AttackLogs.Count + 1;
        AttackLogs.Add(log);
        await _clientHub.Clients.All.SendAsync("ReceiveAttackLogs", AttackLogs, StoppingToken);
        AttackLogAdded?.Invoke(this, log);
    }

    public void Dispose()
    {
        Statistics.State = ServerState.stopped;
    }
}
