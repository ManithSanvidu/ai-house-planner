from sympy.geometry import Polygon, Point
rooms = [
    (0, 0, 11, 13),
    (0, 13, 11, 10.4),
    (11, 0, 8, 10),
    (11, 10, 8, 6),
    (11, 16, 8, 7.4),
    (19, 0, 11, 13),
    (19, 13, 11, 10.4),
]
for x, y, w, l in rooms:
    p = Polygon(Point(x, y), Point(x+w, y), Point(x+w, y+l), Point(x, y+l))
    print(p.area)
