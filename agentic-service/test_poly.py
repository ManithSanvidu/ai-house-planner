from shapely.geometry import Polygon
from shapely.ops import unary_union
rooms = [
    (0, 0, 11, 13),
    (0, 13, 11, 10.4),
    (11, 0, 8, 10),
    (11, 10, 8, 6),
    (11, 16, 8, 7.4),
    (19, 0, 11, 13),
    (19, 13, 11, 10.4),
]
polys = [Polygon([(x, y), (x+w, y), (x+w, y+l), (x, y+l)]) for x, y, w, l in rooms]
for p in polys:
    print(p.area)
union = unary_union(polys)
print(union.area)
