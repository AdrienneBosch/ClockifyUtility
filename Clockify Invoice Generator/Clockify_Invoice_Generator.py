import os
import requests
from datetime import datetime
from dotenv import load_dotenv
from docx import Document
from docx.shared import Inches
from docx2pdf import convert

def load_env():
    load_dotenv()

def get_time_entries(start, end):
    url = f"https://api.clockify.me/api/v1/workspaces/{os.getenv('WORKSPACE_ID')}/user/{os.getenv('USER_ID')}/time-entries"
    
    headers = {
        "X-Api-Key": os.getenv("CLOCKIFY_API_KEY"),
        "Content-Type": "application/json"
    }
    
    params = {
        "start": start,
        "end": end,
        "page-size": 50
    }
    
    all_entries = []
    page = 1
    
    while True:
        params["page"] = page
        response = requests.get(url, headers=headers, params=params)
        response.raise_for_status()
        entries = response.json()
        
        if not entries:
            break
        
        all_entries.extend(entries)
        page += 1
    
    return all_entries

def process_entries(entries):
    project_summary = {}
    
    for entry in entries:
        if not entry.get("billable"):
            continue
        
        project_name = entry["project"]["name"] if entry.get("project") else "No Project"
        duration_seconds = entry["timeInterval"].get("duration", "PT0S")
        
        hours = parse_duration_to_hours(duration_seconds)
        
        if project_name not in project_summary:
            project_summary[project_name] = 0
        
        project_summary[project_name] += hours
    
    return project_summary

def parse_duration_to_hours(duration):
    if duration.startswith("PT"):
        duration = duration[2:]
    
    hours = 0
    minutes = 0
    seconds = 0
    
    if "H" in duration:
        hours_part, duration = duration.split("H")
        hours = int(hours_part)
    if "M" in duration:
        minutes_part, duration = duration.split("M")
        minutes = int(minutes_part)
    if "S" in duration:
        seconds_part = duration.replace("S", "")
        seconds = int(seconds_part)
    
    total_hours = hours + minutes / 60 + seconds / 3600
    return total_hours

def generate_invoice(project_summary, month_year):
    hourly_rate = float(os.getenv("HOURLY_RATE"))
    
    document = Document()
    document.add_heading(f'Invoice - {month_year}', 0)
    
    document.add_paragraph(f'Company: {os.getenv("COMPANY_NAME")}')
    document.add_paragraph(f'Client: {os.getenv("CLIENT_NAME")}')
    document.add_paragraph(f'Date: {datetime.now().strftime("%Y-%m-%d")}')
    
    table = document.add_table(rows=1, cols=4)
    hdr_cells = table.rows[0].cells
    hdr_cells[0].text = 'Project'
    hdr_cells[1].text = 'Hours Worked'
    hdr_cells[2].text = 'Hourly Rate'
    hdr_cells[3].text = 'Total Amount'
    
    total_amount = 0
    
    for project, hours in project_summary.items():
        row_cells = table.add_row().cells
        row_cells[0].text = project
        row_cells[1].text = f"{hours:.2f}"
        row_cells[2].text = f"${hourly_rate:.2f}"
        project_total = hours * hourly_rate
        row_cells[3].text = f"${project_total:.2f}"
        total_amount += project_total
    
    document.add_paragraph()
    document.add_paragraph(f'Total Amount Due: ${total_amount:.2f}')
    
    invoice_filename = f'Invoice_{month_year}.docx'
    document.save(invoice_filename)
    
    return invoice_filename

def convert_to_pdf(docx_filename):
    convert(docx_filename)

def main():
    load_env()
    
    start_date = "2025-05-01T00:00:00Z"
    end_date = "2025-05-31T23:59:59Z"
    
    entries = get_time_entries(start=start_date, end=end_date)
    
    project_summary = process_entries(entries)
    
    invoice_docx = generate_invoice(project_summary, "May_2025")
    
    convert_to_pdf(invoice_docx)

if __name__ == "__main__":
    main()
