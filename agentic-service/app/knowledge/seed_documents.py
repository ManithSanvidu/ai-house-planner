"""
Architecture knowledge base seed documents.
Trusted, curated content for RAG retrieval across all supported categories.
"""
from __future__ import annotations

KNOWLEDGE_DOCUMENTS = [
    # ─── RESIDENTIAL PLANNING ───
    {
        "category": "residential_planning",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Room Sizing Guidelines for Sri Lankan Homes",
        "content": (
            "Standard room sizing for Sri Lankan residential homes: "
            "Master bedroom: 12x14 ft (168 sq ft) minimum, ideally 14x16 ft. "
            "Secondary bedrooms: 10x12 ft (120 sq ft) minimum. "
            "Living room: 12x16 ft (192 sq ft) for small homes, 14x20 ft for medium homes. "
            "Kitchen: 8x10 ft minimum for a compact kitchen, 10x12 ft for a standard kitchen with dining prep area. "
            "Dining room: 10x12 ft for 4-6 seaters, 12x14 ft for 6-8 seaters. "
            "Bathrooms: 5x7 ft minimum for a standard bathroom, 8x10 ft for an attached/ensuite bathroom. "
            "These sizes ensure comfortable movement, furniture placement, and adequate ventilation."
        ),
    },
    {
        "category": "residential_planning",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Minimum Land Sizes for House Types",
        "content": (
            "For Sri Lankan residential construction: "
            "A 2-bedroom single-floor home typically needs 6-10 perches minimum. "
            "A 3-bedroom single-floor home needs 10-15 perches. "
            "A 4-bedroom two-floor home can work on 10-15 perches due to vertical expansion. "
            "A 5-6 bedroom home generally requires 15-25 perches or more depending on floor count. "
            "1 perch equals 272.25 sq ft. The UDA (Urban Development Authority) recommends a maximum ground coverage of 65% for residential zones. "
            "Setbacks, parking, garden area, and access paths reduce the effective buildable area significantly."
        ),
    },
    # ─── CIRCULATION ───
    {
        "category": "circulation",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Internal Circulation and Hallway Design",
        "content": (
            "Corridors and hallways should be at least 3.5 ft (1.07 m) wide for comfortable single-person passage, "
            "and 4.5 ft for wheelchair accessibility. "
            "An efficient home design limits circulation area to 15-20% of total floor area. "
            "Excessive hallways waste space and increase construction costs. "
            "The best layouts use open-plan zones or central-core designs to minimize corridors. "
            "Staircase width should be minimum 3 ft clear, with a landing every 12-14 risers. "
            "A standard staircase in a two-floor home occupies approximately 35-45 sq ft per floor."
        ),
    },
    # ─── ZONING ───
    {
        "category": "zoning",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Residential Zoning and Privacy",
        "content": (
            "Good house design separates public zones (living, dining, kitchen) from private zones (bedrooms, bathrooms). "
            "The public zone should typically face the entrance and road side. "
            "Private zones should be placed towards the rear or upper floors. "
            "Service areas (kitchen, utility, laundry) should be clustered near the kitchen for plumbing efficiency. "
            "In Sri Lankan homes, a separate guest bedroom near the entrance is common cultural practice. "
            "Buffer zones like hallways or stairwells between public and private areas enhance acoustic privacy."
        ),
    },
    # ─── DAYLIGHT ───
    {
        "category": "daylight",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Natural Daylight and Window Placement",
        "content": (
            "Sri Lanka is located near the equator, so the sun travels almost directly overhead. "
            "South and north-facing windows receive consistent indirect light throughout the year. "
            "East-facing rooms get morning sunlight (ideal for bedrooms). "
            "West-facing rooms get harsh afternoon sun (avoid large windows or use shading). "
            "Window area should be at least 10-15% of the floor area of the room for adequate daylight. "
            "Clerestory windows and light shelves can bring daylight deeper into rooms without direct glare. "
            "For Sri Lankan climates, shaded verandas and deep eaves help control excessive sunlight."
        ),
    },
    # ─── VENTILATION ───
    {
        "category": "ventilation",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Natural Ventilation for Tropical Homes",
        "content": (
            "Cross-ventilation is essential in tropical Sri Lankan homes to reduce reliance on mechanical cooling. "
            "Rooms should have openings on at least two opposite or adjacent walls. "
            "The prevailing wind in most of Sri Lanka comes from the southwest during the southwest monsoon (May-September) "
            "and from the northeast during the northeast monsoon (December-February). "
            "Ventilation openings should be at least 15-20% of the floor area. "
            "High-level openings (clerestory or louvres) allow hot air to escape via the stack effect. "
            "Courtyards and internal gardens create thermal chimneys that enhance natural air circulation."
        ),
    },
    {
        "category": "ventilation",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Bathroom and Kitchen Ventilation",
        "content": (
            "Bathrooms must have ventilation to prevent mould and moisture damage. "
            "Minimum bathroom ventilation: a window of at least 1.5 sq ft or a mechanical exhaust fan. "
            "Kitchens produce heat, smoke, and moisture — a chimney hood or exhaust fan above the cooking area is recommended. "
            "In open-plan kitchens, ensure the cooking zone is near an external wall with a window or vent. "
            "Internal bathrooms without windows need mechanical ventilation rated at 8 air changes per hour minimum."
        ),
    },
    # ─── ORIENTATION ───
    {
        "category": "orientation",
        "source": "HomePlannerAI Knowledge Base",
        "title": "House Orientation for Sri Lankan Climate",
        "content": (
            "For optimal comfort in Sri Lanka, orient the longer axis of the house along the east-west direction. "
            "This minimizes direct sun exposure on the larger wall surfaces. "
            "Place bedrooms on the east or north side for cooler sleeping conditions. "
            "Avoid placing large glass facades on the west side. "
            "The entrance is traditionally placed facing the road, but cultural Vastu/Feng Shui preferences may also apply. "
            "North-facing plots are considered auspicious in many Sri Lankan traditions. "
            "Solar panels, if planned, should face south in Sri Lanka for maximum exposure."
        ),
    },
    # ─── LAND PLANNING ───
    {
        "category": "land_planning",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Plot Setbacks and Building Lines",
        "content": (
            "Standard setback guidelines for Sri Lankan residential plots: "
            "Front setback (from road boundary): 10 ft minimum for minor roads, 15 ft for major roads. "
            "Rear setback: 5-7 ft minimum. "
            "Side setbacks: 3-5 ft minimum on each side. "
            "These are conceptual planning defaults and may vary by local authority regulations. "
            "Setbacks serve fire safety, light access, drainage, and privacy purposes. "
            "Always verify actual setback requirements with the local Pradeshiya Sabha or Municipal Council."
        ),
    },
    {
        "category": "land_planning",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Parking and Driveway Planning",
        "content": (
            "A standard car parking space requires approximately 9x18 ft (162 sq ft). "
            "A driveway approach needs at least 10 ft width for a single car. "
            "For homes on smaller plots (under 10 perches), carport or front-setback parking is common. "
            "For larger plots, a dedicated garage (12x20 ft minimum) can be integrated. "
            "When parking is in the front setback, the front setback is effectively increased to 18 ft minimum. "
            "Turning radius for cars requires about 20 ft of clear space."
        ),
    },
    # ─── FOUNDATION TYPES ───
    {
        "category": "foundation_types",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Foundation Types for Residential Construction",
        "content": (
            "Common foundation types for Sri Lankan residential buildings: "
            "1. Strip foundation (continuous footing): Most common for single and two-story houses on flat terrain with good soil. Depth 2-3 ft, width 1.5-2 ft. "
            "2. Pad/isolated footing: Used for column-based structures. Suitable for framed construction. "
            "3. Raft/mat foundation: Used on weak or marshy soil. Distributes load over the entire footprint. More expensive. "
            "4. Pile foundation: Required for very soft soils or hillside construction. Transfers load to deeper, stable soil layers. "
            "The choice depends on soil bearing capacity, number of floors, and terrain type. "
            "A soil investigation report is strongly recommended before deciding on foundation type."
        ),
    },
    # ─── CONSTRUCTION STAGES ───
    {
        "category": "construction_stages",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Residential Construction Stages",
        "content": (
            "Typical construction stages for a Sri Lankan residential house: "
            "1. Site preparation and clearing. "
            "2. Setting out and foundation excavation. "
            "3. Foundation construction (footings, columns to plinth level). "
            "4. Plinth beam and filling. "
            "5. Ground floor slab casting. "
            "6. Ground floor wall construction (brick/block work). "
            "7. Lintel beams over doors and windows. "
            "8. If multi-story: first floor slab casting, then repeat wall construction. "
            "9. Roof structure (timber truss or steel) and roofing (clay tiles, asbestos, or metal sheets). "
            "10. Plumbing and electrical rough-in. "
            "11. Plastering and finishing. "
            "12. Tiling, painting, and fixtures. "
            "13. External works (boundary wall, landscaping, driveway). "
            "A typical 2-bedroom single-floor house takes 6-10 months. A two-floor house takes 10-16 months."
        ),
    },
    {
        "category": "construction_stages",
        "source": "HomePlannerAI Knowledge Base",
        "title": "What Comes After Foundation",
        "content": (
            "After the foundation is completed, the next construction stages are: "
            "1. Plinth beam construction — a reinforced concrete beam at ground level connecting all foundation columns. "
            "2. Plinth filling — the area within the plinth is filled with compacted earth or rubble. "
            "3. Ground floor slab — a reinforced concrete slab cast over the filled plinth. "
            "4. Ground floor walls — brick or block masonry walls built on top of the slab. "
            "The plinth beam is critical as it ties the structure together and prevents differential settlement. "
            "Curing of the slab should be done for at least 7 days with regular water spraying."
        ),
    },
    # ─── MATERIALS ───
    {
        "category": "materials",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Common Building Materials in Sri Lanka",
        "content": (
            "Common wall materials: burnt clay bricks (most traditional), cement blocks (cost-effective), "
            "autoclaved aerated concrete (AAC) blocks (lightweight, good insulation). "
            "Roofing: clay tiles (traditional, good thermal), cement fiber sheets, metal roofing (calicut sheets). "
            "Structural: reinforced concrete for columns, beams, and slabs. Steel reinforcement bars (TMT/CRS). "
            "Flooring: ceramic tiles (most common), granite tiles (premium), polished cement (budget-friendly). "
            "Doors: teak wood (premium), mahogany, jack wood, or aluminium frames with glass. "
            "Windows: aluminium sliding (most common), timber casement (traditional), uPVC (modern, low maintenance). "
            "Prices vary significantly by region and market conditions."
        ),
    },
    # ─── COMMON DESIGN MISTAKES ───
    {
        "category": "common_design_mistakes",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Common House Design Mistakes to Avoid",
        "content": (
            "1. Ignoring ventilation: Rooms without cross-ventilation become uncomfortably hot in tropical climates. "
            "2. Oversized corridors: Excessive hallway space wastes floor area and increases costs. "
            "3. Poor kitchen placement: Kitchens should be near dining areas and have external ventilation. "
            "4. Neglecting storage: Adequate built-in wardrobes, linen cupboards, and utility storage are often overlooked. "
            "5. Undersized bathrooms: Bathrooms smaller than 5x7 ft are impractical. "
            "6. Ignoring future expansion: Not planning for vertical expansion (adding a floor later) is a common regret. "
            "7. Too many small rooms instead of fewer well-sized rooms. "
            "8. Placing bedrooms on the west side without adequate shading. "
            "9. Not considering furniture layout when sizing rooms."
        ),
    },
    # ─── ACCESSIBILITY ───
    {
        "category": "accessibility",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Accessible Home Design Principles",
        "content": (
            "Accessible home design ensures comfort for elderly residents and those with mobility limitations. "
            "Key principles: "
            "- At least one bedroom and one bathroom on the ground floor. "
            "- Doorways at least 3 ft (36 inches) wide for wheelchair clearance. "
            "- Corridors at least 4 ft wide. "
            "- Ramps instead of steps at entrances (1:12 gradient maximum). "
            "- Grab bars in bathrooms. "
            "- Non-slip flooring in wet areas. "
            "- Light switches and electrical outlets at accessible heights (3-4 ft from floor). "
            "- Level thresholds between rooms. "
            "An accessible layout does not significantly increase construction cost if planned from the start."
        ),
    },
    # ─── CONSTRUCTION SCHEDULING ───
    {
        "category": "construction_scheduling",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Construction Timeline Estimation",
        "content": (
            "Typical construction timelines for Sri Lankan residential projects: "
            "2-bedroom single floor: 6-10 months. "
            "3-bedroom single floor: 8-12 months. "
            "3-bedroom two floor: 10-14 months. "
            "4-bedroom two floor: 12-16 months. "
            "5-6 bedroom two floor: 14-20 months. "
            "Key factors affecting timeline: weather (monsoon delays), material availability, "
            "contractor capacity, approval delays, and payment schedule. "
            "Foundation stage: 4-6 weeks. Structure (walls, slabs): 3-6 months. "
            "Finishing (plaster, tiles, paint, fixtures): 2-4 months. "
            "External works: 2-4 weeks. "
            "Plan for at least 20% buffer time for unexpected delays."
        ),
    },
    {
        "category": "construction_scheduling",
        "source": "HomePlannerAI Knowledge Base",
        "title": "Best Time to Start Construction in Sri Lanka",
        "content": (
            "The best time to start construction in Sri Lanka is during the dry inter-monsoon periods: "
            "January to March (dry season between northeast and southwest monsoons). "
            "July to September (inter-monsoon). "
            "Avoid starting foundation work during heavy monsoon months (October-December in the west/south, "
            "November-February in the north/east). "
            "Rain delays foundation excavation, concrete curing, and brickwork. "
            "If starting during monsoon season, ensure proper site drainage and temporary covers for curing concrete."
        ),
    },
]
