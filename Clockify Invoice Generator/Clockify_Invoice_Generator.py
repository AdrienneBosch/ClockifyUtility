import os
import requests
import logging
from datetime import datetime, timedelta
from dotenv import load_dotenv
from docx import Document
from docx2pdf import convert
from docx.shared import Pt
from docx.enum.text import WD_PARAGRAPH_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")

INDUSTRY = "Software Developer"
CONSTANT_LINE_ITEMS = [{"description": "GitHub Co-pilot subscription", "amount": 10.00}]
TABLE_STYLE_CANDIDATES = ["Iron Boardroom", "Light List Accent 1", "Medium Grid 1 Accent 1", "Table Grid"]
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
        if not batch: break
        entries.extend(batch); page += 1
    logging.info(f"Total entries fetched: {len(entries)}")
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
        if not e.get("billable"): continue
        proj = e.get("project", {}).get("name") or (get_project_name(e["projectId"]) if e.get("projectId") else "No Project")
        dur = e["timeInterval"].get("duration", 0)
        hrs = dur/3600 if isinstance(dur, (int,float)) else parse_iso_duration(dur)
        summary[proj] = summary.get(proj, 0) + hrs
    logging.info(f"{len(summary)} projects summarized")
    return summary

def parse_iso_duration(d):
    d = d[2:]
    h = m = s = 0
    if "H" in d: part,d = d.split("H"); h=int(part)
    if "M" in d: part,d = d.split("M"); m=int(part)
    if "S" in d: s=int(d.replace("S",""))
    return h + m/60 + s/3600

def add_divider(doc):
    p = doc.add_paragraph(); pPr = p._p.get_or_add_pPr()
    pb = OxmlElement('w:pBdr'); bottom=OxmlElement('w:bottom')
    bottom.set(qn('w:val'), 'single'); bottom.set(qn('w:sz'), '6'); bottom.set(qn('w:color'), '000000')
    pb.append(bottom); pPr.append(pb)
    doc.add_paragraph()

def set_cell_alignment(cell, align):
    for p in cell.paragraphs: p.alignment = align

def generate_invoice(summary, month_year):
    logging.info(f"Generating invoice for {month_year}")
    load_env()
    frm = os.getenv("FROM_NAME")
    addr = [os.getenv(k) for k in ("COMPANY_ADDRESS_LINE1","COMPANY_ADDRESS_LINE2","COMPANY_ADDRESS_LINE3")]
    email, phone = os.getenv("CONTACT_EMAIL",""), os.getenv("CONTACT_PHONE","")
    bank = {k:os.getenv(k) for k in ("BANK_ACCOUNT_NUMBER","BANK_ACCOUNT_HOLDER","BANK_ROUTING_NUMBER","BANK_SWIFT")}
    rate = float(os.getenv("HOURLY_RATE"))

    doc = Document()
    doc.add_heading(f"Invoice - {month_year} - {frm}", level=1)
    doc.add_heading("Address", level=3)
    for line in addr: doc.add_paragraph(line)
    add_divider(doc)

    doc.add_heading("Contact Information", level=3)
    if email: doc.add_paragraph(f"Email: {email}")
    if phone: doc.add_paragraph(f"Phone: {phone}")
    add_divider(doc)

    doc.add_heading("Billable Hours & Charges", level=2)
    table = doc.add_table(rows=1, cols=4)
    for style in TABLE_STYLE_CANDIDATES:
        try: table.style = style; break
        except KeyError: continue
    hdr = table.rows[0].cells
    for i,text in enumerate(("Description","Hours","Rate","Amount")):
        hdr[i].text, hdr[i].paragraphs[0].runs[0].font.bold = text, True

    total = 0
    for proj,hrs in summary.items():
        row = table.add_row().cells
        row[0].text = proj
        row[1].text = f"{hrs:.2f}"; set_cell_alignment(row[1], WD_PARAGRAPH_ALIGNMENT.RIGHT)
        row[2].text = f"${rate:.2f}"; set_cell_alignment(row[2], WD_PARAGRAPH_ALIGNMENT.RIGHT)
        amt = hrs*rate; row[3].text = f"${amt:.2f}"
        set_cell_alignment(row[3], WD_PARAGRAPH_ALIGNMENT.RIGHT)
        total += amt

    for item in CONSTANT_LINE_ITEMS:
        row = table.add_row().cells
        row[0].text = item["description"]
        row[3].text = f"${item['amount']:.2f}"; set_cell_alignment(row[3], WD_PARAGRAPH_ALIGNMENT.RIGHT)
        total += item["amount"]

    foot = table.add_row().cells
    foot[0].text="Total"; foot[3].text=f"${total:.2f}"; set_cell_alignment(foot[3],WD_PARAGRAPH_ALIGNMENT.RIGHT)
    add_divider(doc)

    doc.add_heading("Banking Details", level=2)
    for k,v in bank.items(): doc.add_paragraph(f"{k.replace('_',' ').title()}: {v}")

    filename = f"Invoice_{month_year}.docx"
    doc.save(filename); logging.info(f"Saved {filename}")
    return filename

def convert_to_pdf(f): convert(f)

def get_current_month_range():
    today=datetime.today(); start=today.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    nxt=start.replace(day=28)+timedelta(days=4); end=nxt.replace(day=1)-timedelta(seconds=1)
    return start.isoformat()+"Z", end.isoformat()+"Z"

def main():
    logging.info("Starting invoice")
    load_env(); s,e = get_current_month_range()
    entries = get_time_entries(s,e); summary=process_entries(entries)
    my = datetime.now().strftime("%B_%Y"); docx=generate_invoice(summary,my)
    convert_to_pdf(docx); logging.info("Done")

if __name__=="__main__":
    main()
