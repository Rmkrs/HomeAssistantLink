using dotenv.net;

using HomeAssistantLink.Clients;
using HomeAssistantLink.Domain;
using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Host.WebApi;
using HomeAssistantLink.Host.WebApi.Models;
using HomeAssistantLink.Infrastructure;
using HomeAssistantLink.Monitors.Display;
using HomeAssistantLink.Monitors.TcpPort;
using HomeAssistantLink.Monitors.Vpn;
using HomeAssistantLink.Monitors.WebCam;
using HomeAssistantLink.Plugins.ScriptRunner;
using HomeAssistantLink.Plugins.ShutDownComputer;
using HomeAssistantLink.UserSession;
using Microsoft.AspNetCore.Mvc;

DotEnv.Fluent().WithProbeForEnv(int.MaxValue).Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithApiKey();
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "HomeAssistantLink Service";
});

builder.Services.AddWindowsService();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.IncludeFields = true;
});

builder.Services.AddHostedService<Worker>();

builder
    .AddDomain()
    .AddUserSessionClient()
    .AddServiceSessionServer()
    .AddUserSessionEventServer()
    .AddInfrastructure()
    .AddClients()
    .AddWebCamMonitor()
    .AddVpnMonitor()
    .AddTcpPortMonitor()
    .AddDisplayMonitor()
    .AddShutdownComputer()
    .AddScriptRunner();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiKeyMiddleware>();

var api = app.MapGroup("/api");

api.MapPost(
    "/",
    ([FromBody] StateModel request, IPluginHandler handler) =>
    {
        handler.Handle(request.EntityId, request.State);
    });

app.Run();
