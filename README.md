# NetworkMonitor

NetworkMonitor is a robust .NET 10 solution for continuous network performance monitoring and metric collection, built with clean architecture principles.

## Features
- **Continuous Background Monitoring:** Agent runs as a reliable .NET BackgroundService.
- **Metric Collection:** Captures and processes raw network metrics (e.g., latency, packet loss, device fingerprinting).
- **Gateway API:** Exposes collected data over HTTP.
- **TUI Client:** Python terminal UI for viewing monitoring data.
- **Clean Architecture:** Domain-centric design isolating entities like `RawMetric`.
- **Modern .NET:** Leverages the performance and features of .NET 10.

## Architecture
- **Core** — `NetworkMonitor.Domain` (entities/interfaces), `NetworkMonitor.Application` (use cases)
- **Infrastructure** — `NetworkMonitor.Infrastructure.Data` (EF persistence)
- **Apps** — `NetworkMonitor.Agent` (background worker), `NetworkMonitor.Gateway.Api` (HTTP API)
- **Tui** — Python terminal client
