from __future__ import annotations

import asyncio
from datetime import datetime
from typing import Optional

from rich.text import Text
from textual import on, work
from textual.app import App, ComposeResult
from textual.binding import Binding
from textual.containers import Horizontal, Vertical
from textual.css.query import NoMatches
from textual.reactive import reactive
from textual.screen import ModalScreen
from textual.widgets import (
    Button,
    DataTable,
    Footer,
    Header,
    Input,
    Label,
    Select,
    Static,
    TabbedContent,
    TabPane,
)
from textual.events import Click
from api.client import (
    create_job,
    delete_job,
    fetch_devices,
    fetch_jobs,
    fetch_scans,
    trigger_scan,
)
from widgets.device_list import DeviceList


# ---------------------------------------------------------------------------
# CSS
# ---------------------------------------------------------------------------

APP_CSS = """
/* ── Global ─────────────────────────────────────────────────────────────── */
Screen {
    background: #0d1117;
    color: #c9d1d9;
}

/* ── Custom Header bar ───────────────────────────────────────────────────── */
#header-bar {
    height: 3;
    background: #161b22;
    border-bottom: solid #30363d;
    layout: horizontal;
    align: left middle;
    padding: 0 2;
}

#app-title {
    color: #58a6ff;
    text-style: bold;
    width: auto;
    margin-right: 1;
}

#nav-sep {
    color: #30363d;
    width: auto;
    margin: 0 1;
}

.nav-item {
    color: #8b949e;
    width: auto;
    margin: 0 1;
}

.nav-item.active {
    color: #58a6ff;
    text-style: bold underline;
}

#clock {
    dock: right;
    color: #8b949e;
    width: auto;
    margin-right: 2;
}

/* ── Tab content areas ───────────────────────────────────────────────────── */
#devices-view {
    height: 1fr;
    layout: vertical;
}

#scans-table {
    height: 1fr;
    margin: 1 2;
    border: round #30363d;
}

#jobs-table {
    height: 1fr;
    margin: 1 2;
    border: round #30363d;
}

/* ── DataTable theming ───────────────────────────────────────────────────── */
DataTable {
    background: #0d1117;
}

DataTable > .datatable--header {
    background: #161b22;
    color: #8b949e;
    text-style: bold;
}

DataTable > .datatable--cursor {
    background: #1f3a5f;
    color: #58a6ff;
}

DataTable > .datatable--fixed {
    background: #161b22;
}

DataTable > .datatable--zebra {
    background: #0d1117;
}

/* ── Detail / sparkline panel ────────────────────────────────────────────── */
DeviceDetailPanel {
    border: round #30363d;
    border-title-color: #58a6ff;
    background: #0d1117;
    height: 7;
    margin: 0 1 1 1;
    padding: 0 1;
}

DeviceDetailPanel #spark-label {
    color: #58a6ff;
    text-style: bold;
    margin-top: 1;
}

DeviceDetailPanel #spark-stats {
    color: #8b949e;
    margin-top: 1;
}

/* ── Footer keybindings ──────────────────────────────────────────────────── */
Footer {
    background: #161b22;
    color: #8b949e;
}

Footer > .footer--key {
    background: #30363d;
    color: #c9d1d9;
}

/* ── Modal dialogs ───────────────────────────────────────────────────────── */
ModalScreen {
    background: rgba(0, 0, 0, 0.7);
    align: center middle;
}

#modal-container {
    background: #161b22;
    border: round #30363d;
    padding: 2 3;
    width: 60;
    height: auto;
    max-height: 30;
}

#modal-container Label {
    color: #58a6ff;
    text-style: bold;
    margin-bottom: 1;
}

#modal-container Input {
    margin-bottom: 1;
    border: round #30363d;
    background: #0d1117;
}

#modal-container Select {
    margin-bottom: 1;
    border: round #30363d;
    background: #0d1117;
}

#modal-buttons {
    layout: horizontal;
    height: auto;
    align: right middle;
    margin-top: 1;
}

#modal-buttons Button {
    margin-left: 1;
}

Button.-primary {
    background: #1f6feb;
    border: none;
    color: #ffffff;
}

Button.-secondary {
    background: #21262d;
    border: none;
    color: #c9d1d9;
}

/* ── Filter bar ──────────────────────────────────────────────────────────── */
#filter-bar {
    height: 3;
    display: none;
    padding: 0 2;
    background: #161b22;
    border-bottom: solid #30363d;
}

#filter-bar.visible {
    display: block;
}

#filter-input {
    border: round #30363d;
    background: #0d1117;
}

/* ── Summary bar ─────────────────────────────────────────────────────────── */
#summary-bar {
    height: 1;
    margin: 0 2;
    color: #8b949e;
    padding: 0 1;
}
"""

