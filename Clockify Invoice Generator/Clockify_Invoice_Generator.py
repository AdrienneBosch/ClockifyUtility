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

INDUSTRY = "Software Developer"
CONSTANT_LINE_ITEMS = [
    {"description": "GitHub Co-pilot subscription", "amount": 10.00}
]
_project_cache = {}

def load_env():
    logging.info("Loading environment variables")
    load_dotenv()

def get_time_entries(start, end):
    logging.info(f"Fetching detailed report entries from {start} to {end}")
    url = (
        f"https://reports.api.clockify.me/v1/"
        f"workspaces/{os.getenv('WORKSPACE_ID')}/reports/detailed"
    )
    headers = {
        "X-Api-Key": os.getenv("CLOCKIFY_API_KEY"),
        "Content-Type": "application/json"
    }
    entries = []
    page = 1
    while True:
        body = {
            "dateRangeStart": start,
            "dateRangeEnd": end,
            "exportType": "JSON",
            "users": {"ids": [os.getenv("USER_ID")]},
            "detailedFilter": {"page": page, "pageSize": 50}
        }
        logging.info(f"Requesting page {page}")
        resp = requests.post(url, headers=headers, json=body)
        resp.raise_for_status()
        batch = resp.json().get("timeentries", [])
        if not batch:
            break
        entries.extend(batch)
        page += 1
    logging.info(f"Total entries fetched: {len(entries)}")
    return entries

def get_project_name(project_id):
    if project_id in _project_cache:
        return _project_cache[project_id]
    url = (
        f"https://api.clockify.me/api/v1/"
        f"workspaces/{os.getenv('WORKSPACE_ID')}/projects/{project_id}"
    )
    headers = {"X-Api-Key": os.getenv("CLOCKIFY_API_KEY")}
    resp = requests.get(url, headers=headers)
    resp.raise_for_status()
    name = resp.json().get("name", "Unknown Project")
    _project_cache[project_id] = name
    return name

def process_entries(entries):
    logging.info("Summarizing billable hours by project")
    summary = {}
    for e in entries:
        if not e.get("billable"):
            continue
        proj = e.get("project", {}).get("name")
        if not proj and e.get("projectId"):
            proj = get_project_name(e["projectId"])
        proj = proj or "No Project"
        dur = e["timeInterval"].get("duration", 0)
        hrs = parse_duration_to_hours(dur)
        summary.setdefault(proj, 0)
        summary[proj] += hrs
    logging.info(f"{len(summary)} projects summarized")
    return summary

def parse_duration_to_hours(duration):
    if isinstance(duration, (int, float)):
        return duration / 3600
    if duration.startswith("PT"):
        d = duration[2:]
        h = m = s = 0
        if "H" in d:
            part, d = d.split("H"); h = int(part)
        if "M" in d:
            part, d = d.split("M"); m = int(part)
        if "S" in d:
            s = int(d.replace("S", ""))
        return h + m/60 + s/3600
    return 0

def generate_invoice(summary, month_year):
    logging.info(f"Generating invoice for {month_year}")
    from_name   = os.getenv("FROM_NAME")
    addr1       = os.getenv("COMPANY_ADDRESS_LINE1")
    addr2       = os.getenv("COMPANY_ADDRESS_LINE2")
    addr3       = os.getenv("COMPANY_ADDRESS_LINE3")
    email       = os.getenv("CONTACT_EMAIL", "")
    phone       = os.getenv("CONTACT_PHONE", "")
    acct_num    = os.getenv("BANK_ACCOUNT_NUMBER")
    acct_holder = os.getenv("BANK_ACCOUNT_HOLDER")
    routing     = os.getenv("BANK_ROUTING_NUMBER")
    swift       = os.getenv("BANK_SWIFT")
    rate        = float(os.getenv("HOURLY_RATE"))

    doc = Document()
    doc.add_heading(f"Invoice - {month_year} - {from_name}", level=1)

    doc.add_heading("Address", level=3)
    doc.add_paragraph(addr1)
    doc.add_paragraph(addr2)
    doc.add_paragraph(addr3)

    doc.add_paragraph("-" * 50)

    doc.add_heading("Contact Information", level=3)
    if email:
        doc.add_paragraph(f"Email: {email}")
    if phone:
        doc.add_paragraph(f"Phone: {phone}")

    doc.add_paragraph("-" * 50)

    doc.add_heading("Billable Hours & Charges", level=2)
    table = doc.add_table(rows=1, cols=4)
    try:
        table.style = "Iron Boardroom"
    except KeyError:
        table.style = "Table Grid"
    hdr = table.rows[0].cells
    hdr[0].text = "Description"
    hdr[1].text = "Hours"
    hdr[2].text = "Rate"
    hdr[3].text = "Amount"

    total = 0.0
    for proj, hrs in summary.items():
        row = table.add_row().cells
        row[0].text = proj
        row[1].text = f"{hrs:.2f}"
        row[2].text = f"${rate:.2f}"
        amt = hrs * rate
        row[3].text = f"${amt:.2f}"
        total += amt

    for item in CONSTANT_LINE_ITEMS:
        row = table.add_row().cells
        row[0].text = item["description"]
        row[1].text = ""
        row[2].text = ""
        row[3].text = f"${item['amount']:.2f}"
        total += item["amount"]

    footer_row = table.add_row().cells
    footer_row[0].text = "Total"
    footer_row[1].text = ""
    footer_row[2].text = ""
    footer_row[3].text = f"${total:.2f}"

    doc.add_paragraph("-" * 50)

    doc.add_heading("Banking Details", level=2)
    doc.add_paragraph(f"Account Number: {acct_num}")
    doc.add_paragraph(f"Account Holder: {acct_holder}")
    doc.add_paragraph(f"Routing Number: {routing}")
    doc.add_paragraph(f"SWIFT: {swift}")

    filename = f"Invoice_{month_year}.docx"
    doc.save(filename)
    logging.info(f"Saved invoice as {filename}")
    return filename

def convert_to_pdf(docx_filename):
    logging.info(f"Converting {docx_filename} to PDF")
    convert(docx_filename)
    logging.info("Conversion complete")

def get_current_month_range():
    today = datetime.today()
    start = today.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    nxt   = start.replace(day=28) + timedelta(days=4)
    end   = nxt.replace(day=1) - timedelta(seconds=1)
    return start.isoformat() + "Z", end.isoformat() + "Z"

def main():
    logging.info("Starting invoice generation")
    load_env()
    start, end = get_current_month_range()
    entries = get_time_entries(start, end)
    summary = process_entries(entries)
    month_year = datetime.now().strftime("%B_%Y")
    docx = generate_invoice(summary, month_year)
    convert_to_pdf(docx)
    logging.info("Invoice generation complete")

if __name__ == "__main__":
    main()
