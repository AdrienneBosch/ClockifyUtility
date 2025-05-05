import os
import requests
from logger import get_logger
from utils import parse_iso_duration
from exceptions import ApiError

logger = get_logger()
_project_cache = {}

def fetch_time_entries(start, end, config):
    logger.info(f"Fetching entries from {start} to {end}")

    url = f"https://reports.api.clockify.me/v1/workspaces/{config['WORKSPACE_ID']}/reports/detailed"
    headers = {
        "X-Api-Key": config["CLOCKIFY_API_KEY"],
        "Content-Type": "application/json"
    }

    entries = []
    page = 1

    while True:
        body = {
            "dateRangeStart": start,
            "dateRangeEnd": end,
            "exportType": "JSON",
            "users": {"ids": [config["USER_ID"]]},
            "detailedFilter": {"page": page, "pageSize": 50}
        }

        resp = requests.post(url, headers=headers, json=body)
        if not resp.ok:
            raise ApiError(f"Clockify report request failed: {resp.status_code} {resp.text}")

        batch = resp.json().get("timeentries", [])
        if not batch:
            break

        entries.extend(batch)
        page += 1

    logger.info(f"Retrieved {len(entries)} entries")
    return entries

def resolve_project_name(project_id, config):
    if project_id in _project_cache:
        return _project_cache[project_id]

    url = f"https://api.clockify.me/api/v1/workspaces/{config['WORKSPACE_ID']}/projects/{project_id}"
    headers = {"X-Api-Key": config["CLOCKIFY_API_KEY"]}

    resp = requests.get(url, headers=headers)
    if not resp.ok:
        raise ApiError(f"Clockify project lookup failed: {resp.status_code} {resp.text}")

    name = resp.json().get("name", "Unknown Project")
    _project_cache[project_id] = name
    return name

def summarize_entries(entries, config):
    summary = {}

    for entry in entries:
        if not entry.get("billable"):
            continue

        name = entry.get("project", {}).get("name")
        if not name and entry.get("projectId"):
            name = resolve_project_name(entry["projectId"], config)

        name = name or "No Project"
        duration = entry["timeInterval"].get("duration", 0)
        seconds = duration if isinstance(duration, (int, float)) else parse_iso_duration(duration)
        hours = seconds / 3600

        summary.setdefault(name, 0)
        summary[name] += hours

    return summary
