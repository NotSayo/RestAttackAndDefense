using System.ComponentModel;
using System.Runtime.CompilerServices;
using ApiServer.Controllers;
using Classes.Statistics;

namespace ApiServer.Endpoints;

public static class ClientEndpoints
{
    public static void MapClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/defenceLog", (
            GameController controller) =>
        {
            return Results.Ok(controller.DefenceLogs);
        });

        endpoints.MapGet("/attackLog", (
            GameController controller) =>
        {
            return Results.Ok(controller.AttackLogs);
        });

        endpoints.MapGet("/clients", (GameController controller, ILogger<Program> logger) =>
        {
            logger.LogInformation("Retrieving enemy clients.");

            var clients = controller.EnemyClients.Values.ToList(); // Convert to a list
            return Results.Ok(clients);
        });


    }
}