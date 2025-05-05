import os
from dotenv import load_dotenv
from exceptions import ConfigError

REQUIRED_KEYS = [
    "CLOCKIFY_API_KEY",
    "WORKSPACE_ID",
    "USER_ID",
    "HOURLY_RATE",
    "FROM_NAME",
    "COMPANY_ADDRESS_LINE1",
    "COMPANY_ADDRESS_LINE2",
    "COMPANY_ADDRESS_LINE3",
    "CONTACT_EMAIL",
    "CONTACT_PHONE",
    "BANK_NAME",
    "BANK_ACCOUNT_NUMBER",
    "BANK_ACCOUNT_HOLDER",
    "BANK_ROUTING_NUMBER",
    "BANK_SWIFT"
]

def get_config(env_file: str):
    load_dotenv(env_file)

    missing = [key for key in REQUIRED_KEYS if not os.getenv(key)]
    if missing:
        raise ConfigError(f"Missing required environment variables: {', '.join(missing)}")

    return {key: os.getenv(key) for key in REQUIRED_KEYS}
