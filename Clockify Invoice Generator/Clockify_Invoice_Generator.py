import os
import requests
import logging
from datetime import datetime, timedelta
from dotenv import load_dotenv
from docx import Document
from docx.shared import Pt
from docx.enum.text import WD_PARAGRAPH_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx2pdf import convert

logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")

CONSTANT_LINE_ITEMS = [{"description": "GitHub Co-pilot ($10/month)", "amount": 10.00}]
TABLE_STYLE_CANDIDATES = ["Iron Boardroom", "Light List Accent 1", "Medium Grid 1 Accent 1", "Table Grid"]

def load_env():
    load_dotenv()

def get_time_entries(start, end):
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
    return entries

def process_entries(entries):
    total = 0.0
    for e in entries:
        if e.get("billable"):
            dur = e["timeInterval"].get("duration", 0)
            total += (dur if isinstance(dur, (int, float)) else parse_iso_duration(dur))
    return total / 3600

def parse_iso_duration(d):
    d = d[2:]; h = m = s = 0
    if "H" in d: part, d = d.split("H"); h = int(part)
    if "M" in d: part, d = d.split("M"); m = int(part)
    if "S" in d: s = int(d.replace("S",""))
    return h*3600 + m*60 + s

def get_current_month_range():
    today = datetime.today()
    start = today.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    nxt = start.replace(day=28) + timedelta(days=4)
    end = nxt.replace(day=1) - timedelta(seconds=1)
    return start.isoformat()+"Z", end.isoformat()+"Z"

def add_divider(doc):
    p = doc.add_paragraph()
    pPr = p._p.get_or_add_pPr()
    pb = OxmlElement('w:pBdr'); bottom = OxmlElement('w:bottom')
    bottom.set(qn('w:val'), 'single'); bottom.set(qn('w:sz'), '6'); bottom.set(qn('w:color'), '000000')
    pb.append(bottom); pPr.append(pb)

def setup_styles(doc):
    normal = doc.styles['Normal']
    pf = normal.paragraph_format
    pf.line_spacing = 1
    pf.space_before = Pt(0)
    pf.space_after = Pt(0)

def generate_invoice(total_hours, month_year):
    load_env()
    # load config
    frm   = os.getenv("FROM_NAME")
    addr  = [os.getenv("COMPANY_ADDRESS_LINE1"),
             os.getenv("COMPANY_ADDRESS_LINE2"),
             os.getenv("COMPANY_ADDRESS_LINE3")]
    email = os.getenv("CONTACT_EMAIL")
    phone = os.getenv("CONTACT_PHONE")
    rate  = float(os.getenv("HOURLY_RATE"))

    doc = Document()
    setup_styles(doc)

    # Title: large, bold, centered with divider
    title_p = doc.add_paragraph()
    title_run = title_p.add_run(f"Developer Invoice        {month_year}")
    title_run.bold = True
    title_run.font.size = Pt(24)
    title_p.alignment = WD_PARAGRAPH_ALIGNMENT.CENTER
    add_divider(doc)

    # Address Section
    doc.add_paragraph()  # blank
    doc.add_heading("Address", level=3)
    for line in addr:
        doc.add_paragraph(line)
    doc.add_paragraph()

    # Contact Information under Address
    doc.add_heading("Contact Information", level=3)
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
    for idx, text in enumerate(("Project","Hours","Rate","Amount")):
        run = hdr[idx].paragraphs[0].add_run(text)
        run.bold = True

    total_amt = total_hours * rate
    # Worked Hours row
    row = table.add_row().cells
    row[0].text = "Worked Hours"
    row[1].text = f"{total_hours:.2f}"; row[1].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT
    row[2].text = f"${rate:.2f}"; row[2].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT
    row[3].text = f"${total_amt:.2f}"; row[3].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT

    # GitHub line item
    gh = CONSTANT_LINE_ITEMS[0]
    row = table.add_row().cells
    row[0].text = gh["description"]
    row[3].text = f"${gh['amount']:.2f}"; row[3].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT

    # Total row
    grand = total_amt + gh["amount"]
    row = table.add_row().cells
    row[0].text = "Total"
    row[3].text = f"${grand:.2f}"; row[3].paragraphs[0].alignment = WD_PARAGRAPH_ALIGNMENT.RIGHT
    doc.add_paragraph()

    # Banking Section
    doc.add_heading("Wise Banking Details", level=2)
    doc.add_paragraph(f"Account Number: {os.getenv('BANK_ACCOUNT_NUMBER')}")
    doc.add_paragraph(f"Account holder: {os.getenv('BANK_ACCOUNT_HOLDER')}")
    doc.add_paragraph(f"ACH & Wire Routing Number: {os.getenv('BANK_ROUTING_NUMBER')}")
    doc.add_paragraph(f"SWIFT: {os.getenv('BANK_SWIFT')}")
    for line in addr:
        doc.add_paragraph(line)
    if email:
        doc.add_paragraph(f"Email: {email}")

    filename = f"Invoice_{month_year}.docx"
    doc.save(filename)
    logging.info(f"Saved {filename}")
    return filename

def convert_to_pdf(fn):
    convert(fn)

def main():
    load_env()
    start, end = get_current_month_range()
    entries     = get_time_entries(start, end)
    hours       = process_entries(entries)
    month_year  = datetime.today().strftime("%B %Y")
    docx        = generate_invoice(hours, month_year)
    convert_to_pdf(docx)

if __name__ == "__main__":
    main()
