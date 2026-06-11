from textual.app import ComposeResult
from textual.containers import Horizontal
from textual.widgets import Label, ListItem, ListView
from textual.widget import Widget

class DeviceRow(ListItem):
    """A custom widget representing a single device in the list."""
    
    DEFAULT_CSS = """
    DeviceRow {
        height: 3;
        padding: 1 2;
        margin-bottom: 1;
        background: $boost;
        border-left: solid transparent;
    }
    
    DeviceRow:focus {
        background: $accent 20%;
        border-left: solid $accent;
    }

    .device-row-container {
        align: left middle;
        width: 100%;
    }

    .hostname {
        width: 30%;
        text-style: bold;
    }

    .ip-address {
        width: 25%;
        color: $text-muted;
    }

    .status {
        width: 20%;
    }

    .latency {
        width: 25%;
        content-align: right middle;
        color: $warning;
    }
    """

    def __init__(self, device: dict) -> None:
        super().__init__()
        self.device = device

    def compose(self) -> ComposeResult:
        """Layout the row horizontally."""
        hostname = self.device.get("hostname", "Unknown")
        ip = self.device.get("ipAddress", "0.0.0.0")
        status = self.device.get("status", "Offline").capitalize()
        latency = self.device.get("latencyMs", 0)

        status_color = "[bold green]" if status == "Online" else "[bold red]"
        icon = "🟢" if status == "Online" else "🔴"

        with Horizontal(classes="device-row-container"):
            yield Label(f"🖥️ {hostname}", classes="hostname")
            yield Label(ip, classes="ip-address")
            yield Label(f"{icon} {status_color}{status}[/]", classes="status")
            yield Label(f"{latency} ms", classes="latency")


class DeviceList(Widget):
    """The main container for the device list."""

    DEFAULT_CSS = """
    DeviceList {
        width: 100%;
        height: 100%;
        padding: 1;
        border: round $primary;
        border-title-color: $text;
    }
    """

    def compose(self) -> ComposeResult:
        self.border_title = "Network Devices"
        yield ListView(id="main_list")

    async def update_devices(self, devices: list) -> None:
        """Clears the list and repopulates it with new data."""
        list_view = self.query_one("#main_list", ListView)
        
        current_index = list_view.index

        await list_view.clear()
        
        for dev in devices:
            list_view.append(DeviceRow(dev))
            
        if current_index is not None and current_index < len(devices):
            list_view.index = current_index