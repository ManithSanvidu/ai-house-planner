import os
import re

test_dir = 'tests'

for filename in os.listdir(test_dir):
    if not filename.endswith('.py'): continue
    filepath = os.path.join(test_dir, filename)
    with open(filepath, 'r') as f:
        content = f.read()

    # Replace manual_terrain_type, preferences, plot_constraints
    content = re.sub(r'manual_terrain_type=[^,]+,', '', content)
    content = re.sub(r'preferences=[^,]+,', 'bedrooms=3,\n            house_type="modern",', content)
    content = re.sub(r'plot_constraints=[^,]+,', '', content)
    # also remove any dangling empty preferences kwargs
    content = re.sub(r'preferences=\{[^\}]*\},?', 'bedrooms=3, house_type="modern",', content)

    with open(filepath, 'w') as f:
        f.write(content)
print("Done")
