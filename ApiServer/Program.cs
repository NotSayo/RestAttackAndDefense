using ApiServer;
using ApiServer.Controllers;
using ApiServer.Endpoints;
using ApiServer.Hubs;
using ApiServer.Services;
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
builder.Services.AddHostedService<DisableServerService>();

builder.Services.Configure<GameSettings>(builder.Configuration.GetSection("GameSettings"));



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

