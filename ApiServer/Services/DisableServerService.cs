using ApiServer.Controllers;
using Classes.Enums;
using Classes.Statistics;

namespace ApiServer.Services;

public class DisableServerService(IHostApplicationLifetime lifetime, GameController controller, ILogger<DisableServerService> _logger) : BackgroundService
{

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        controller.StatisticsChanged += OnStatisticsChanged;
        controller.DefenceLogAdded += DefenceHandler;
        controller.AttackLogAdded += AttackHandler;
        return Task.CompletedTask;
    }

    private void OnStatisticsChanged(object? sender, int e)
    {
        if (controller.Statistics.Points <= 0)
        {
            _logger.LogCritical("Game over! Server is stopping.");
            controller.Statistics.State = ServerState.stopped;
            return;
        }

    }

    private void AttackHandler(object? sender, AttackLog e)
    {
        if (e.Result == AttackResult.Hacked)
        {
            controller.UpdateStatistics((stats) =>
            {
                stats.Points += controller.Options.Value.PointsGainedForSuccessfulHack;
                stats.Attack += controller.Options.Value.AttackValueGainedForSuccessfulHack;
            });
        }
        else
        {
            controller.UpdateStatistics((stats) =>
            {
                stats.Points -= controller.Options.Value.PointsLostForUnsuccessfulHack;
                stats.Attack -= controller.Options.Value.AttackValueLostForUnsuccessfulHack;
            });
        }
    }

    private void DefenceHandler(object? sender, DefenceLog e)
    {
        if (e.Result == AttackResult.Defended)
        {
            controller.UpdateStatistics((stats) =>
            {
                stats.Points += controller.Options.Value.PointsGainedForSuccessfulDefense;
                stats.Defense += controller.Options.Value.DefenseValueGainedForSuccessfulDefense;
            });
        }
        else
        {
            controller.UpdateStatistics((stats) =>
            {
                stats.Points -= controller.Options.Value.PointsLostForUnsuccessfulDefense;
                stats.Defense -= controller.Options.Value.DefenseValueLostForUnsuccessfulDefense;
            });
        }
    }

    private async Task StopServer()
    {
        controller.Statistics.State = ServerState.disabled;
        await Task.Delay(controller.Options.Value.DisabledStateDurationSeconds * 1000, lifetime.ApplicationStopping);
        controller.Statistics.State = ServerState.running;
        _logger.LogInformation("Server is running again.");
    }

    public override void Dispose()
    {
        controller.Statistics.State = ServerState.stopped;
        base.Dispose();
    }
}