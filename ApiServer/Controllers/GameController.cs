using System.Collections.Concurrent;
using System.Threading.Channels;
using ApiServer.Hubs;
using Classes;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ApiServer.Controllers;

public class GameController : IDisposable
{
    private ILogger<GameController> _logger;
    public event EventHandler<int>? StatisticsChanged;
    public event EventHandler<DefenceLog>? DefenceLogAdded;
    public event EventHandler<AttackLog>? AttackLogAdded;
    public ServerStatistics Statistics { get; set; }
    public readonly IOptions<GameSettings> Options;
    private readonly IHubContext<ClientHub> _clientHub;

    public List<DefenceLog> DefenceLogs { get; private set; }
    public List<AttackLog> AttackLogs { get; private set; }
    public ConcurrentDictionary<string, EnemyClient> EnemyClients { get; } = new(); // Thread-safe data structure
    public List<string> IpAddresses { get; set; }
    private readonly Random _rng;
    private readonly CancellationToken _stoppingToken;

    public void UpdateStatistics(Action<ServerStatistics> updateAction)
    {
        var previousPoints = Statistics.Points;
        updateAction(Statistics); // Perform updates via the action
        StatisticsChanged?.Invoke(this, previousPoints); // Notify after changes
    }


    public GameController(IOptions<GameSettings> options, IOptions<List<string>> addressList, IHostApplicationLifetime lifetime,
        ILogger<GameController> logger, IHubContext<ClientHub> hub)
    {
        _logger = logger;
        _stoppingToken = lifetime.ApplicationStopping;
        Options = options;
        _clientHub = hub;

        _rng = new Random();
        DefenceLogs = new List<DefenceLog>();
        AttackLogs = new List<AttackLog>();
        IpAddresses = addressList.Value;

        Statistics = new ServerStatistics
        {
            Points = Options.Value.StartingPoints,
            Attack = Options.Value.StartingAttackValue,
            Defense = Options.Value.StartingDefenseValue,
            State = ServerState.running
        };
    }

    public bool IsServiceAvailable() => Statistics.State == ServerState.running;


    // IActionResult => ActionResult
    public async Task<IResult> ReceiveAttack(HttpContext context,string Name, AttackModel attackModel)
    {
        if(string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Name))
            return Results.BadRequest("Name and attack value must be provided");
        if (!IsServiceAvailable())
            return Results.StatusCode(statusCode:503);

        double newDefenceValue = Statistics.Defense * ((_rng.NextDouble() / 5 - 0.1) + 1);

        if (attackModel.Attack > newDefenceValue)
        {
            AddDefenceLog(context, Name, AttackResult.Hacked, attackModel, (float) newDefenceValue);
            return Results.Ok(new AttackResultModel() { AttackResult = AttackResult.Hacked.ToString() });
        }

        AddDefenceLog(context, Name, AttackResult.Defended, attackModel, (float) newDefenceValue);

        return Results.Ok(new AttackResultModel() {AttackResult = AttackResult.Defended.ToString()});
    }

    public void AddDefenceLog(HttpContext context, string name, AttackResult result, AttackModel model,
        float newDefenceValue)
    {
        DefenceLogs.Add(new DefenceLog()
        {
            AttackId = DefenceLogs.Count + 1,
            AttackedByIp = context.Connection.RemoteIpAddress is not null ? context.Connection.RemoteIpAddress.ToString() : "Unknown",
            AttackedByName = name,
            Result = AttackResult.Hacked,
            AttackValue = model.Attack,
            DefenceValue = newDefenceValue
        });

        var t = async () => await _clientHub.Clients.All.SendAsync("UpdateDefenceLog", DefenceLogs.Last(), _stoppingToken);
        _= t.Invoke();
        DefenceLogAdded?.Invoke(this, DefenceLogs.Last());
    }

    public void AddAttackLog(AttackLog log)
    {
        AttackLogs.Add(log);

        var t = async () => await _clientHub.Clients.All.SendAsync("LogAttackResult", log, _stoppingToken);
        AttackLogAdded?.Invoke(this, log);
    }
    

    public void Dispose()
    {
        Statistics.State = ServerState.stopped;
    }
}