using ApiServer;
using ApiServer.Controllers;
using ApiServer.Endpoints;
using Classes.Statistics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<GameController>();
builder.Services.AddHostedService<DisableServerService>();

builder.Services.Configure<GameSettings>(builder.Configuration.GetSection("GameSettings"));



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGameEndpoints();
app.MapClientEndpoints();

app.UseHttpsRedirection();



app.Run();

