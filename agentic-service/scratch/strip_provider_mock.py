import os
import glob

def strip_provider():
    test_files = glob.glob('tests/**/*.py', recursive=True)
    for file in test_files:
        with open(file, 'r') as f:
            lines = f.readlines()
        
        new_lines = []
        changed = False
        for line in lines:
            if 'get_available_design_provider' in line and ('monkeypatch' in line or 'patch' in line):
                changed = True
                continue
            if 'from app.design.generation.generation_service import get_available_design_provider' in line:
                changed = True
                continue
            new_lines.append(line)
            
        if changed:
            with open(file, 'w') as f:
                f.writelines(new_lines)
            print(f"Cleaned {file}")

if __name__ == '__main__':
    strip_provider()
