# HomeAssistantLink

![Build & Test](https://github.com/Rmkrs/HomeAssistantLink/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**HomeAssistantLink** is a .NET-powered Windows Service that links your PC or workstation to [Home Assistant](https://www.home-assistant.io/) via its REST API. It detects system-level states (like webcam or VPN activity) and updates Home Assistant entities in real-time — enabling fast and flexible automations.

---

## ✨ Features

- Detects system-level states (e.g., webcam usage, VPN active)
- Updates `input_boolean`, `input_text` or `sensor` entities in Home Assistant
- Flexible monitor + plugin architecture
- Debounce mechanism to reduce noise
- Local API to trigger plugins
- API key protection for local API
- Optionally works as a Windows Service
- Unit tested and extensible

---

## 🚀 Quick Start

### 1. Clone the Repo

```bash
git clone https://github.com/Rmkrs/HomeAssistantLink.git
cd HomeAssistantLink
```

### 2. Configure `.env`

Create a `.env` file at the root:

# Replace 'machinename' with your actual device identifier

```env
HomeAssistantLink__HomeAssistant__Host=http://homeassistant.local:8123
HomeAssistantLink__HomeAssistant__ApiKey=your_long_lived_token

HomeAssistantLink__Monitors__WebCamMonitor__EntityId=input_boolean.machinename_webcam
HomeAssistantLink__Monitors__VpnMonitor__EntityId=input_boolean.machinename_vpn

HomeAssistantLink__Plugins__ShutdownPlugin__Command=shutdown
HomeAssistantLink__Plugins__ShutdownPlugin__EntityId=machinename

HomeAssistantLink__ApiKey=your_local_api_key
```

You can also configure these settings in `appsettings.json`.

### 3. Run the App

```bash
dotnet run --project Source/HomeAssistantLink.Host.WebApi
```

To install as a Windows Service:

```bash
sc create "HomeAssistantLink Service" binPath="C:\Path\To\Published\App.exe"
```

---

## 🧰 Concepts

### Monitors

A monitor detects a system state (e.g., camera usage, VPN connection). When its state changes, it updates an entity in Home Assistant.

Example:

```csharp
public class WebCamMonitor : IMonitorBool
{
    public string EntityId => "input_boolean.machinename_webcam";
    public bool Value => /* logic */;
    public void Start(Action onChange) => /* ... */;
    public void Stop() => /* ... */;
}
```

### Plugins

A plugin reacts to changes from Home Assistant (e.g., shutdown command).

Example:

```csharp
public class ShutDownPlugin : IPlugin
{
    public void Execute(string entityId, string state)
    {
        if (entityId == "machinename" && state == "shutdown")
        {
            /* shutdown logic */
        }
    }
}
```

---

## 📡 Home Assistant Setup

Add these to your `configuration.yaml`:

```yaml
input_boolean:
  machinename_webcam:
    name: Webcam Monitor
  machinename_vpn:
    name: VPN Monitor
```

Example automation:

```yaml
alias: Turn KeyLight off when Webcam turns off
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

---

## 🔐 API Protection

All calls to `/api` require an API key. Add it to your `.env`:

```env
HomeAssistantLink__ApiKey=your_local_api_key
```

Swagger UI (in development) will prompt for this key.

---

## 📆 Structure

- `Domain`: Monitor and plugin interfaces and core orchestration
- `Infrastructure`: Debounce and platform helpers
- `Clients`: HTTP client for Home Assistant
- `Host.WebApi`: Windows service host with local API
- `Monitors.*`: Webcam and VPN monitors
- `Plugins.*`: Example shutdown plugin

---

## ✅ Testing

All non-integration logic is covered by unit tests.

Run tests:

```bash
dotnet test
```

---

## 📜 License

MIT

