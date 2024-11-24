﻿using System.Collections.Concurrent;
using System.Text.Json;
using ApiServer.Controllers;
using ApiServer.Hubs;
using Classes.Enums;
using Classes.Models;
using Classes.Statistics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace ApiServer.Services;

public class UpdateEnemyClientsService(GameController controller, ILogger<UpdateEnemyClientsService> logger, IHubContext<ClientHub> hub)
    : BackgroundService
{
    public HttpClient Client { get; } = new HttpClient();
    public int Crashes { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var address in controller.IpAddresses)
                {
                    var response = await Client.GetAsync($"http://{address}:1337/status", stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var enemyStatus = await response.Content.ReadFromJsonAsync<EnemyStatusModel>(stoppingToken);

                        if (enemyStatus != null)
                        {
                            EnemyClient client = new EnemyClient()
                            {
                                IpAddress = address,
                                Points = enemyStatus.Points,
                                Attack = enemyStatus.Attack,
                                Defense = enemyStatus.Defense,
                                State = Enum.Parse<ServerState>(enemyStatus.State)
                            };
                            controller.EnemyClients[client.IpAddress] = client; // Update client in dictionary
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Task was cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while updating enemy clients");
                Crashes++;
                if (Crashes >= 3)
                {
                    logger.LogCritical("Too many crashes. Stopping service.");
                    break;
                }
            }
            // logger.LogInformation("Enemy clients updated successfully.");
            await hub.Clients.All.SendAsync("UpdateEnemyClients", controller.EnemyClients.Values.ToList(), stoppingToken);
            await Task.Delay(3000, stoppingToken);
        }
    }
}