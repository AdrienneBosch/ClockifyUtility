```markdown
# Overview

This Python-based invoice generator retrieves billable hours from Clockify and produces styled `.docx` invoices, with optional PDF conversion.

---

## Requirements

- **Windows** (batch file support)
- **Python 3.11** (recommended)
    - Download Python 3.11 from the official source:  
        - https://www.python.org/downloads/release/python-3110/
    - Ensure you check **"Add Python to PATH"** during installation.
- **MS Word**

---

## Features

* Pulls billable time entries from Clockify's Detailed Reports API
* Resolves missing project names via project API lookup
* Groups and totals hours by project
* Supports constant line items (e.g., subscriptions)
* Configurable currency symbol
* Customizable invoice table style and title color
* Automatically detects missing `USER_ID` and `WORKSPACE_ID` on first run
* Fully isolated virtual environment (no global Python pollution)

---

## Notes

* The batch file is designed for Windows. For macOS/Linux, use a shell script or run the commands manually.
* The app uses `docx` and `docx2pdf` for document generation and conversion.
* Ensure Word is installed for PDF conversion to work on Windows (docx2pdf requires Word on Windows).


---

## Quick Start

Use the provided batch script:

```

run\_invoice.bat

````

This script will:

1. Create a `.venv` virtual environment in the project root (if not present)
2. Activate the environment
3. Install all dependencies from `requirements.txt` using PyPI
4. Run the invoice generator (`main.py`)
5. Display logs in the terminal

The virtual environment is reused for future runs.

---

## First Run Behavior

If the `.env` file does **not** contain `USER_ID` or `WORKSPACE_ID`, the application will:

- Use the `CLOCKIFY_API_KEY` to retrieve the values
- Display them clearly in the terminal
- Exit without generating the invoice

Note: You must copy the displayed values into your `.env` before rerunning.

---

## How to Get Your Clockify API Key

1. Log in to [Clockify](https://clockify.me/)
2. Click your avatar → `Profile Settings`
3. Scroll down to the **API** section
4. Copy your **API Key**
5. Paste it in `.env` as `CLOCKIFY_API_KEY`

---

## Environment Setup

Create a `.env` file in the root directory. Use `.env.example` as a template.

### Required Fields

```dotenv
CLOCKIFY_API_KEY=your_api_key
USER_ID=your_user_id
WORKSPACE_ID=your_workspace_id

HOURLY_RATE=100.00
CURRENCY_SYMBOL=$

FROM_NAME=Your Name or Company
COMPANY_ADDRESS_LINE1=123 Main St
COMPANY_ADDRESS_LINE2=Suite 456
COMPANY_ADDRESS_LINE3=City, State ZIP

CONTACT_EMAIL=your@email.com
CONTACT_PHONE=+1-555-123-4567

BANK_NAME=Your Bank
BANK_ACCOUNT_NUMBER=000123456789
BANK_ACCOUNT_HOLDER=Your Name
BANK_ROUTING_NUMBER=110000000
BANK_SWIFT=SWFTCODE
````

### Optional Fields

```dotenv
OUTPUT_PATH=output
CONSTANT_LINE_ITEMS=[{"description": "GitHub Co-pilot ($10/month)", "amount": 10.00}]
TABLE_STYLE=Medium Shading 1 Accent 2
TITLE_COLOR=c0504d

TITLE_FONT_SIZE=24
BODY_FONT_SIZE=11
HEADING_FONT_SIZES={"1": 18, "2": 16, "3": 14, "4": 12}
SEPARATOR_COLOR=c0504d
SEPARATOR_SIZE=6

HEADER_SPACING_BEFORE=7
HEADER_SPACING_AFTER=10
BODY_SPACING_BEFORE=1
BODY_SPACING_AFTER=2
````

* `OUTPUT_PATH` is the root directory where invoices will be saved. You can specify either an absolute path (`C:\Invoices`) or a relative one (`output`).
* `CONSTANT_LINE_ITEMS` is a JSON array of fixed recurring charges to include on every invoice. Each item must have a `description` and `amount`.
* `TABLE_STYLE` must exactly match a built-in Microsoft Word table style. You can preview available styles in Word by creating a table and hovering over styles under the **Table Design** tab.
* `TITLE_COLOR` is a 6-digit hex code (no `#`) used for the invoice title text color.
* `TITLE_FONT_SIZE` sets the font size (in points) for the main title.
* `BODY_FONT_SIZE` controls font size for paragraph and body text.
* `HEADING_FONT_SIZES` is a JSON dictionary that maps heading levels (1–4) to specific font sizes.
* `SEPARATOR_COLOR` sets the color of the horizontal rule under headings/titles.
* `SEPARATOR_SIZE` is the thickness of that horizontal line.
* `HEADER_SPACING_BEFORE` and `HEADER_SPACING_AFTER` control spacing (in points) above and below titles/headings.
* `BODY_SPACING_BEFORE` and `BODY_SPACING_AFTER` do the same for paragraphs and body text.

---

## Output Location

By default, invoices are saved to:

```
output/
  └── yyyy/
      └── Month/
          └── Your Name Invoice (May 2025).docx
```

Customize the root path with `OUTPUT_PATH` in `.env`.
You may use an absolute path (e.g. `C:\Invoices`).

---

## Command Line Usage

You can run the invoice generator directly:

```bash
python main.py --start 2025-05-01T00:00:00Z --end 2025-05-31T23:59:59Z --output "C:\Invoices\Invoice May.docx" --no-pdf
```

### Arguments

| Flag       | Description                      |
| ---------- | -------------------------------- |
| `--start`  | Start date (ISO8601 format)      |
| `--end`    | End date (ISO8601 format)        |
| `--output` | Full path to output `.docx` file |
| `--no-pdf` | Skip PDF generation              |
| `--config` | Path to alternate `.env` file    |

``````