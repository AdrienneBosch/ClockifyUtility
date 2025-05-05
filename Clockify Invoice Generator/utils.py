import os
from datetime import datetime, timedelta

def parse_iso_duration(duration):
    duration = duration[2:]
    h = m = s = 0
    if "H" in duration:
        part, duration = duration.split("H")
        h = int(part)
    if "M" in duration:
        part, duration = duration.split("M")
        m = int(part)
    if "S" in duration:
        s = int(duration.replace("S", ""))
    return h * 3600 + m * 60 + s

def get_month_range():
    today = datetime.today()
    start = today.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    next_month = start.replace(day=28) + timedelta(days=4)
    end = next_month.replace(day=1) - timedelta(seconds=1)
    return start.isoformat() + "Z", end.isoformat() + "Z"

def get_month_year(start):
    dt = datetime.fromisoformat(start.replace("Z", ""))
    return dt.strftime("%B %Y")

def ensure_directory(path):
    if not os.path.exists(path):
        os.makedirs(path)

def clear_existing_file(path):
    if os.path.exists(path):
        os.remove(path)
