import os
import json
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
    workspace_id = os.getenv("WORKSPACE_ID")

    needs_exit = False

    if not user_id:
        try:
            user_id = fetch_user_id(api_key)
            logger.info("USER_ID not set in .env")
            logger.info(f"→ Your Clockify USER_ID is: {user_id}")
            logger.info("Please copy this ID and add it to your .env file as USER_ID=")
            needs_exit = True
        except ApiError as e:
            logger.error(str(e))
            needs_exit = True

    if not workspace_id:
        try:
            workspaces = fetch_workspace_ids(api_key)
            logger.info("WORKSPACE_ID not set in .env")
            logger.info("→ Available Clockify WORKSPACE_IDs:")
            for w in workspaces:
                logger.info(f"- {w['name']}: {w['id']}")
            logger.info("Please copy the appropriate ID and add it to your .env file as WORKSPACE_ID=")
            needs_exit = True
        except ApiError as e:
            logger.error(str(e))
            needs_exit = True

    if needs_exit:
        return None

    config["USER_ID"] = user_id
    config["WORKSPACE_ID"] = workspace_id

    config["CLIENT_NAME"] = os.environ["CLIENT_NAME"]
    config["CLIENT_ADDRESS_1"] = os.environ["CLIENT_ADDRESS_1"]
    config["CLIENT_ADDRESS_2"] = os.environ["CLIENT_ADDRESS_2"]
    config["CLIENT_ADDRESS_3"] = os.environ["CLIENT_ADDRESS_3"]
    config["CLIENT_EMAIL_ADDRESS"] = os.environ["CLIENT_EMAIL_ADDRESS"]
    config["CLIENT_TEXT_NUMBER"] = os.environ["CLIENT_TEXT_NUMBER"]
    config["CLIENT_NUMBER"] = os.environ["CLIENT_NUMBER"]

    config["CURRENCY_SYMBOL"] = os.getenv("CURRENCY_SYMBOL", "$")
    config["TABLE_STYLE"] = os.getenv("TABLE_STYLE", "Medium Shading 1 Accent 2")
    config["TITLE_COLOR"] = os.getenv("TITLE_COLOR", "c0504d")

    output_path = os.getenv("OUTPUT_PATH", "output").strip()
    config["OUTPUT_PATH"] = os.path.abspath(output_path)

    config["TITLE_FONT_SIZE"] = int(os.getenv("TITLE_FONT_SIZE", 24))
    config["BODY_FONT_SIZE"] = int(os.getenv("BODY_FONT_SIZE", 11))
    config["SEPARATOR_COLOR"] = os.getenv("SEPARATOR_COLOR", "c0504d")
    config["SEPARATOR_SIZE"] = int(os.getenv("SEPARATOR_SIZE", 6))
    config["HEADER_SPACING_BEFORE"] = int(os.getenv("HEADER_SPACING_BEFORE", 7))
    config["HEADER_SPACING_AFTER"] = int(os.getenv("HEADER_SPACING_AFTER", 10))
    config["BODY_SPACING_BEFORE"] = int(os.getenv("BODY_SPACING_BEFORE", 1))
    config["BODY_SPACING_AFTER"] = int(os.getenv("BODY_SPACING_AFTER", 2))

    try:
        heading_sizes = os.getenv("HEADING_FONT_SIZES", '{"1": 18, "2": 16, "3": 14, "4": 12}')
        config["HEADING_FONT_SIZES"] = {int(k): int(v) for k, v in json.loads(heading_sizes).items()}
    except (json.JSONDecodeError, ValueError):
        raise ConfigError("Invalid JSON format in HEADING_FONT_SIZES")

    try:
        line_items_raw = os.getenv("CONSTANT_LINE_ITEMS", "[]")
        config["CONSTANT_LINE_ITEMS"] = json.loads(line_items_raw)
    except json.JSONDecodeError:
        raise ConfigError("Invalid JSON in CONSTANT_LINE_ITEMS")

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

def fetch_workspace_ids(api_key: str):
    headers = {"X-Api-Key": api_key}
    url = "https://api.clockify.me/api/v1/workspaces"

    try:
        resp = requests.get(url, headers=headers)
        resp.raise_for_status()
        return resp.json()
    except Exception as e:
        raise ApiError(f"Could not retrieve WORKSPACE_IDs from Clockify API: {e}")
