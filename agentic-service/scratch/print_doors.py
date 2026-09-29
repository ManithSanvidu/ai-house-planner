import asyncio
from scratch.geometry_generator_temp import LayoutSolver, generate_geometry
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.spatial_program import SpatialProgram, RoomIntent, EntranceIntent, VerticalCoreIntent
from app.design.geometry.finishing import finish_generative_layout

def run():
    plot = PlotConstraints(land_size_perches=20, plot_width_ft=80, plot_length_ft=80, road_side="south")
    program = SpatialProgram(
        concept="Family", floor_count=2,
        rooms=[
            RoomIntent(id="living", type="living", floor=1, zone="PUBLIC", target_area_sqft=300, min_area_sqft=200, preferred_position="FRONT", exterior_wall_required=True, privacy_level="LOW"),
            RoomIntent(id="kitchen", type="kitchen", floor=1, zone="SERVICE", target_area_sqft=150, min_area_sqft=100, preferred_position="CENTER", exterior_wall_required=True, privacy_level="LOW"),
            RoomIntent(id="dining", type="dining", floor=1, zone="PUBLIC", target_area_sqft=150, min_area_sqft=100, preferred_position="CENTER", exterior_wall_required=False, privacy_level="LOW"),
            RoomIntent(id="bath1", type="bathroom", floor=1, zone="SERVICE", target_area_sqft=50, min_area_sqft=40, preferred_position="CENTER", exterior_wall_required=False, privacy_level="LOW"),
            RoomIntent(id="bed1", type="bedroom", floor=1, zone="PRIVATE", target_area_sqft=150, min_area_sqft=100, preferred_position="REAR", exterior_wall_required=True, privacy_level="MEDIUM"),
            RoomIntent(id="bed2", type="bedroom", floor=2, zone="PRIVATE", target_area_sqft=150, min_area_sqft=100, preferred_position="FRONT", exterior_wall_required=True, privacy_level="HIGH"),
            RoomIntent(id="bed3", type="bedroom", floor=2, zone="PRIVATE", target_area_sqft=150, min_area_sqft=100, preferred_position="FRONT", exterior_wall_required=True, privacy_level="HIGH"),
            RoomIntent(id="bed4", type="bedroom", floor=2, zone="PRIVATE", target_area_sqft=200, min_area_sqft=150, preferred_position="REAR", exterior_wall_required=True, privacy_level="HIGH"),
            RoomIntent(id="bath2", type="bathroom", floor=2, zone="SERVICE", target_area_sqft=50, min_area_sqft=40, preferred_position="CENTER", exterior_wall_required=False, privacy_level="LOW"),
            RoomIntent(id="bath3", type="bathroom", floor=2, zone="SERVICE", target_area_sqft=50, min_area_sqft=40, preferred_position="CENTER", exterior_wall_required=False, privacy_level="LOW")
        ],
        adjacencies=[],
        entrance=EntranceIntent(preferred_side="SOUTH", connect_to="living"),
        vertical_core=VerticalCoreIntent(stair_position="CENTER", align_service_zones=False),
        reason_codes=[]
    )
    
    solver = LayoutSolver(program, plot)
    cands = solver.generate()
    for i, (c, _) in enumerate(cands[:5]):
        print(f"\nCandidate {i}:")
        for r in c.rooms:
            if r.floor == 2:
                print(f"{r.room_id}: x={r.x}, y={r.y}, w={r.width}, l={r.length}")

if __name__ == "__main__":
    run()
