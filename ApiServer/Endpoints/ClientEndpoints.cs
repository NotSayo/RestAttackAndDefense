using ApiServer.Controllers;

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
    }
}