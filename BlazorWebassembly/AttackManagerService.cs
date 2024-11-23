using System.Threading.Channels;
using Classes.Statistics;

namespace BlazorWebassembly;

public class AttackManagerService(ILogger<AttackManagerService> logger)
{
    public event EventHandler<AttackLog>? AttackLogAdded;

    public ServerStatistics Statistics { get; set; }

    public List<EnemyClient> OtherClients { get; set; }


    public void UpdateEnemyClients(List<EnemyClient> clients)
    {
        OtherClients = clients;
        logger.LogInformation(OtherClients.Count.ToString());
    }

}