# ---------------------------------------------------------------------------
# New Job Modal
# ---------------------------------------------------------------------------

JOB_TYPES = [
    ("ICMP Ping", "1"),
    ("HTTP Check", "2"),
    ("TCP Port", "3"),
    ("SNMP", "4"),
]


class NewJobModal(ModalScreen):
    """Modal for creating a new monitoring job on the selected device."""

    def __init__(self, device: dict, **kwargs):
        super().__init__(**kwargs)
        self.device = device

    def compose(self) -> ComposeResult:
        name = self.device.get("displayName") or self.device.get("hostname", "Unknown")
        ip = self.device.get("ipAddress", "")
        with Vertical(id="modal-container"):
            yield Label(f"New Job — {name} ({ip})")
            yield Label("Job Type:", classes="field-label")
            yield Select(
                options=JOB_TYPES,
                value="1",
                id="job-type",
                allow_blank=False,
            )
            yield Label("Interval (seconds):", classes="field-label")
            yield Input(placeholder="60", id="interval", value="60")
            yield Label("Config JSON (optional):", classes="field-label")
            yield Input(placeholder='{"Url": "http://..."}', id="config-json")
            with Horizontal(id="modal-buttons"):
                yield Button("Cancel", variant="default", id="btn-cancel", classes="-secondary")
                yield Button("Create", variant="primary", id="btn-create", classes="-primary")

    @on(Button.Pressed, "#btn-cancel")
    def cancel(self) -> None:
        self.dismiss(None)

    @on(Button.Pressed, "#btn-create")
    async def create(self) -> None:
        job_type_val = self.query_one("#job-type", Select).value
        interval_raw = self.query_one("#interval", Input).value.strip()
        config = self.query_one("#config-json", Input).value.strip() or None

        try:
            interval = int(interval_raw) if interval_raw else 60
        except ValueError:
            interval = 60

        try:
            job_type = int(job_type_val) if job_type_val else 1
        except (ValueError, TypeError):
            job_type = 1

        device_id = self.device.get("id")
        if device_id is not None:
            result = await create_job(device_id, job_type, interval, config)
            self.dismiss(result)
        else:
            self.dismiss(None)


# ---------------------------------------------------------------------------
# Main Application
# ---------------------------------------------------------------------------

