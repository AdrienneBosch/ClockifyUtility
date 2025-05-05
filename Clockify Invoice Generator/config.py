import os
import requests
from dotenv import load_dotenv
from exceptions import ConfigError, ApiError

REQUIRED_KEYS = [
    "CLOCKIFY_API_KEY",
    "WORKSPACE_ID",
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

OPTIONAL_KEYS = [
    "USER_ID"
]

def get_config(env_file: str):
    load_dotenv(env_file)

    config = {}
    missing = []

    for key in REQUIRED_KEYS:
        val = os.getenv(key)
        if not val:
            missing.append(key)
        else:
            config[key] = val

    if missing:
        raise ConfigError(f"Missing required environment variables: {', '.join(missing)}")

    user_id = os.getenv("USER_ID")
    if not user_id:
        user_id = fetch_user_id(config["CLOCKIFY_API_KEY"])
    config["USER_ID"] = user_id

    return config

def fetch_user_id(api_key: str):
    headers = {"X-Api-Key": api_key}
    url = "https://api.clockify.me/api/v1/user"

    try:
        resp = requests.get(url, headers=headers)
        resp.raise_for_status()
        return resp.json().get("id")
    except Exception as e:
        raise ApiError(f"Could not retrieve USER_ID from Clockify API: {e}")
