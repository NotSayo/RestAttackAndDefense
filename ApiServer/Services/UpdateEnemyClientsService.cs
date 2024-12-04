using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ApiServer.Controllers;
using ApiServer.Hubs;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;

namespace ApiServer.Services;

public class UpdateEnemyClientsService(GameController controller, ILogger<UpdateEnemyClientsService> logger,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    public HttpClient Client { get; } = new HttpClient();
    public int Crashes { get; private set; }

    private ConcurrentDictionary<string, Task> _tasks = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Updater started");

        foreach (var client in controller.IpAddresses)
        {
            _tasks[client] = StartTask(client, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var client in controller.IpAddresses)
            {
                if (_tasks[client].IsCompleted)
                {
                    logger.LogInformation($"Task {client} stopped. Restarting...");
                    _tasks[client] = StartTask(client, stoppingToken);
                }
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    private Task StartTask(string ipAddress, CancellationToken token)
    {
        return Task.Run(async () => await  UpdateClient(ipAddress ,token), token);
    }

    private async Task UpdateClient(string ipAddress, CancellationToken token)
    {
        using HttpClient _client = new HttpClient();
        logger.LogInformation($"Task for {ipAddress} started");
        while (!token.IsCancellationRequested)
        {
             if(controller.Statistics.State == ServerState.stopped)
             {
                 await Task.Delay(2000, token);
                 continue;
             }

             var request = new HttpRequestMessage(HttpMethod.Get, $"http://{ipAddress}:1337/status");
             request.Headers.Add("Attacker", controller.DisplayName);

             HttpResponseMessage? response;
             try
             {
                 response = await _client.SendAsync(request, token);
                 if (response is null)
                     continue;
                 if (response.IsSuccessStatusCode)
                 {
                     var enemyStatus = await response.Content.ReadFromJsonAsync<EnemyStatusModel>(token);

                     if (enemyStatus != null)
                     {
                         EnemyClient client = new EnemyClient()
                         {
                             IpAddress = ipAddress,
                             Points = enemyStatus.Points,
                             Attack = enemyStatus.Attack,
                             Defense = enemyStatus.Defense,
                             State = Enum.Parse<ServerState>((enemyStatus.State ?? "error").ToLower())
                         };
                         controller.UpdateEnemyClient(new(client.IpAddress, client));
                     }
                 }
                 // logger.LogInformation($"Updated {ipAddress}");

             }
             catch (TaskCanceledException ex)
             {
                 logger.LogInformation($"Task was canceled for {ipAddress}");
             }
             catch(ArgumentException ex)
             {
                 logger.LogCritical(ex, "The format of the response is invalid.");
             }
             catch (InvalidOperationException ex)
             {
                 logger.LogCritical(ex, "The format of the response is invalid.");
             }
             catch (HttpRequestException)
             {
                 logger.LogWarning(new StringBuilder().Append($"Could not connect to enemy client: {ipAddress}").ToString());
             }
             catch (Exception ex)
             {
                 logger.LogError(ex, "Error while updating enemy clients");
                 Crashes++;
                 if (Crashes >= 10)
                 {
                     logger.LogCritical("Too many crashes.");
                 }
             }
             await Task.Delay(1000, token);
        }
    }
}
