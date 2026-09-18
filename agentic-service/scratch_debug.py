import sys
from app.tools.layout_generation_tool import generate_layout, prepare_inputs

def test():
    preferences = {
        'bedrooms': 3,
        'bathrooms': 2,
        'floors': 1,
        'style': 'modern',
        'road_side': 'south',
        'design_seed': 42
    }
    try:
        generate_layout(8, 'flat', preferences, design_seed=42)
    except Exception as e:
        print(f"Exception: {e}")

if __name__ == "__main__":
    test()
