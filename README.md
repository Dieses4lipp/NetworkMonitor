# NetworkMonitor

NetworkMonitor is a robust .NET 10 Worker Service designed for continuous network performance monitoring and metric collection. Built with clean architecture principles, it efficiently processes and stores network data in the background.

## Features
- **Continuous Background Monitoring:** Runs as a reliable .NET BackgroundService.
- **Metric Collection:** Captures and processes raw network metrics (e.g., latency, packet loss).
- **Clean Architecture:** Domain-centric design isolating entities like `RawMetric`.
- **Modern .NET:** Leverages the performance and features of .NET 10.

## Architecture
Divided into Core (Domain entities/interfaces), Infrastructure, and Worker projects to ensure modularity and ease of testing.
