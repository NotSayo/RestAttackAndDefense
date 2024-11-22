using ApiServer.Controllers;
using Classes.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Endpoints;

public static class Game
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("status", (
            HttpContext context,
            GameController controller) =>
        {
            return Results.Ok(new
            {
                Points = controller.Statistics.Points,
                Attack = controller.Statistics.Attack,
                Defense = controller.Statistics.Defense,
                State = controller.Statistics.State.ToString()
            });
        });

        endpoints.MapPost("hacking-attempt", (
            GameController controller,
            [FromHeader(Name = "Attacker")] string Name,
            [FromBody] AttackModel AttackValue) => controller.ReceiveAttack(Name, AttackValue));
    }
}