class NetworkMonitorApp(App):
    """NetworkMonitor TUI — terminal frontend for the .NET Gateway API."""

    CSS = APP_CSS
    TITLE = "NetworkMonitor"

    BINDINGS = [
        Binding("s", "scan", "Scan", show=True),
        Binding("n", "new_job", "New Job", show=True),
        Binding("d", "delete_job", "Delete", show=True),
        Binding("slash", "toggle_filter", "Filter", show=True),
        Binding("1", "show_tab('devices')", "Devices", show=False),
        Binding("2", "show_tab('scans')", "Scans", show=False),
        Binding("3", "show_tab('jobs')", "Jobs", show=False),
        Binding("q", "quit", "Quit", show=True),
    ]

    _active_tab: reactive[str] = reactive("devices")

    def compose(self) -> ComposeResult:
        # ── Custom header ──────────────────────────────────────────────────
        with Horizontal(id="header-bar"):
            yield Static("NetworkMonitor", id="app-title")
            yield Static("│", id="nav-sep")
            yield Static("Devices", id="nav-devices", classes="nav-item active")
            yield Static("│", classes="nav-sep-inner")
            yield Static("Scans", id="nav-scans", classes="nav-item")
            yield Static("│", classes="nav-sep-inner")
            yield Static("Jobs", id="nav-jobs", classes="nav-item")
            yield Static("", id="clock")

        # ── Filter bar (hidden by default) ─────────────────────────────────
        with Horizontal(id="filter-bar"):
            yield Input(placeholder="Filter by IP or name…", id="filter-input")

        # ── Devices tab ────────────────────────────────────────────────────
        with Vertical(id="devices-view"):
            yield DeviceList(id="device-list")

        # ── Scans tab (hidden initially) ───────────────────────────────────
        yield DataTable(id="scans-table", cursor_type="row", zebra_stripes=False)

        # ── Jobs tab (hidden initially) ────────────────────────────────────
        yield DataTable(id="jobs-table", cursor_type="row", zebra_stripes=False)

        yield Footer()

    # ── Lifecycle ─────────────────────────────────────────────────────────

    def on_mount(self) -> None:
        # Init scans table columns
        scans_table = self.query_one("#scans-table", DataTable)
        scans_table.add_columns("ID", "Start Time", "End Time", "Devices Found", "Status")

        # Init jobs table columns
        jobs_table = self.query_one("#jobs-table", DataTable)
        jobs_table.add_columns("ID", "Device ID", "Type", "Interval (s)", "Last Run")

        # Hide secondary views initially
        self.query_one("#scans-table").display = False
        self.query_one("#jobs-table").display = False

        # Start clock ticker
        self.set_interval(1.0, self._tick_clock)

        # Start auto-refresh
        self.set_interval(10.0, self._refresh_current_tab)

        # Initial load
        self._refresh_current_tab()

    def _tick_clock(self) -> None:
        now = datetime.now().strftime("%H:%M:%S")
        try:
            self.query_one("#clock", Static).update(now)
        except NoMatches:
            pass

    # ── Tab switching ──────────────────────────────────────────────────────

    def action_show_tab(self, tab: str) -> None:
        self._switch_to(tab)

    def _switch_to(self, tab: str) -> None:
        self._active_tab = tab

        # Show/hide panels
        self.query_one("#devices-view").display = tab == "devices"
        self.query_one("#scans-table").display = tab == "scans"
        self.query_one("#jobs-table").display = tab == "jobs"

        # Update nav highlight
        for t in ("devices", "scans", "jobs"):
            try:
                el = self.query_one(f"#nav-{t}", Static)
                if t == tab:
                    el.add_class("active")
                    el.remove_class("nav-item") # force re-render trick
                    el.add_class("nav-item")
                else:
                    el.remove_class("active")
            except NoMatches:
                pass

        self._refresh_current_tab()

    @on(Click, "#nav-devices")
    def nav_devices(self) -> None:
        self._switch_to("devices")

    @on(Click, "#nav-scans")
    def nav_scans(self) -> None:
        self._switch_to("scans")

    @on(Click, "#nav-jobs")
    def nav_jobs(self) -> None:
        self._switch_to("jobs")

    # ── Data loading ───────────────────────────────────────────────────────

    @work(exclusive=True)
    async def _refresh_current_tab(self) -> None:
        if self._active_tab == "devices":
            await self._load_devices()
        elif self._active_tab == "scans":
            await self._load_scans()
        elif self._active_tab == "jobs":
            await self._load_jobs()

    async def _load_devices(self) -> None:
        devices = await fetch_devices()
        try:
            device_list = self.query_one("#device-list", DeviceList)
            device_list.devices = devices
        except NoMatches:
            pass

    async def _load_scans(self) -> None:
        scans = await fetch_scans()
        table = self.query_one("#scans-table", DataTable)
        table.clear()
        for scan in scans:
            start = scan.get("startTime", "—")
            end = scan.get("endTime", "—") or "—"
            # Trim ISO timestamps to readable form
            if "T" in str(start):
                start = start[:19].replace("T", " ")
            if "T" in str(end):
                end = end[:19].replace("T", " ")
            status = scan.get("status", "—")
            status_text = Text(status, style="green" if status == "Completed" else "yellow")
            table.add_row(
                str(scan.get("id", "—")),
                start,
                end,
                str(scan.get("devicesFound", "—")),
                status_text,
            )

    async def _load_jobs(self) -> None:
        # No list-all endpoint; show job types as a reference or leave empty
        # with a helpful message
        table = self.query_one("#jobs-table", DataTable)
        table.clear()
        job_types = await fetch_jobs()
        for jt in job_types:
            table.add_row(
                str(jt.get("id", "—")),
                "—",
                jt.get("name", "—"),
                "—",
                "—",
            )

    # ── Actions ────────────────────────────────────────────────────────────

    @work(exclusive=False)
    async def action_scan(self) -> None:
        """Trigger an on-demand network scan."""
        self.notify("Scanning network…", title="Scan", timeout=3)
        result = await trigger_scan()
        if result:
            found = result.get("devicesFound", "?")
            self.notify(f"Scan complete — {found} device(s) found.", title="✓ Scan", timeout=5)
            await self._load_devices()
        else:
            self.notify("Scan failed. Is the API running?", severity="error", title="✗ Scan", timeout=5)

    def action_new_job(self) -> None:
        """Open the New Job modal for the currently selected device."""
        try:
            device_list = self.query_one("#device-list", DeviceList)
            device = device_list.get_selected_device()
        except NoMatches:
            device = None

        if device is None:
            self.notify("Select a device first.", severity="warning", timeout=3)
            return

        def _on_close(result) -> None:
            if result is not None:
                self.notify("Job created.", title="✓ New Job", timeout=3)
                self._refresh_current_tab()
            
        self.push_screen(NewJobModal(device), _on_close)

    @work(exclusive=False)
    async def action_delete_job(self) -> None:
        """Delete the selected job (if on jobs tab) or notify."""
        if self._active_tab != "jobs":
            self.notify("Switch to the Jobs tab to delete a job.", timeout=3)
            return
        table = self.query_one("#jobs-table", DataTable)
        if table.cursor_row is None:
            self.notify("No job selected.", severity="warning", timeout=3)
            return
        row = table.get_row_at(table.cursor_row)
        job_id_str = str(row[0]) if row else None
        if job_id_str and job_id_str.isdigit():
            success = await delete_job(int(job_id_str))
            if success:
                self.notify(f"Job {job_id_str} deleted.", title="✓ Delete", timeout=3)
                await self._load_jobs()
            else:
                self.notify("Delete failed.", severity="error", timeout=3)

    def action_toggle_filter(self) -> None:
        """Show/hide the filter input bar."""
        bar = self.query_one("#filter-bar")
        if bar.has_class("visible"):
            bar.remove_class("visible")
        else:
            bar.add_class("visible")
            try:
                self.query_one("#filter-input", Input).focus()
            except NoMatches:
                pass

    @on(Input.Changed, "#filter-input")
    def on_filter_changed(self, event: Input.Changed) -> None:
        """Live-filter the device table."""
        query = event.value.lower()
        try:
            device_list = self.query_one("#device-list", DeviceList)
            if not query:
                # Re-render full list
                device_list.watch_devices(device_list.devices)
                return
            filtered = [
                d for d in device_list.devices
                if query in (d.get("ipAddress") or "").lower()
                or query in (d.get("displayName") or "").lower()
            ]
            device_list.watch_devices(filtered)
        except NoMatches:
            pass

    @on(Input.Submitted, "#filter-input")
    def on_filter_submitted(self) -> None:
        """Close filter on Enter."""
        self.action_toggle_filter()


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    app = NetworkMonitorApp()
    app.run()