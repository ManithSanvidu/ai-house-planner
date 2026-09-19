from app.design.adjacency import build_connections
from app.schemas.design_result import RoomLayout

hall_1 = RoomLayout(room_id="hall_1", room_type="hallway", floor=1, x=16, y=10, width=3, length=10)
stair_1 = RoomLayout(room_id="stair_1", room_type="staircase", floor=1, x=11, y=20, width=8, length=10)
print(build_connections([hall_1, stair_1], False))
