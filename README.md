# NetworkMonitor

NetworkMonitor is a robust .NET 10 solution for continuous network performance monitoring and metric collection, built with clean architecture principles.

## Features
- **Continuous Background Monitoring:** Agent runs as a reliable .NET BackgroundService.
- **Metric Collection:** Captures and processes raw network metrics (e.g., latency, packet loss).
- **Service Discovery:** TCP connect scan of every discovered device across a configurable port set (38 well-known ports by default), naming each open port from a built-in catalog and recording any banner it offers. Results are reconciled per device on every scan and drive platform classification.
- **Gateway API:** Exposes collected data over HTTP, including `GET /api/devices/{id}/services` for discovered network services and `GET /api/devices/{id}/service-units` for systemd units read from inspected hosts.
- **TUI Client:** Python terminal UI for viewing monitoring data, with a per-device service list (`v`).
- **Clean Architecture:** Domain-centric design isolating entities like `RawMetric`.
- **Modern .NET:** Leverages the performance and features of .NET 10.

## Architecture
- **Core** — `NetworkMonitor.Domain` (entities/interfaces), `NetworkMonitor.Application` (use cases)
- **Infrastructure** — `NetworkMonitor.Infrastructure.Data` (EF persistence)
- **Apps** — `NetworkMonitor.Agent` (background worker), `NetworkMonitor.Gateway.Api` (HTTP API)
- **Tui** — Python terminal client
