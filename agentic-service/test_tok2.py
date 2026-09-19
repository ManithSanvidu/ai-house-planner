from app.tools.layout_generation_tool import generate_layout
preferences = {
    'bedrooms': 3,
    'style': 'modern',
    'design_seed': 42,
    'plot_constraints': {
        'plot_width_ft': 50.0,
        'plot_length_ft': 50.0
    }
}
try:
    generate_layout(10.0, 'flat', preferences, design_seed=42)
except Exception as e:
    print(repr(e))
