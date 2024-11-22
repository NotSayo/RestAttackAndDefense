using Classes;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;
using Microsoft.Extensions.Options;

namespace ApiServer.Controllers;

public class GameController : IDisposable
{
    private ILogger<GameController> _logger;
    public event EventHandler<int>? StatisticsChanged;

    public ServerStatistics Statistics { get; set; }
    public readonly IOptions<GameSettings> Options;

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
        _rng = new Random();
        Options = options;
        OtherClients = new List<ServerStatistics>(); // TODO implement auto searching for other clients

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
    public async Task<IResult> ReceiveAttack(string Name, AttackModel attackModel)
    {
        if (!IsServiceAvailable())
            return Results.StatusCode(statusCode:503);

        double newAttackValue = Statistics.Attack * ((_rng.NextDouble() / 5 - 0.1) + 1);

        if (attackModel.Attack > newAttackValue)
        {
            UpdateStatistics((stats) =>
            {
                stats.Points -= Options.Value.PointsLostForUnsuccessfulDefense;
                stats.Defense -= Options.Value.PointsLostForUnsuccessfulDefense;
            });
            return Results.Ok(new AttackResultModel() { AttackResult = AttackResult.Hacked.ToString() });
        }

        UpdateStatistics((stats) =>
        {
            stats.Points += Options.Value.PointsGainedForSuccessfulDefense;
            stats.Defense += Options.Value.PointsGainedForSuccessfulDefense;
        });

        return Results.Ok(new AttackResultModel() {AttackResult = AttackResult.Defended.ToString()});
    }

    public void Dispose()
    {
        Statistics.State = ServerState.stopped;
    }
}