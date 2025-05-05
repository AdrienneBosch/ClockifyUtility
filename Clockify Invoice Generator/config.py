import os
import requests
from dotenv import load_dotenv
from exceptions import ConfigError, ApiError
from logger import get_logger

REQUIRED_KEYS = [
    "CLOCKIFY_API_KEY",
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

OPTIONAL_KEYS = ["USER_ID", "WORKSPACE_ID"]

logger = get_logger()

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

    api_key = config["CLOCKIFY_API_KEY"]

    user_id = os.getenv("USER_ID")
    if not user_id:
        user_id = fetch_user_id(api_key)
        logger.info("USER_ID not set in .env")
        logger.info(f"→ Your Clockify USER_ID is: {user_id}")
        logger.info("Please copy this ID and add it to your .env file as USER_ID=")
    config["USER_ID"] = user_id

    workspace_id = os.getenv("WORKSPACE_ID")
    if not workspace_id:
        workspace_id = fetch_workspace_id(api_key)
        logger.info("WORKSPACE_ID not set in .env")
        logger.info(f"→ Your Clockify WORKSPACE_ID is: {workspace_id}")
        logger.info("Please copy this ID and add it to your .env file as WORKSPACE_ID=")
        config["WORKSPACE_ID"] = workspace_id
    else:
        config["WORKSPACE_ID"] = workspace_id

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

def fetch_workspace_id(api_key: str):
    headers = {"X-Api-Key": api_key}
    url = "https://api.clockify.me/api/v1/workspaces"

    try:
        resp = requests.get(url, headers=headers)
        resp.raise_for_status()
        workspaces = resp.json()
        if not workspaces:
            raise ApiError("No workspaces returned from Clockify.")
        return workspaces[0]["id"]
    except Exception as e:
        raise ApiError(f"Could not retrieve WORKSPACE_ID from Clockify API: {e}")
