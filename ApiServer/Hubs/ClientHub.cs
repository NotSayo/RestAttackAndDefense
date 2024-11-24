using ApiServer.Controllers;
using Classes.Statistics;
using Microsoft.AspNetCore.SignalR;

namespace ApiServer.Hubs;

public class ClientHub(GameController controller, ILogger<ClientHub> _logger) : Hub
{
    public async Task GetStatistics()
    {
        await Clients.Caller.SendAsync("ReceiveStatistics", controller.Statistics);
    }

    public async Task GetDefenceLogs()
    {
        await Clients.Caller.SendAsync("ReceiveDefenceLogs", controller.DefenceLogs);
    }

    public void ReceiveEnemyClients()
    {
        Clients.Caller.SendAsync("UpdateEnemyClients", controller.EnemyClients.Values.ToList());
    }

    public async Task UpdateDefenceLog(DefenceLog newLog)
    {
        await Clients.Caller.SendAsync("UpdateDefenceLog", newLog);
    }

    public void LogAttack(AttackLog log)
    {
        log.AttackId = controller.AttackLogs.Count + 1;
        _logger.LogInformation($"Received attack log: {log.AttackId} : {log.AttackedIp} : {log.Result}");
        controller.AddAttackLog(log);
    }


}