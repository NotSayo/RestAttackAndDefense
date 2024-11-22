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

    public async Task GetDefenceLogs()
    {
        await Clients.Caller.SendAsync("ReceiveDefenceLogs", controller.DefenceLogs);
    }

    public async Task UpdateDefenceLog(DefenceLog newLog)
    {
        await Clients.Caller.SendAsync("UpdateDefenceLog", newLog);
    }
}