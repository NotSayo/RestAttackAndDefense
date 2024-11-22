using ApiServer.Controllers;
using Classes.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Endpoints;

public static class Game
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("status", (
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
            HttpContext context,
            GameController controller,
            [FromHeader(Name = "Attacker")] string name,
            [FromBody] AttackModel attackValue) => controller.ReceiveAttack(context, name, attackValue));
    }
}