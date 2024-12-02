using ApiServer.Controllers;
using ApiServer.Services;
using Classes.Enums;
using Classes.Statistics;
using Microsoft.AspNetCore.SignalR;

namespace ApiServer.Hubs;

public class ClientHub(GameController controller, AttackManagerService attackManagerService) : Hub
{
    public async Task GetStatistics()
    {
        await Clients.Caller.SendAsync("ReceiveStatistics", controller.Statistics);
    }

    public async Task GetDefenseLogs()
    {
        await Clients.Caller.SendAsync("ReceiveDefenseLogs", controller.DefenseLogs);
    }

    public async Task GetAttackLogs()
    {
        await Clients.Caller.SendAsync("ReceiveAttackLogs", controller.AttackLogs);
    }

    public async Task GetStrategy()
    {
        await Clients.Caller.SendAsync("ReceiveStrategy", attackManagerService.Strategy);
    }

    public async Task UpdateAttackStrategy(AttackStrategy strategy)
    {
        attackManagerService.Strategy = strategy;
        await Clients.Caller.SendAsync("ReceiveStrategy", strategy);
    }





}