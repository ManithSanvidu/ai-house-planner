import re
with open('app/design/adjacency.py', 'r') as f:
    code = f.read()

code = code.replace(
    "{ak, bk} <= {'living_room', 'dining', 'kitchen'}",
    "{ak, bk} <= {'living_room', 'dining', 'kitchen', 'family_room', 'family_lounge', 'study', 'home_office'}"
)

with open('app/design/adjacency.py', 'w') as f:
    f.write(code)
print("Patched adjacency!")
