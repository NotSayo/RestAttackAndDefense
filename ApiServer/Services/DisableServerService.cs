using ApiServer.Controllers;
using Classes.Enums;
using Classes.Statistics;

namespace ApiServer.Services;

public class DisableServerService(IHostApplicationLifetime lifetime, GameController controller, ILogger<DisableServerService> _logger) : BackgroundService
{
    private int _attackRewardCount = 1;
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        controller.StatisticsChanged += OnStatisticsChanged;
        controller.DefenseLogAdded += DefenseHandler;
        controller.AttackLogAdded += AttackHandler;
        return Task.CompletedTask;
    }

    private void OnStatisticsChanged(object? sender, ServerStatistics e)
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
        if(controller.Statistics.Points <= 0)
            return;
        if (e.Result == AttackResult.Hacked)
        {
            controller.UpdateStatistics((stats) =>
            {
                stats.Points += controller.Options.Value.PointsGainedForSuccessfulHack;
                stats.Attack += controller.Options.Value.AttackValueGainedForSuccessfulHack;
            });
            if (controller.AttackLogs.Count(s => s.Result == AttackResult.Hacked) >= _attackRewardCount *
                controller.Options.Value.NumberOfSuccessfulHacksForExtraDefense)
            {
                _attackRewardCount++;
                controller.UpdateStatistics((stats) =>
                {
                    stats.Defense += controller.Options.Value.NumberOfDefensePointsGainedForExtraDefense;
                });
            }
        }
        else
        {
            controller.UpdateStatistics((stats) =>
            {
                stats.Points -= controller.Statistics.Points == 0 ? 0 : controller.Options.Value.PointsLostForUnsuccessfulHack;
                stats.Attack -= controller.Statistics.Attack == 0 ? 0 : controller.Options.Value.AttackValueLostForUnsuccessfulHack;
            });
        }
    }

    private void DefenseHandler(object? sender, DefenseLog e)
    {
        if(controller.Statistics.Points <= 0)
            return;
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
                stats.Points -= controller.Statistics.Points == 0 ? 0 : controller.Options.Value.PointsLostForUnsuccessfulDefense;
                stats.Defense -= controller.Statistics.Defense == 0 ? 0 : controller.Options.Value.DefenseValueLostForUnsuccessfulDefense;
            });
            var t =  () => StopServer();
            _=t.Invoke();
        }
    }

    private async Task StopServer()
    {
        controller.UpdateStatistics(stats =>
        {
            stats.State = stats.State == ServerState.stopped ? ServerState.stopped : ServerState.disabled;
        });
        await Task.Delay(controller.Options.Value.DisabledStateDurationSeconds * 1000, lifetime.ApplicationStopping);
        controller.UpdateStatistics(stats =>
        {
            stats.State  = controller.Statistics.State == ServerState.stopped ? ServerState.stopped : ServerState.running;
        });
        if(controller.Statistics.State == ServerState.running)
            _logger.LogInformation("Server is running again.");
        else
            _logger.LogCritical("Server is stopped.");
    }

    public override void Dispose()
    {
        controller.Statistics.State = ServerState.stopped;
        base.Dispose();
    }
}