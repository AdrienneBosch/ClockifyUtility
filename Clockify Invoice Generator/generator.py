import os
from datetime import datetime
from docx import Document
from docx2pdf import convert
from formatter import DocumentFormatter
from table_builder import TableBuilder
from utils import ensure_directory, clear_existing_file, get_month_year
from logger import get_logger

logger = get_logger()

def generate_invoice_workflow(start, end, output_path, generate_pdf, config):
    from_name = config["FROM_NAME"]
    address_lines = [
        config["COMPANY_ADDRESS_LINE1"],
        config["COMPANY_ADDRESS_LINE2"],
        config["COMPANY_ADDRESS_LINE3"],
    ]
    email = config["CONTACT_EMAIL"]
    phone = config["CONTACT_PHONE"]
    rate = float(config["HOURLY_RATE"])
    bank = {
        "Bank Name": config["BANK_NAME"],
        "Account Number": config["BANK_ACCOUNT_NUMBER"],
        "Account holder": config["BANK_ACCOUNT_HOLDER"],
        "ACH & Wire Routing Number": config["BANK_ROUTING_NUMBER"],
        "SWIFT": config["BANK_SWIFT"]
    }

    from clockify import fetch_time_entries, summarize_entries
    entries = fetch_time_entries(start, end, config)
    summary = summarize_entries(entries, config)

    month_year = get_month_year(start)
    doc = Document()

    formatter = DocumentFormatter(doc)
    table_builder = TableBuilder(doc)

    formatter.add_title(f"Developer Invoice        {month_year}")
    doc.add_paragraph()

    formatter.add_heading_level(2, "Contact Information")
    formatter.add_body(from_name)
    formatter.add_body(f"Email: {email}")
    formatter.add_body(f"Phone: {phone}")
    doc.add_paragraph()

    formatter.add_heading_level(2, "Banking Details")
    for key, val in bank.items():
        p = doc.add_paragraph()
        p.add_run(f"{key}: ").bold = True
        p.add_run(val)
    doc.add_paragraph()

    formatter.add_heading_level(2, "Address")
    for line in address_lines:
        formatter.add_body(line)
    doc.add_paragraph()

    formatter.add_heading_level(2, "Billing Details")
    doc.add_paragraph()

    table_builder.create_billing_table(summary, rate)
    doc.add_paragraph()

    if output_path:
        base_path = output_path
    else:
        now = datetime.now()
        base_dir = os.path.join("output", str(now.year), now.strftime("%B"))
        ensure_directory(base_dir)
        base_path = os.path.join(base_dir, f"{from_name} Invoice ({month_year}).docx")

    clear_existing_file(base_path)
    doc.save(base_path)
    logger.info(f"Saved invoice: {base_path}")

    if generate_pdf:
        pdf_path = base_path.replace(".docx", ".pdf")
        clear_existing_file(pdf_path)
        convert(base_path)
        logger.info(f"Saved PDF: {pdf_path}")
