using Classes;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace ApiServer.Controllers;

public class GameController : IDisposable
{
    private ILogger<GameController> _logger;
    public event EventHandler<int>? StatisticsChanged;
    public ServerStatistics Statistics { get; set; }
    public readonly IOptions<GameSettings> Options;

    public List<DefenceLog> DefenceLogs { get; private set; }
    public List<ServerStatistics> OtherClients;
    private readonly Random _rng;
    private readonly CancellationToken _stoppingToken;

    private void UpdateStatistics(Action<ServerStatistics> updateAction)
    {
        var previousPoints = Statistics.Points;
        updateAction(Statistics); // Perform updates via the action
        StatisticsChanged?.Invoke(this, previousPoints); // Notify after changes
    }


    public GameController(IOptions<GameSettings> options, IHostApplicationLifetime lifetime, ILogger<GameController> logger)
    {
        _logger = logger;
        _stoppingToken = lifetime.ApplicationStopping;
        Options = options;

        _rng = new Random();
        OtherClients = new List<ServerStatistics>(); // TODO implement auto searching for other clients
        DefenceLogs = new List<DefenceLog>();

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
            UpdateStatistics((stats) =>
            {
                stats.Points -= Options.Value.PointsLostForUnsuccessfulDefense;
                stats.Defense -= Options.Value.PointsLostForUnsuccessfulDefense;
            });
            return Results.Ok(new AttackResultModel() { AttackResult = AttackResult.Hacked.ToString() });
        }

        AddDefenceLog(context, Name, AttackResult.Defended, attackModel, (float) newDefenceValue);
        UpdateStatistics((stats) =>
        {
            stats.Points += Options.Value.PointsGainedForSuccessfulDefense;
            stats.Defense += Options.Value.PointsGainedForSuccessfulDefense;
        });

        return Results.Ok(new AttackResultModel() {AttackResult = AttackResult.Defended.ToString()});
    }

    private void AddDefenceLog(HttpContext context, string Name, AttackResult result, AttackModel model, float newDefenceValue)
        => DefenceLogs.Add(new DefenceLog()
        {
            AttackId = DefenceLogs.Count + 1,
            AttackedByIp = context.Connection.RemoteIpAddress is not null ? context.Connection.RemoteIpAddress.ToString() : "Unknown",
            AttackedByName = Name,
            Result = AttackResult.Hacked,
            AttackValue = model.Attack,
            DefenceValue = newDefenceValue
        });
    

    public void Dispose()
    {
        Statistics.State = ServerState.stopped;
    }
}