import os
import requests
import logging
from datetime import datetime, timedelta
from dotenv import load_dotenv
from docx import Document
from docx.shared import Pt
from docx.enum.text import WD_PARAGRAPH_ALIGNMENT
from docx2pdf import convert

logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")

CONSTANT_LINE_ITEMS = [{"description": "GitHub Co-pilot ($10/month)", "amount": 10.00}]
TABLE_STYLE_CANDIDATES = ["Light List Accent 1", "Medium Grid 1 Accent 1", "Table Grid"]
_project_cache = {}

def load_env():
    logging.info("Loading environment variables")
    load_dotenv()

def get_time_entries(start, end):
    logging.info(f"Fetching detailed entries from {start} to {end}")
    url = f"https://reports.api.clockify.me/v1/workspaces/{os.getenv('WORKSPACE_ID')}/reports/detailed"
    headers = {"X-Api-Key": os.getenv("CLOCKIFY_API_KEY"), "Content-Type": "application/json"}
    entries, page = [], 1
    while True:
        body = {
            "dateRangeStart": start, "dateRangeEnd": end, "exportType": "JSON",
            "users": {"ids": [os.getenv("USER_ID")]},
            "detailedFilter": {"page": page, "pageSize": 50}
        }
        resp = requests.post(url, headers=headers, json=body)
        resp.raise_for_status()
        batch = resp.json().get("timeentries", [])
        if not batch:
            break
        entries.extend(batch)
        page += 1
    logging.info(f"Retrieved {len(entries)} entries")
    return entries

def get_project_name(project_id):
    if project_id in _project_cache:
        return _project_cache[project_id]
    url = f"https://api.clockify.me/api/v1/workspaces/{os.getenv('WORKSPACE_ID')}/projects/{project_id}"
    resp = requests.get(url, headers={"X-Api-Key": os.getenv("CLOCKIFY_API_KEY")})
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
        secs = dur if isinstance(dur, (int, float)) else parse_iso(dur)
        hours = secs / 3600
        summary.setdefault(proj, 0)
        summary[proj] += hours
    return summary

def parse_iso(d):
    d = d[2:]; h = m = s = 0
    if "H" in d: part, d = d.split("H"); h = int(part)
    if "M" in d: part, d = d.split("M"); m = int(part)
    if "S" in d: s = int(d.replace("S", ""))
    return h*3600 + m*60 + s

def get_month_range():
    today = datetime.today()
    start = today.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    nxt = start.replace(day=28) + timedelta(days=4)
    end = nxt.replace(day=1) - timedelta(seconds=1)
    return start.isoformat()+"Z", end.isoformat()+"Z"

def setup_styles(doc):
    fmt = doc.styles['Normal'].paragraph_format
    fmt.space_before = Pt(0); fmt.space_after = Pt(0); fmt.line_spacing = 1

def generate_invoice(summary, month_year):
    load_env()
    frm   = os.getenv("FROM_NAME")
    addr  = [os.getenv("COMPANY_ADDRESS_LINE1"), os.getenv("COMPANY_ADDRESS_LINE2"), os.getenv("COMPANY_ADDRESS_LINE3")]
    email = os.getenv("CONTACT_EMAIL","")
    phone = os.getenv("CONTACT_PHONE","")
    bank  = {
        "Account Number": os.getenv("BANK_ACCOUNT_NUMBER"),
        "Account holder": os.getenv("BANK_ACCOUNT_HOLDER"),
        "ACH & Wire Routing Number": os.getenv("BANK_ROUTING_NUMBER"),
        "SWIFT": os.getenv("BANK_SWIFT")
    }
    rate = float(os.getenv("HOURLY_RATE"))

    doc = Document()
    setup_styles(doc)

    # Title
    p = doc.add_paragraph()
    run = p.add_run(f"Developer Invoice        {month_year}")
    run.bold = True; run.font.size = Pt(24)
    p.alignment = WD_PARAGRAPH_ALIGNMENT.CENTER

    doc.add_paragraph()

    # Address
    p = doc.add_paragraph()
    run = p.add_run("Address")
    run.bold = True
    p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT
    for line in addr:
        doc.add_paragraph(line)

    # Contact Information
    doc.add_paragraph()
    p = doc.add_paragraph()
    run = p.add_run("Contact Information")
    run.bold = True
    doc.add_paragraph(frm)
    if email:
        doc.add_paragraph(f"Email: {email}")
    if phone:
        doc.add_paragraph(f"Phone: {phone}")

    doc.add_paragraph()

    # Billing Table
    table = doc.add_table(rows=1, cols=4)
    for style in TABLE_STYLE_CANDIDATES:
        try:
            table.style = style
            break
        except KeyError:
            continue
    hdr = table.rows[0].cells
    for i, text in enumerate(("Project","Hours","Rate","Amount")):
        run = hdr[i].paragraphs[0].add_run(text)
        run.bold = True
        hdr[i].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.CENTER

    total = 0.0
    for proj, hrs in summary.items():
        row = table.add_row().cells
        row[0].text = proj
        row[1].text = f"{hrs:.2f}"; row[1].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT
        row[2].text = f"${rate:.2f}"; row[2].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT
        amt = hrs * rate
        row[3].text = f"${amt:.2f}"; row[3].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT
        total += amt

    # GitHub line item
    gh = CONSTANT_LINE_ITEMS[0]
    row = table.add_row().cells
    row[0].text = gh["description"]
    row[3].text = f"${gh['amount']:.2f}"; row[3].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT
    total += gh["amount"]

    # Total row
    row = table.add_row().cells
    row[0].text = "Total"
    row[3].text = f"${total:.2f}"; row[3].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT

    doc.add_paragraph()

    # Banking Details
    p = doc.add_paragraph()
    run = p.add_run("Wise Banking Details")
    run.bold = True
    for key, val in bank.items():
        doc.add_paragraph(f"{key}: {val}")
    for line in addr:
        doc.add_paragraph(line)
    if email:
        doc.add_paragraph(f"Email: {email}")

    filename = f"Invoice_{month_year}.docx"
    doc.save(filename)
    logging.info(f"Saved {filename}")
    return filename

def convert_to_pdf(fn):
    logging.info(f"Converting {fn} to PDF")
    convert(fn)
    logging.info("Conversion done")

def main():
    logging.info("Starting invoice generation")
    load_env()
    start, end = get_month_range()
    entries = get_time_entries(start, end)
    summary = process_entries(entries)
    month_year = datetime.today().strftime("%B %Y")
    docx = generate_invoice(summary, month_year)
    convert_to_pdf(docx)
    logging.info("Invoice complete")

if __name__ == "__main__":
    main()
