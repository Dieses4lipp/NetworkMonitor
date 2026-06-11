import httpx

API_BASE_URL = "http://localhost:5000/api"

async def fetch_devices():
    """Fetches the current list of devices from the Gateway API."""
    async with httpx.AsyncClient() as client:
        try:
            response = await client.get(f"{API_BASE_URL}/devices")
            response.raise_for_status()
            return response.json()
        except Exception as e:
            print(f"Error fetching devices: {e}")
            return []