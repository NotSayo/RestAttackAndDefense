using ApiServer.Controllers;
using Classes.Enums;

namespace ApiServer;

public class DisableServerService(IHostApplicationLifetime lifetime, GameController controller, ILogger<DisableServerService> _logger) : BackgroundService
{

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        controller.StatisticsChanged += OnStatisticsChanged;
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

        if (controller.Statistics.Points < e)
        {
            _logger.LogInformation("Defence lost. Disabling server.");
            var t = () => StopServer();
            _ = t.Invoke();
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