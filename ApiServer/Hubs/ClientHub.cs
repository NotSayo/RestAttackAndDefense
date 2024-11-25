using ApiServer.Controllers;
using Classes.Statistics;
using Microsoft.AspNetCore.SignalR;

namespace ApiServer.Hubs;

public class ClientHub(GameController controller) : Hub
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





}