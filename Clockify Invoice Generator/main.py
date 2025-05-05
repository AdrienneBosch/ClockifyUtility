import sys
from cli import parse_args
from config import get_config
from utils import get_month_range
from generator import generate_invoice_workflow
from logger import get_logger

def main():
    args = parse_args()
    logger = get_logger()

    try:
        config = get_config(args.config)

        if config is None:
            logger.info("Configuration incomplete. Exiting without generating invoice.")
            sys.exit(0)

        start = args.start
        end = args.end

        if not start or not end:
            start, end = get_month_range()

        generate_invoice_workflow(
            start=start,
            end=end,
            output_path=args.output,
            generate_pdf=not args.no_pdf,
            config=config
        )

    except Exception as e:
        logger.error(f"Invoice generation failed: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
