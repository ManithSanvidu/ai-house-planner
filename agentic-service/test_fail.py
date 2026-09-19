from app.tools.layout_generation_tool import generate_layout
import traceback
try:
    generate_layout(
        land_size_perches=20.0,
        terrain_type="flat",
        preferences={"bedrooms": 4, "floors": 2}
    )
except Exception as e:
    traceback.print_exc()
