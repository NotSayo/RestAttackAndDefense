using ApiServer;
using ApiServer.Controllers;
using ApiServer.Endpoints;
using ApiServer.Hubs;
using ApiServer.Services;
using BlazorWebassembly.Services;
using Classes.Models;
using Classes.Statistics;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400000;
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

builder.Services.AddCors();

builder.Services.AddSingleton<GameController>();
builder.Services.AddSingleton<AttackManagerService>();
builder.Services.AddHostedService<DisableServerService>();
builder.Services.AddHostedService<UpdateEnemyClientsService>();
builder.Services.AddHostedService<AttackLauncher>();

builder.Services.Configure<GameSettings>(builder.Configuration.GetSection("GameSettings"));
builder.Services.Configure<List<string>>(builder.Configuration.GetSection("EnemiesIpAddresses"));
builder.Services.Configure<NameModel>(builder.Configuration.GetSection("DisplayedName"));





var app = builder.Build();

app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true)
    .AllowCredentials());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseResponseCompression();

app.MapGameEndpoints();
app.MapClientEndpoints();
app.MapHub<ClientHub>("/clientHub");

// app.UseHttpsRedirection();



app.Run();

