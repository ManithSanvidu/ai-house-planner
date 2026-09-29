import io
from PIL import Image, ImageDraw

def render_blueprint(layout_json: dict) -> bytes:
    rooms = layout_json.get("rooms", [])
    if not rooms:
        img = Image.new("RGB", (800, 600), "white")
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        return buf.getvalue()
        
    # Scale calculation
    min_x = min(r.get("x", 0) for r in rooms)
    min_y = min(r.get("y", 0) for r in rooms)
    max_x = max(r.get("x", 0) + r.get("width", 0) for r in rooms)
    max_y = max(r.get("y", 0) + r.get("length", r.get("height", 0)) for r in rooms)
    
    width_ft = max_x - min_x
    height_ft = max_y - min_y
    if width_ft <= 0: width_ft = 10
    if height_ft <= 0: height_ft = 10
    
    scale = min(1000 / width_ft, 1000 / height_ft)
    padding = 100
    
    img_width = int(width_ft * scale) + padding * 2
    img_height = int(height_ft * scale) + padding * 2
    
    img = Image.new("RGB", (img_width, img_height), "white")
    draw = ImageDraw.Draw(img)
    
    # Draw title (room count)
    beds = sum(1 for r in rooms if 'bedroom' in str(r.get("room_type", "")).lower())
    baths = sum(1 for r in rooms if 'bath' in str(r.get("room_type", "")).lower())
    title = f"Blueprint - {beds} Bedrooms, {baths} Bathrooms ({len(rooms)} Rooms Total)"
    draw.text((padding, padding // 4), title, fill="black")
    
    # Draw rooms
    for r in rooms:
        rx = r.get("x", 0) - min_x
        # invert y so standard cartesian works logically, or just keep as is
        # typical top-down images have y=0 at top
        ry = r.get("y", 0) - min_y
        rw = r.get("width", 0)
        rh = r.get("length", r.get("height", 0))
        
        px0 = int(rx * scale) + padding
        # invert y: Image origin is top-left, coordinates are usually bottom-left.
        # to properly map, we subtract from max_y
        py0 = int((height_ft - (ry + rh)) * scale) + padding
        px1 = int((rx + rw) * scale) + padding
        py1 = int((height_ft - ry) * scale) + padding
        
        draw.rectangle([px0, py0, px1, py1], outline="black", width=3, fill="#f0f0f0")
        
        name = r.get("name") or r.get("room_type", "Room").replace("_", " ").title()
        
        # Center label
        cx = (px0 + px1) / 2
        cy = (py0 + py1) / 2
        
        draw.text((cx - 30, cy - 10), name, fill="black")
        
        # Draw doors if available
        doors = r.get("doors", [])
        for door in doors:
            wall = door.get("wall")
            offset = door.get("offset", 0)
            door_w = door.get("width", 3)
            
            if wall == "north":
                dx0 = int((rx + offset) * scale) + padding
                dy0 = py0 - 3
                dx1 = int((rx + offset + door_w) * scale) + padding
                dy1 = py0 + 3
            elif wall == "south":
                dx0 = int((rx + offset) * scale) + padding
                dy0 = py1 - 3
                dx1 = int((rx + offset + door_w) * scale) + padding
                dy1 = py1 + 3
            elif wall == "east":
                dx0 = px1 - 3
                dy0 = int((height_ft - (ry + offset + door_w)) * scale) + padding
                dx1 = px1 + 3
                dy1 = int((height_ft - (ry + offset)) * scale) + padding
            elif wall == "west":
                dx0 = px0 - 3
                dy0 = int((height_ft - (ry + offset + door_w)) * scale) + padding
                dx1 = px0 + 3
                dy1 = int((height_ft - (ry + offset)) * scale) + padding
            else:
                continue
                
            draw.rectangle([dx0, dy0, dx1, dy1], fill="white")
    
    buf = io.BytesIO()
    img.save(buf, format="PNG")
    return buf.getvalue()
