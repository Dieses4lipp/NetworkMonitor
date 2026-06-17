import httpx
from typing import Optional
from textual import log

API_BASE_URL = "https://localhost:7270/api/"

_client: Optional[httpx.AsyncClient] = None


def get_client() -> httpx.AsyncClient:
    global _client
    if _client is None or _client.is_closed:
        _client = httpx.AsyncClient(base_url=API_BASE_URL, timeout=10.0, verify=False)
    return _client


async def fetch_devices() -> list[dict]:
    """GET /api/devices — returns all known devices."""
    try:
        response = await get_client().get("devices")
        response.raise_for_status()
        return response.json()
    except Exception as e:
        log(f"Error fetching devices: {e}")
        return []


async def fetch_device(device_id: int) -> Optional[dict]:
    """GET /api/devices/{id} — returns one device with job count."""
    try:
        response = await get_client().get(f"devices/{device_id}")
        response.raise_for_status()
        return response.json()
    except Exception:
        return None


async def fetch_scans() -> list[dict]:
    """GET /api/devices/scans — returns scan history."""
    try:
        response = await get_client().get("devices/scans")
        response.raise_for_status()
        return response.json()
    except Exception:
        return []


async def trigger_scan() -> Optional[dict]:
    """POST /api/devices/scan — triggers an on-demand network scan."""
    try:
        response = await get_client().post("devices/scan")
        response.raise_for_status()
        return response.json()
    except Exception:
        return None


async def fetch_jobs() -> list[dict]:
    """GET /api/jobs — fetches all monitoring jobs (via types endpoint for reference)."""
    try:
        # There is no GET /api/jobs list endpoint; we derive jobs from devices
        # We expose job types for display purposes
        response = await get_client().get("jobs/types")
        response.raise_for_status()
        return response.json()
    except Exception:
        return []


async def create_job(device_id: int, job_type: int, interval_seconds: int, config_json: Optional[str] = None) -> Optional[dict]:
    """POST /api/jobs — creates a new monitoring job."""
    try:
        payload = {
            "deviceId": device_id,
            "type": job_type,
            "intervalSeconds": interval_seconds,
            "configurationJson": config_json,
        }
        response = await get_client().post("jobs", json=payload)
        response.raise_for_status()
        return response.json()
    except Exception:
        return None


async def delete_job(job_id: int) -> bool:
    """DELETE /api/jobs/{id} — removes a monitoring job."""
    try:
        response = await get_client().delete(f"jobs/{job_id}")
        return response.status_code == 204
    except Exception:
        return False