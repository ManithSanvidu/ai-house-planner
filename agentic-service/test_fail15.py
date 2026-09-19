from app.design.adjacency import shared_wall
from app.schemas.design_result import RoomLayout

hall_1 = RoomLayout(room_id="hall_1", room_type="hallway", floor=1, x=16, y=10, width=3, length=10)
stair_1 = RoomLayout(room_id="stair_1", room_type="staircase", floor=1, x=11, y=20, width=8, length=10)
print("hall_1 and stair_1:", shared_wall(hall_1, stair_1))

bed_1 = RoomLayout(room_id="bed_1", room_type="bedroom_1", floor=1, x=0, y=13, width=11, length=17)
print("bed_1 and stair_1:", shared_wall(bed_1, stair_1))
