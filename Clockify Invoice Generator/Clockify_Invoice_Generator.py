import os
import requests
import logging
from datetime import datetime, timedelta
from dotenv import load_dotenv
from docx import Document
from docx2pdf import convert

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s"
)

def load_env():
    logging.info("Loading environment variables from .env file.")
    load_dotenv()

def get_time_entries(
    start,
    end
):
    logging.info(f"Fetching detailed report entries from {start} to {end}.")
    url = (
        f"https://reports.api.clockify.me/v1/"
        f"workspaces/{os.getenv('WORKSPACE_ID')}/reports/detailed"
    )
    headers = {
        "X-Api-Key": os.getenv("CLOCKIFY_API_KEY"),
        "Content-Type": "application/json"
    }

    all_entries = []
    page = 1
    while True:
        body = {
            "dateRangeStart": start,
            "dateRangeEnd": end,
            "exportType": "JSON",
            "users": {"ids": [os.getenv("USER_ID")]},
            "detailedFilter": {"page": page, "pageSize": 50}
        }
        logging.info(f"Requesting detailed report page {page}.")
        response = requests.post(url, headers=headers, json=body)
        try:
            response.raise_for_status()
        except requests.HTTPError as e:
            logging.exception(f"Failed to fetch detailed report page {page}.")
            raise e

        data = response.json()
        entries = data.get("timeentries", [])
        if not entries:
            logging.info("No more detailed entries to fetch.")
            break

        logging.info(f"Fetched {len(entries)} detailed entries on page {page}.")
        all_entries.extend(entries)
        page += 1

    logging.info(f"Total detailed entries fetched: {len(all_entries)}")
    return all_entries

def process_entries(
    entries
):
    logging.info("Processing detailed entries into project summaries.")
    project_summary = {}

    for entry in entries:
        if not entry.get("billable"):
            continue

        project_name = entry.get("project", {}).get("name", "No Project")
        duration = entry["timeInterval"].get("duration", 0)
        hours = parse_duration_to_hours(duration)

        project_summary.setdefault(project_name, 0)
        project_summary[project_name] += hours

    logging.info(f"Processed summaries for {len(project_summary)} projects.")
    return project_summary

def parse_duration_to_hours(
    duration
):
    if isinstance(duration, (int, float)):
        return duration / 3600
    if duration.startswith("PT"):
        duration = duration[2:]
    hours = minutes = seconds = 0
    if "H" in duration:
        h, duration = duration.split("H")
        hours = int(h)
    if "M" in duration:
        m, duration = duration.split("M")
        minutes = int(m)
    if "S" in duration:
        s = duration.replace("S", "")
        seconds = int(s)
    return hours + minutes / 60 + seconds / 3600

def generate_invoice(
    project_summary,
    month_year
):
    logging.info(f"Generating invoice document for {month_year}.")
    hourly_rate = float(os.getenv("HOURLY_RATE"))

    document = Document()
    document.add_heading(f'Invoice - {month_year}', 0)
    document.add_paragraph(f'Company: {os.getenv("COMPANY_NAME")}')
    document.add_paragraph(f'Client: {os.getenv("CLIENT_NAME")}')
    document.add_paragraph(f'Date: {datetime.now():%Y-%m-%d}')

    table = document.add_table(rows=1, cols=4)
    table.style = 'Table Grid'
    hdr_cells = table.rows[0].cells
    hdr_cells[0].text = 'Project'
    hdr_cells[1].text = 'Hours'
    hdr_cells[2].text = 'Rate'
    hdr_cells[3].text = 'Amount'

    total_amount = 0
    for project, hours in project_summary.items():
        row = table.add_row().cells
        row[0].text = project
        row[1].text = f"{hours:.2f}"
        row[2].text = f"${hourly_rate:.2f}"
        amt = hours * hourly_rate
        row[3].text = f"${amt:.2f}"
        total_amount += amt

    document.add_paragraph()
    document.add_paragraph(f'Total Due: ${total_amount:.2f}')

    filename = f'Invoice_{month_year}.docx'
    document.save(filename)
    logging.info(f"Saved Word invoice as {filename}.")
    return filename

def convert_to_pdf(
    docx_filename
):
    logging.info(f"Converting {docx_filename} to PDF.")
    try:
        convert(docx_filename)
        logging.info(f"PDF created: {docx_filename.replace('.docx', '.pdf')}")
    except Exception:
        logging.exception("Conversion to PDF failed.")
        raise

def get_current_month_range():
    today = datetime.today()
    start = today.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    next_month = start.replace(day=28) + timedelta(days=4)
    end = next_month.replace(day=1) - timedelta(seconds=1)
    return start.isoformat() + "Z", end.isoformat() + "Z"

def main():
    logging.info("Starting invoice generation.")
    load_env()
    start_date, end_date = get_current_month_range()
    try:
        entries = get_time_entries(start_date, end_date)
        summary = process_entries(entries)
        month_year = datetime.now().strftime("%B_%Y")
        docx = generate_invoice(summary, month_year)
        convert_to_pdf(docx)
    except Exception:
        logging.exception("Invoice generation failed.")
        raise
    logging.info("Invoice generation complete.")

if __name__ == "__main__":
    main()
