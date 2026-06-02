# HomeAssistantLink

![Build & Test](https://github.com/Rmkrs/HomeAssistantLink/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**HomeAssistantLink** is a small .NET-powered Windows service that links your PC or workstation to [Home Assistant](https://www.home-assistant.io/).

It watches local system state, such as webcam usage or VPN connectivity, and publishes those changes to Home Assistant through its REST API. It also exposes a small local API that Home Assistant can call back into, for example to trigger local plugins such as shutting down the machine.

In other words: your workstation gets a little Home Assistant nervous system.

## Features

- Detect webcam usage and publish it to Home Assistant
- Detect VPN connectivity and publish it to Home Assistant
- Update Home Assistant helpers through the REST API
- Supports `input_boolean`, `input_text`, and `input_number`
- Debounces repeated state updates to reduce noise
- Local API for Home Assistant triggered plugins
- API key protection for the local API
- Runs as a normal app during development
- Runs as a Windows service in production
- Unit tested
- Small monitor and plugin architecture

## Requirements

- Windows
- .NET 10 SDK for development
- .NET 10 runtime for deployment
- Home Assistant with a long-lived access token
- Optional: configured Home Assistant helpers such as `input_boolean`

## Quick start

### 1. Clone the repository

```bash
git clone https://github.com/Rmkrs/HomeAssistantLink.git
cd HomeAssistantLink
```

### 2. Configure `.env`

Create a `.env` file in the repository root.

```env
HomeAssistantLink__HomeAssistant__Host=http://homeassistant.local:8123
HomeAssistantLink__HomeAssistant__ApiKey=your_home_assistant_long_lived_access_token

HomeAssistantLink__Monitors__WebCamMonitor__EntityId=input_boolean.machinename_webcam

HomeAssistantLink__Monitors__VpnMonitor__EntityId=input_boolean.machinename_vpn
HomeAssistantLink__Monitors__VpnMonitor__NetworkInterfaceDescription=Your VPN Network Adapter Description

HomeAssistantLink__Plugins__ShutdownPlugin__Command=shutdown
HomeAssistantLink__Plugins__ShutdownPlugin__EntityId=machinename

HomeAssistantLink__ApiKey=your_local_api_key
```

Replace `machinename` with the identifier you want to use for this machine.

The Home Assistant token is used by HomeAssistantLink when it calls Home Assistant.

The local API key is used by Home Assistant when it calls HomeAssistantLink.

You can also configure the same settings in `appsettings.json`.

### 3. Run the app

```bash
dotnet run --project Source/HomeAssistantLink.Host.WebApi
```

In development, Swagger UI is available and can be used to call the local API.

## Home Assistant setup

Create helpers for the values you want HomeAssistantLink to update.

Example `configuration.yaml`:

```yaml
input_boolean:
  machinename_webcam:
    name: Webcam Monitor
  machinename_vpn:
    name: VPN Monitor
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

The local API can be used by Home Assistant automations to trigger plugins.

Example payload:

```json
{
  "entityId": "machinename",
  "state": "shutdown"
}
```

The shutdown plugin checks the configured entity id and command before invoking the shutdown action.

## Running as a Windows service

Publish the app first:

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

## Concepts

### Monitors

A monitor watches local machine state and publishes `EntityStateUpdate` values.

Current monitors include:

- `WebCamMonitor`
- `VpnMonitor`

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
    void Execute(string entityId, string state);
}
```

The included shutdown plugin checks the configured entity id and command before invoking shutdown behavior.

## Project structure

```text
Source/
  HomeAssistantLink.Clients/
  HomeAssistantLink.Domain/
  HomeAssistantLink.Host.WebApi/
  HomeAssistantLink.Infrastructure/
  HomeAssistantLink.Monitors.Vpn/
  HomeAssistantLink.Monitors.WebCam/
  HomeAssistantLink.Plugins.ShutDownComputer/

Tests/
  Unit/
    HomeAssistantLink.DomainUnitTests/
    HomeAssistantLink.InfrastructureUnitTests/
    HomeAssistantLink.Plugins.ShutDownComputerUnitTests/
```

### Source projects

- `HomeAssistantLink.Domain`: contracts and orchestration
- `HomeAssistantLink.Infrastructure`: debounce and platform-independent infrastructure
- `HomeAssistantLink.Clients`: Home Assistant REST API client
- `HomeAssistantLink.Host.WebApi`: service host and local API
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

## License

MIT
