import os
from dotenv import load_dotenv

load_dotenv()

ASPNET_API_URL = os.getenv("ASPNET_API_URL", "http://localhost:5000")
INTERNAL_API_KEY = os.getenv("INTERNAL_API_KEY", "default_internal_api_key")
GOOGLE_API_KEY = os.getenv("GOOGLE_API_KEY", os.getenv("GEMINI_API_KEY", ""))
GEMINI_API_KEY = GOOGLE_API_KEY
