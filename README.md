# HomeAssistantLink

![Build & Test](https://github.com/Rmkrs/HomeAssistantLink/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**HomeAssistantLink** is a small .NET-powered Windows service that links your PC or workstation to [Home Assistant](https://www.home-assistant.io/).

It watches local system state, such as webcam usage, VPN connectivity, or TCP port availability, and publishes those changes to Home Assistant through its REST API. It also exposes a small local API that Home Assistant can call back into to trigger local plugins.

Plugins can run either in the Windows service session or in the interactive user session through the companion tray app. This makes system-safe actions, such as shutdown, possible from the service while still allowing desktop-aware actions, such as starting a kiosk browser, to run inside the logged-in user session.

In other words: your workstation gets a little Home Assistant nervous system, now with a polite desktop butler attached.

## Features

- Detect webcam usage and publish it to Home Assistant
- Detect VPN connectivity and publish it to Home Assistant
- Monitor TCP ports and publish availability to Home Assistant
- Update Home Assistant helpers through the REST API
- Supports `input_boolean`, `input_text`, `input_number`, and date/time values through `input_text`
- Debounces repeated state updates to reduce noise
- Local API for Home Assistant triggered plugins
- API key protection for the local API
- System-session plugins for service-safe actions
- User-session plugins for desktop-aware actions
- Companion tray app for interactive user-session commands
- Named-pipe bridge between service and tray app
- Tray menu is generated from service-owned plugin configuration
- Tray app survives service restarts and reports service availability
- Configured PowerShell script runner plugin
- Shutdown plugin
- Runs as a normal app during development
- Runs as a Windows service in production
- Unit tested
- Small monitor and plugin architecture

## Requirements

- Windows
- .NET 10 SDK for development
- .NET 10 runtime for deployment
- Home Assistant with a long-lived access token
- Optional: configured Home Assistant helpers such as `input_boolean`, `input_text`, or `input_number`
- Optional: the tray app for plugins that require the interactive user session

## Quick start

### 1. Clone the repository

```bash
git clone https://github.com/Rmkrs/HomeAssistantLink.git
cd HomeAssistantLink
```

### 2. Configure `.env`

Create a `.env` file in the repository root for development, or next to the published service executable for deployment.

```env
HomeAssistantLink__HomeAssistant__Host=http://homeassistant.local:8123
HomeAssistantLink__HomeAssistant__ApiKey=your_home_assistant_long_lived_access_token

HomeAssistantLink__Api__Key=your_local_api_key

HomeAssistantLink__Monitors__WebCamMonitor__EntityId=input_boolean.machinename_webcam

HomeAssistantLink__Monitors__VpnMonitor__EntityId=input_boolean.machinename_vpn
HomeAssistantLink__Monitors__VpnMonitor__NetworkInterfaceDescription=Your VPN Network Adapter Description

HomeAssistantLink__Monitors__TcpPortMonitor__Targets__0__Name=Example RDP
HomeAssistantLink__Monitors__TcpPortMonitor__Targets__0__EntityId=input_boolean.machinename_rdp_available
HomeAssistantLink__Monitors__TcpPortMonitor__Targets__0__Host=machinename.local
HomeAssistantLink__Monitors__TcpPortMonitor__Targets__0__Port=3389
HomeAssistantLink__Monitors__TcpPortMonitor__Targets__0__ScanIntervalSeconds=30
HomeAssistantLink__Monitors__TcpPortMonitor__Targets__0__TimeoutMilliseconds=2000

HomeAssistantLink__PluginHost__RunAs=System

HomeAssistantLink__UserSession__PipeName=HomeAssistantLink.UserSession
HomeAssistantLink__UserSession__ConnectTimeoutMilliseconds=2000

HomeAssistantLink__ServiceSession__PipeName=HomeAssistantLink.ServiceSession
HomeAssistantLink__ServiceSession__ConnectTimeoutMilliseconds=2000

HomeAssistantLink__Plugins__ShutDownPlugin__RunAs=System
HomeAssistantLink__Plugins__ShutDownPlugin__DisplayName=Shutdown machine
HomeAssistantLink__Plugins__ShutDownPlugin__Command=shutdown-machine
HomeAssistantLink__Plugins__ShutDownPlugin__EntityId=button.machinename_shutdown

HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__0__RunAs=User
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__0__DisplayName=Start kiosk
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__0__EntityId=button.machinename_kiosk
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__0__Command=start-kiosk
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__0__ScriptPath=C:\Services\Kiosk\Start-Kiosk.ps1
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__0__TimeoutSeconds=30

HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__1__RunAs=User
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__1__DisplayName=Stop kiosk
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__1__EntityId=button.machinename_kiosk
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__1__Command=stop-kiosk
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__1__ScriptPath=C:\Services\Kiosk\Stop-Kiosk.ps1
HomeAssistantLink__Plugins__ScriptRunnerPlugin__Actions__1__TimeoutSeconds=30
```

Replace `machinename` with the identifier you want to use for this machine.

The Home Assistant token is used by HomeAssistantLink when it calls Home Assistant.

The local API key is used by Home Assistant when it calls HomeAssistantLink.

You can also configure the same settings in `appsettings.json`.

### 3. Run the service host

```bash
dotnet run --project Source/HomeAssistantLink.Host.WebApi
```

In development, Swagger UI is available and can be used to call the local API.

### 4. Run the tray app when using user-session plugins

```bash
dotnet run --project Source/HomeAssistantLink.Host.TrayApp
```

The tray app does not own plugin configuration. It asks the service for user-session commands and builds its context menu from the service-owned command catalog.

If the service is unavailable, the tray menu stays alive and reports that the service session is unavailable. When the service comes back, reopening the menu reloads the available commands.

## Home Assistant setup

Create helpers for the values you want HomeAssistantLink to update.

Example `configuration.yaml`:

```yaml
input_boolean:
  machinename_webcam:
    name: Webcam Monitor
  machinename_vpn:
    name: VPN Monitor
  machinename_rdp_available:
    name: RDP Available
```

Example automation:

```yaml
alias: Turn key light off when webcam turns off
trigger:
  - platform: state
    entity_id: input_boolean.machinename_webcam
    from: "on"
    to: "off"
action:
  - service: light.turn_off
    target:
      entity_id: light.right
```

## Local API

HomeAssistantLink exposes a local API under:

```text
/api
```

The API is protected by an API key. Requests must include:

```http
X-Api-Key: your_local_api_key
```

The local API can be used by Home Assistant automations or REST commands to trigger plugins.

Example shutdown payload:

```json
{
  "entityId": "button.machinename_shutdown",
  "state": "shutdown-machine"
}
```

Example script runner payload:

```json
{
  "entityId": "button.machinename_kiosk",
  "state": "start-kiosk"
}
```

The plugin command must match configured command metadata before anything is executed.

## Home Assistant REST command examples

Example `rest_command` entries:

```yaml
shutdown_machinename:
  url: "http://machinename.local:5421/api/"
  method: POST
  headers:
    X-Api-Key: !secret ha_link_machinename_api_key
  payload: '{"entityId":"button.machinename_shutdown","state":"shutdown-machine"}'
  content_type: "application/json"

start_kiosk_on_machinename:
  url: "http://machinename.local:5421/api/"
  method: POST
  headers:
    X-Api-Key: !secret ha_link_machinename_api_key
  payload: '{"entityId":"button.machinename_kiosk","state":"start-kiosk"}'
  content_type: "application/json"

stop_kiosk_on_machinename:
  url: "http://machinename.local:5421/api/"
  method: POST
  headers:
    X-Api-Key: !secret ha_link_machinename_api_key
  payload: '{"entityId":"button.machinename_kiosk","state":"stop-kiosk"}'
  content_type: "application/json"
```

## Running as a Windows service

Publish the service host first:

```bash
dotnet publish Source/HomeAssistantLink.Host.WebApi -c Release -o C:\Services\HomeAssistantLink
```

Create the Windows service:

```bash
sc create "HomeAssistantLink Service" binPath="C:\Services\HomeAssistantLink\HomeAssistantLink.Host.WebApi.exe"
```

Start it:

```bash
sc start "HomeAssistantLink Service"
```

Stop it:

```bash
sc stop "HomeAssistantLink Service"
```

Delete it:

```bash
sc delete "HomeAssistantLink Service"
```

## Running the tray app at login

Publish the tray app:

```bash
dotnet publish Source/HomeAssistantLink.Host.TrayApp -c Release -o C:\Services\HomeAssistantLink.TrayApp
```

Then start `HomeAssistantLink.Host.TrayApp.exe` from your preferred Windows startup mechanism, for example the Startup folder, Task Scheduler, or another launcher.

The tray app should run inside the interactive user session. It is needed for plugins configured with:

```json
"RunAs": "User"
```

## Concepts

### Monitors

A monitor watches local machine state and publishes `EntityStateUpdate` values.

Current monitors include:

- `WebCamMonitor`
- `VpnMonitor`
- `TcpPortMonitor`

Monitors implement `IMonitor`:

```csharp
public interface IMonitor
{
    string Name { get; }

    Task StartAsync(
        Func<EntityStateUpdate, CancellationToken, Task> publish,
        CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
```

A monitor decides when something changed. The domain layer decides whether the update should be sent to Home Assistant.

### Entity updates

State changes are represented by `EntityStateUpdate`:

```csharp
public sealed record EntityStateUpdate(
    string EntityId,
    HomeAssistantEntityType EntityType,
    object? Value);
```

Supported entity types:

```csharp
public enum HomeAssistantEntityType
{
    Boolean,
    Text,
    Number,
    DateTime,
}
```

The Home Assistant REST client maps those types to service calls:

- `Boolean` -> `input_boolean.turn_on` / `input_boolean.turn_off`
- `Text` -> `input_text.set_value`
- `Number` -> `input_number.set_value`
- `DateTime` -> `input_text.set_value`

### Debounce

`Debounce` prevents repeated identical updates from being sent too quickly.

This keeps Home Assistant updates calm and avoids noisy flapping when Windows emits multiple system events for the same effective state.

### Plugins

A plugin reacts to calls from Home Assistant through the local API.

Plugins implement `IPlugin`:

```csharp
public interface IPlugin
{
    IEnumerable<PluginCommand> GetCommands();

    bool CanExecute(string entityId, string state);

    void Execute(string entityId, string state);
}
```

Each plugin exposes one or more `PluginCommand` values. The command metadata tells HomeAssistantLink which `entityId` and `state` combinations are valid, how the command should be displayed, and whether the command should run as `System` or `User`.

### Plugin command routing

Plugin execution is intentionally split by host type.

`System` commands execute in the service host. Use this for actions that are safe and valid in session 0, such as shutting down the machine.

`User` commands execute in the tray app. Use this for actions that need the logged-in user's desktop session, such as opening, moving, or closing visible applications.

The service remains the authority:

1. Home Assistant calls the service API.
2. The service validates the API key.
3. The service resolves the command from its configured plugin catalog.
4. If the command is `System`, the service executes it directly.
5. If the command is `User`, the service forwards the approved command to the tray app over a named pipe.
6. The tray app executes the matching user-session command.

The tray app can also show configured user commands in its context menu. Even then, it invokes by `CommandId`; the service resolves the command again before execution.

### Named pipes

HomeAssistantLink uses named pipes for communication between the service and tray app.

There are two channels:

- `ServiceSession`: tray app asks the service for available user commands or requests command execution by `CommandId`
- `UserSession`: service forwards approved user-session commands to the tray app

Default pipe names:

```text
HomeAssistantLink.ServiceSession
HomeAssistantLink.UserSession
```

### Script runner plugin

The script runner plugin executes configured PowerShell scripts.

It never accepts an arbitrary script path from Home Assistant. Scripts must be configured ahead of time.

Example:

```json
"ScriptRunnerPlugin": {
    "Actions": [
        {
            "RunAs": "User",
            "DisplayName": "Start kiosk",
            "EntityId": "button.machinename_kiosk",
            "Command": "start-kiosk",
            "ScriptPath": "C:\\Services\\Kiosk\\Start-Kiosk.ps1",
            "TimeoutSeconds": 30
        }
    ]
}
```

Safety model:

- Wrong entity id -> ignored
- Wrong command -> ignored
- Unknown command id -> rejected
- Unknown script path -> not executed
- Non-`.ps1` file -> not executed
- Configured script only -> executed

## Project structure

```text
Source/
  HomeAssistantLink.Clients/
  HomeAssistantLink.Domain/
  HomeAssistantLink.Host.TrayApp/
  HomeAssistantLink.Host.WebApi/
  HomeAssistantLink.Infrastructure/
  HomeAssistantLink.Monitors.TcpPort/
  HomeAssistantLink.Monitors.Vpn/
  HomeAssistantLink.Monitors.WebCam/
  HomeAssistantLink.Plugins.ScriptRunner/
  HomeAssistantLink.Plugins.ShutDownComputer/
  HomeAssistantLink.UserSession/

Tests/
  Unit/
    HomeAssistantLink.DomainUnitTests/
    HomeAssistantLink.InfrastructureUnitTests/
    HomeAssistantLink.Plugins.ShutDownComputerUnitTests/
```

### Source projects

- `HomeAssistantLink.Domain`: contracts, command catalog, plugin routing, and orchestration
- `HomeAssistantLink.Infrastructure`: debounce and platform-independent infrastructure
- `HomeAssistantLink.Clients`: Home Assistant REST API client
- `HomeAssistantLink.Host.WebApi`: service host and local API
- `HomeAssistantLink.Host.TrayApp`: interactive user-session tray host
- `HomeAssistantLink.UserSession`: named-pipe communication between service and tray app
- `HomeAssistantLink.Monitors.*`: local state monitors
- `HomeAssistantLink.Plugins.*`: local actions triggered from Home Assistant

## Testing

Run all tests:

```bash
dotnet test
```

The test suite uses:

- NUnit
- Shouldly
- Moq
- Library.UnitTesting

## Development notes

HomeAssistantLink intentionally avoids heavy Home Assistant client dependencies. The app only needs a small part of the Home Assistant REST API, so it uses `HttpClient` directly.

This keeps the dependency graph small, avoids stale transitive packages, and makes the actual Home Assistant calls easy to inspect.

The plugin system also intentionally keeps Home Assistant commands boring. Home Assistant sends an `entityId` and `state`; HomeAssistantLink resolves that against its own configuration and command catalog. This keeps the service in charge and avoids turning Home Assistant into a remote arbitrary command runner.

## License

MIT
