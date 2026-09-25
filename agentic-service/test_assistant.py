import sys
import os

# add agentic-service to python path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__))))

from app.agents.architecture_assistant import interpret_user_message

messages = [
    "I have 25 perch land. How many bedrooms are suitable?",
    "Design me a 4 bedroom two floor modern house on 25 perch land",
    "What comes after foundation?",
    "dfjsklfjdl"
]

for m in messages:
    print(f"\n--- Testing: {m} ---")
    res = interpret_user_message(m)
    print(res)
    print("--------------------------------------------------")
