import re
with open('app/design/architectural_quality.py', 'r') as f:
    code = f.read()

code = code.replace(
    "if bed_distance > config.bedroom_travel_max_ft:",
    "print('ACTUAL bed_distance:', bed_distance, 'max:', config.bedroom_travel_max_ft)\n    if bed_distance > config.bedroom_travel_max_ft:"
)
with open('app/design/architectural_quality.py', 'w') as f:
    f.write(code)
