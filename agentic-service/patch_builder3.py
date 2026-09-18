with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'r') as f:
    lines = f.readlines()

new_lines = []
for i, line in enumerate(lines):
    if i == 84: # line 85 (0-indexed)
        new_lines.append('    for f in range(2, floors + 1):\n')
    else:
        new_lines.append(line)

with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'w') as f:
    f.writelines(new_lines)
