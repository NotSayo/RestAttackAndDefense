using System.ComponentModel;
using System.Runtime.CompilerServices;
using ApiServer.Controllers;
using Classes.Statistics;

namespace ApiServer.Endpoints;

public static class ClientEndpoints
{
    public static void MapClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/defenseLog", (
            GameController controller) =>
        {
            return Results.Ok(controller.DefenseLogs);
        });

        endpoints.MapGet("/attackLog", (
            GameController controller) =>
        {
            return Results.Ok(controller.AttackLogs);
        });

        endpoints.MapGet("/clients", (GameController controller) =>
        {
            var clients = controller.EnemyClients.Values.ToList();
            return Results.Ok(clients);
        });
    }
}