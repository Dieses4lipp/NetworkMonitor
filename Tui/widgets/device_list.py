from __future__ import annotations

import random
from datetime import datetime
from typing import Optional

from rich.text import Text
from textual.app import ComposeResult
from textual.containers import Horizontal, Vertical
from textual.css.query import NoMatches
from textual.reactive import reactive
from textual.widget import Widget
from textual.widgets import DataTable, Label, Static


# ---------------------------------------------------------------------------
# Sparkline renderer — renders a ping history as block-character bar chart
# ---------------------------------------------------------------------------

BLOCKS = " ▁▂▃▄▅▆▇█"

def _render_sparkline(values: list[float], width: int = 60) -> Text:
    """Turn a list of latency floats into a Rich Text sparkline."""
    if not values:
        return Text("no data", style="dim")

    # Take the last `width` samples
    sample = values[-width:]
    max_val = max(sample) if max(sample) > 0 else 1
    line = Text()
    for v in sample:
        ratio = v / max_val
        idx = min(int(ratio * (len(BLOCKS) - 1)), len(BLOCKS) - 1)
        char = BLOCKS[idx]
        if ratio > 0.75:
            style = "bold red"
        elif ratio > 0.4:
            style = "bold yellow"
        else:
            style = "bold green"
        line.append(char, style=style)
    return line


# ---------------------------------------------------------------------------
# Detail Panel
# ---------------------------------------------------------------------------

class DeviceDetailPanel(Widget):
    """Bottom panel showing ICMP ping sparkline for the selected device."""

    DEFAULT_CSS = """
    DeviceDetailPanel {
        height: 7;
        border: round $primary;
        padding: 0 1;
        background: $surface;
    }
    DeviceDetailPanel Label {
        color: $text;
    }
    DeviceDetailPanel #spark-label {
        color: $primary;
        text-style: bold;
    }
    DeviceDetailPanel #spark-stats {
        color: $text-muted;
        margin-top: 1;
    }
    """

    device: reactive[Optional[dict]] = reactive(None)
    # Fake ping history — replaced by real data when you wire up a metrics endpoint
    ping_history: reactive[list] = reactive(list)

    def compose(self) -> ComposeResult:
        yield Label("", id="spark-label")
        yield Static("", id="spark-line")
        yield Label("", id="spark-stats")

    def watch_device(self, device: Optional[dict]) -> None:
        if device is None:
            self._clear()
            return

        name = device.get("displayName") or device.get("hostname", "Unknown")
        self.query_one("#spark-label", Label).update(f"Detail: {name}")

        # Generate mock history for demo; swap for real metrics when available
        history = [max(0.0, random.gauss(2.0, 1.2)) for _ in range(60)]
        self.ping_history = history
        self._update_spark(history)

    def _update_spark(self, history: list[float]) -> None:
        spark_line = self.query_one("#spark-line", Static)
        spark_label = self.query_one("#spark-label", Label)
        stats_label = self.query_one("#spark-stats", Label)

        spark_line.update(Text("  ") + _render_sparkline(history, width=58))

        if history:
            valid = [v for v in history if v > 0]
            avg = sum(valid) / len(valid) if valid else 0
            mn = min(valid) if valid else 0
            mx = max(valid) if valid else 0
            stats_label.update(
                f"  avg {avg:.1f}ms   min {mn:.0f}ms   max {mx:.0f}ms"
            )
        else:
            stats_label.update("  no data")

    def _clear(self) -> None:
        try:
            self.query_one("#spark-label", Label).update("Detail: —")
            self.query_one("#spark-line", Static).update("")
            self.query_one("#spark-stats", Label).update("")
        except NoMatches:
            pass


# ---------------------------------------------------------------------------
# Main DeviceList widget
# ---------------------------------------------------------------------------

class DeviceList(Widget):
    """Device table + summary bar + detail panel."""

    DEFAULT_CSS = """
    DeviceList {
        width: 100%;
        height: 100%;
        layout: vertical;
    }

    #device-table {
        height: 1fr;
        margin: 0 1;
    }

    #summary-bar {
        height: 1;
        margin: 0 2;
        color: $text-muted;
    }

    DeviceDetailPanel {
        margin: 0 1 1 1;
    }
    """

    devices: reactive[list] = reactive(list)

    def __init__(self, **kwargs) -> None:
        super().__init__(**kwargs)
        self._selected_device: Optional[dict] = None

    def compose(self) -> ComposeResult:
        yield DataTable(id="device-table", cursor_type="row", zebra_stripes=False)
        yield Label("", id="summary-bar")
        yield DeviceDetailPanel()

    def on_mount(self) -> None:
        table = self.query_one("#device-table", DataTable)
        table.add_columns(
            "IP Address", "Name", "Status", "Latency", "Jobs"
        )

    def watch_devices(self, devices: list) -> None:
        table = self.query_one("#device-table", DataTable)
        table.clear()

        online = 0
        offline = 0

        for dev in devices:
            ip = dev.get("ipAddress") or dev.get("ip_address", "—")
            name = dev.get("displayName") or dev.get("hostname", "Unknown")
            raw_status = dev.get("status", 0)

            # status can be int (0/1) from the API or string from mock data
            if isinstance(raw_status, str):
                is_online = raw_status.lower() == "online"
            else:
                is_online = int(raw_status) == 1

            if is_online:
                online += 1
                status_text = Text("● Online", style="green")
                latency_val = dev.get("latencyMs") or dev.get("latency_ms")
                latency = Text(f"{latency_val}ms", style="green") if latency_val is not None else Text("—")
            else:
                offline += 1
                status_text = Text("○ Offline", style="red")
                latency = Text("—", style="dim")

            job_count = dev.get("jobCount") or dev.get("job_count") or 0
            job_text = Text(str(job_count), style="cyan" if job_count > 0 else "dim")

            # Highlight the selected IP
            ip_text = Text(ip, style="cyan bold" if self._selected_device and self._selected_device.get("ipAddress") == ip else "")

            table.add_row(ip_text, name, status_text, latency, job_text, key=str(dev.get("id", ip)))

        # Update summary bar
        last_scan = "—"
        try:
            summary = self.query_one("#summary-bar", Label)
            total = online + offline
            summary.update(
                f"  {total} device{'s' if total != 1 else ''}  ·  "
                f"[green]{online} online[/green]  ·  "
                f"[red]{offline} offline[/red]  ·  "
                f"last scan just now"
            )
        except NoMatches:
            pass

    def on_data_table_row_highlighted(self, event: DataTable.RowHighlighted) -> None:
        """Update the detail panel when a row is focused."""
        if event.row_key is None:
            return
        key = str(event.row_key.value)
        device = next(
            (d for d in self.devices if str(d.get("id", d.get("ipAddress"))) == key),
            None
        )
        self._selected_device = device
        detail = self.query_one(DeviceDetailPanel)
        detail.device = device

    def get_selected_device(self) -> Optional[dict]:
        return self._selected_device