from textual.app import App, ComposeResult
from textual.widgets import Header, Footer, DataTable
from textual.reactive import reactive
from rich.text import Text

async def fetch_devices_mock():
    """Mock data based on your screenshot."""
    return [
        {"id": 1, "hostname": "Proxmox-Node-1", "ipAddress": "192.168.1.10", "status": "Online", "latencyMs": 12},
        {"id": 2, "hostname": "PiHole-DNS", "ipAddress": "192.168.1.53", "status": "Online", "latencyMs": 3},
        {"id": 3, "hostname": "Unifi-Controller", "ipAddress": "192.168.1.200", "status": "Offline", "latencyMs": 0},
    ]

class NetworkMonitorApp(App):
    """A Textual app to monitor network devices."""
    
    CSS = """
    DataTable {
        height: 100%;
        margin: 1 2;
        border: round $primary;
    }
    """

    BINDINGS = [
        ("q", "quit", "Quit"),
        ("r", "refresh_data", "Manual Refresh")
    ]

    devices = reactive([])

    def compose(self) -> ComposeResult:
        yield Header(show_clock=True)
        yield DataTable(id="device_table")
        yield Footer()

    def on_mount(self) -> None:
        table = self.query_one(DataTable)
        
        table.cursor_type = "row" 
        table.zebra_stripes = True 
        
        table.add_columns("Hostname", "IP Address", "Status", "Latency")
        
        self.set_interval(5.0, self.action_refresh_data)
        self.action_refresh_data()

    async def action_refresh_data(self) -> None:
        self.devices = await fetch_devices_mock()

    def watch_devices(self, new_devices: list) -> None:
        table = self.query_one(DataTable)
        table.clear()
        
        for dev in new_devices:
            hostname = f"🖥️  {dev.get('hostname', 'Unknown')}"
            
            ip = dev.get("ipAddress", "-")
            
            raw_status = str(dev.get("status", "Offline")).capitalize()
            if raw_status == "Online":
                status = Text("🟢 Online", style="bold green")
            else:
                status = Text("🔴 Offline", style="bold red")
                
            latency = f"{dev.get('latencyMs', 0)} ms"

            table.add_row(hostname, ip, status, latency)

if __name__ == "__main__":
    app = NetworkMonitorApp()
    app.run()