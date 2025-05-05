import argparse

def parse_args():
    parser = argparse.ArgumentParser(
        description="Generate invoices from Clockify data."
    )

    parser.add_argument(
        "--start",
        help="ISO8601 start date (e.g. 2025-04-01T00:00:00Z)",
        default=None,
    )

    parser.add_argument(
        "--end",
        help="ISO8601 end date (e.g. 2025-04-30T23:59:59Z)",
        default=None,
    )

    parser.add_argument(
        "--output",
        help="Optional output file path (.docx or .pdf). Default is auto-generated.",
        default=None,
    )

    parser.add_argument(
        "--no-pdf",
        help="Disable PDF generation.",
        action="store_true",
    )

    parser.add_argument(
        "--config",
        help="Path to .env file",
        default=".env",
    )

    return parser.parse_args()
