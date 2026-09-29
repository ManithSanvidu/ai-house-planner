import time
from typing import Any
from pydantic import BaseModel
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.spatial_program import SpatialProgram
from app.schemas.design_result import DesignResult, RoomLayout
from app.design.exceptions import GenerationFailure
from app.validation.geometry_validator import validate_geometry
from app.design.program.room_rules import rule_for

def _normalize_room_type(gpt_type: str) -> str:
    t = gpt_type.lower().replace(' ', '_')
    if t == 'living': return 'living_room'
    if t == 'dining': return 'dining'
    if t == 'bath': return 'bathroom'
    if t == 'master_bedroom': return 'bedroom_master'
    return t

class PlacedRoom:
    def __init__(self, room_id: str, rtype: str, floor: int, x: float, y: float, w: float, h: float, target_area: float):
        self.id = room_id
        self.type = rtype
        self.floor = floor
        self.x = x
        self.y = y
        self.width = w
        self.length = h
        self.target_area = target_area

class LayoutSolver:
    def __init__(self, program, plot):
        self.program = program
        self.plot = plot
        self.env_w = plot.buildable_width
        self.env_l = plot.buildable_length
        self.global_evals = 0
        self.global_backtracks = 0
        self.MAX_BACKTRACKS = 20000
        self.stair_candidates_evaluated = 0
        self.MAX_CANDIDATES = 200
        self.candidates = []
        
        from app.design.program.spatial_program import RoomIntent
        from app.design.program.room_rules import room_kind
        
        self.program_rooms = list(self.program.rooms)
        self.hallways_added = 0
        
        for floor in range(1, self.program.floor_count + 1):
            floor_rooms = [r for r in self.program_rooms if r.floor == floor]
            has_circ = any(room_kind(r.type) in ('hallway', 'foyer', 'living_room', 'dining', 'landing') for r in floor_rooms)
            private_rooms = [r for r in floor_rooms if room_kind(r.type) in ('bedroom', 'bathroom', 'home_office')]
            
            if len(private_rooms) > 1 and not has_circ:
                num_hallways = 2 if len(private_rooms) >= 3 else 1
                hallway_ids = []
                for i in range(num_hallways):
                    self.hallways_added += 1
                    new_id = f"generated_hallway_{floor}_{i}"
                    hallway_ids.append(new_id)
                    hallway = RoomIntent(
                        id=new_id,
                        type="hallway",
                        floor=floor,
                        zone="CIRCULATION",
                        target_area_sqft=45,
                        min_area_sqft=35,
                        preferred_position="CENTER",
                        exterior_wall_required=False,
                        privacy_level="LOW"
                    )
                    self.program_rooms.insert(0, hallway)
                    
                from app.design.program.spatial_program import AdjacencyIntent
                if num_hallways > 1:
                    for i in range(num_hallways - 1):
                        self.program.adjacencies.append(AdjacencyIntent(
                            room_a=hallway_ids[i],
                            room_b=hallway_ids[i+1],
                            relationship="ADJACENT",
                            priority="HIGH"
                        ))
                for idx, pr in enumerate(private_rooms):
                    h_id = hallway_ids[idx % num_hallways]
                    self.program.adjacencies.append(AdjacencyIntent(
                        room_a=pr.id,
                        room_b=h_id,
                        relationship="ADJACENT",
                        priority="HIGH"
                    ))
                if floor > 1:
                    self.program.adjacencies.append(AdjacencyIntent(
                        room_a=f"staircase_{floor}",
                        room_b=hallway_ids[0],
                        relationship="ADJACENT",
                        priority="HIGH"
                    ))

    def generate(self):
        if self.program.floor_count > 2:
            raise GenerationFailure("ONLY_UP_TO_2_FLOORS_SUPPORTED_IN_G2B")
            
        floors_present = {r.floor for r in self.program_rooms}
        if floors_present != set(range(1, self.program.floor_count + 1)):
            raise GenerationFailure("NON_CONTIGUOUS_OR_MISSING_FLOORS")

        self.rooms_by_floor = {f: [] for f in range(1, self.program.floor_count + 1)}
        for r in self.program_rooms:
            norm_type = _normalize_room_type(r.type)
            rule = rule_for(norm_type)
            safe_target = max(r.target_area_sqft, rule.min_width * rule.min_length)
            
            if rule.min_width > self.env_w or rule.min_length > self.env_l:
                raise GenerationFailure("ROOM_MIN_DIMENSIONS_EXCEED_PLOT")
                
            self.rooms_by_floor[r.floor].append({
                'intent': r,
                'norm_type': norm_type,
                'target': safe_target,
                'rule': rule
            })
            
        for f in self.rooms_by_floor:
            self.rooms_by_floor[f].sort(key=lambda x: (x['intent'].zone == 'CIRCULATION', x['target']), reverse=True)

        if self.program.floor_count == 1:
            best = None
            def backtrack_f1(room_idx, placed):
                nonlocal best
                if room_idx == len(self.rooms_by_floor[1]):
                    best = placed
                    return True
                rp = self.rooms_by_floor[1][room_idx]
                intent = rp['intent']
                candidates = self._get_candidates(rp['rule'], rp['target'], self.env_w, self.env_l)
                placements = self._score_placements(candidates, placed, intent, 0, 0, self.env_w, self.env_l)
                for score, x, y, w, l in placements[:10]:
                    new_placed = list(placed)
                    new_placed.append(PlacedRoom(intent.id, rp['norm_type'], intent.floor, x, y, w, l, rp['target']))
                    if backtrack_f1(room_idx + 1, new_placed):
                        return True
                    self.global_backtracks += 1
                    if self.global_backtracks > self.MAX_BACKTRACKS:
                        return True
                return False
            backtrack_f1(0, [])
            if not best:
                if self.global_backtracks > self.MAX_BACKTRACKS: raise GenerationFailure("VERTICAL_SEARCH_LIMIT_EXCEEDED")
                raise GenerationFailure("NO_NON_OVERLAPPING_PLACEMENT")
            return self._build_result(best)
            
        # Multi-floor unified backtracking
        stair_rule = rule_for('staircase')
        stair_w, stair_l = stair_rule.min_width, stair_rule.min_length
        if self.env_w < stair_w or self.env_l < stair_l:
            raise GenerationFailure("STAIR_CORE_UNPLACEABLE")
            
        core_intent = self.program.vertical_core
        stair_pos = core_intent.stair_position if core_intent else 'CENTER'
        align_service = core_intent.align_service_zones if core_intent else False
        
        y_base, x_base = round(self.env_l / 2 - stair_l / 2), round(self.env_w / 2 - stair_w / 2)
        if 'FRONT' in stair_pos: y_base = 0.0 if self.plot.road_side.lower() == 'south' else round(self.env_l - stair_l)
        elif 'REAR' in stair_pos: y_base = round(self.env_l - stair_l) if self.plot.road_side.lower() == 'south' else 0.0
        if 'LEFT' in stair_pos: x_base = 0.0 if self.plot.road_side.lower() in ['south', 'north'] else round(self.env_l / 2)
        elif 'RIGHT' in stair_pos: x_base = round(self.env_w - stair_w) if self.plot.road_side.lower() in ['south', 'north'] else round(self.env_w)
            
        core_cands = [(x_base, y_base, stair_w, stair_l)]
        center = (round(self.env_w / 2 - stair_w / 2), round(self.env_l / 2 - stair_l / 2), stair_w, stair_l)
        if center not in core_cands:
            core_cands.append(center)
            
        best_full_layout = None

        def backtrack_f2(room_idx, f2_placed, f1_placed, bound_w, bound_l, offset_x, offset_y, f2_backtracks):
            if room_idx == len(self.rooms_by_floor[2]):
                # Check topological validity BEFORE accepting
                circ_rooms = [r for r in f2_placed if 'staircase' in r.id or 'hallway' in r.id]
                private_rooms = [r for r in f2_placed if r.type in ['bedroom_1', 'bedroom_2', 'bedroom_3', 'bedroom_4', 'bedroom_5', 'bedroom']]
                valid = True
                for pr in private_rooms:
                    connected = False
                    for cr in circ_rooms:
                        x_overlap = min(pr.x + pr.width, cr.x + cr.width) - max(pr.x, cr.x)
                        y_overlap = min(pr.y + pr.length, cr.y + cr.length) - max(pr.y, cr.y)
                        if x_overlap >= 2.99 and (abs(pr.y + pr.length - cr.y) < 0.01 or abs(cr.y + cr.length - pr.y) < 0.01):
                            connected = True
                        if y_overlap >= 2.99 and (abs(pr.x + pr.width - cr.x) < 0.01 or abs(cr.x + cr.width - pr.x) < 0.01):
                            connected = True
                    if not connected:
                        valid = False
                        break
                
                if valid:
                    self.candidates.append(f1_placed + f2_placed)
                    return True if len(self.candidates) >= self.MAX_CANDIDATES else False
                return False
                
            rp = self.rooms_by_floor[2][room_idx]
            intent = rp['intent']
            candidates = self._get_candidates(rp['rule'], rp['target'], bound_w, bound_l)
            placements = self._score_placements(candidates, f2_placed, intent, offset_x, offset_y, bound_w, bound_l, f1_placed)
            
            for score, x, y, w, l in placements[:10]:
                new_placed = list(f2_placed)
                new_placed.append(PlacedRoom(intent.id, rp['norm_type'], intent.floor, x, y, w, l, rp['target']))
                if backtrack_f2(room_idx + 1, new_placed, f1_placed, bound_w, bound_l, offset_x, offset_y, f2_backtracks):
                    if len(self.candidates) >= self.MAX_CANDIDATES: return True
                f2_backtracks[0] += 1
                self.global_backtracks += 1
                if f2_backtracks[0] > 500 or self.global_backtracks > self.MAX_BACKTRACKS:
                    return False # Give up on this f1 shape if it's too hard
            return False

        def backtrack_f1(room_idx, f1_placed):
            if room_idx == len(self.rooms_by_floor[1]):
                min_x = min(r.x for r in f1_placed)
                min_y = min(r.y for r in f1_placed)
                max_x = max(r.x + r.width for r in f1_placed)
                max_y = max(r.y + r.length for r in f1_placed)
                
                s2 = next((r for r in f1_placed if r.type == 'staircase'), None)
                f2_start = [PlacedRoom('staircase_2', 'staircase', 2, s2.x, s2.y, s2.width, s2.length, s2.width*s2.length)]
                
                f2_backtracks = [0]
                if backtrack_f2(0, f2_start, f1_placed, max_x - min_x, max_y - min_y, min_x, min_y, f2_backtracks):
                    if len(self.candidates) >= self.MAX_CANDIDATES: return True
                return False
                
            rp = self.rooms_by_floor[1][room_idx]
            intent = rp['intent']
            candidates = self._get_candidates(rp['rule'], rp['target'], self.env_w, self.env_l)
            placements = self._score_placements(candidates, f1_placed, intent, 0, 0, self.env_w, self.env_l)
            
            for score, x, y, w, l in placements[:10]:
                new_placed = list(f1_placed)
                new_placed.append(PlacedRoom(intent.id, rp['norm_type'], intent.floor, x, y, w, l, rp['target']))
                if backtrack_f1(room_idx + 1, new_placed):
                    if len(self.candidates) >= self.MAX_CANDIDATES: return True
                self.global_backtracks += 1
                if self.global_backtracks > self.MAX_BACKTRACKS:
                    return True
            return False

        for cx, cy, cw, cl in core_cands:
            self.stair_candidates_evaluated += 1
            s1 = PlacedRoom('staircase_1', 'staircase', 1, cx, cy, cw, cl, cw*cl)
            if backtrack_f1(0, [s1]):
                if len(self.candidates) >= self.MAX_CANDIDATES: break
                
        if not self.candidates:
            if self.global_backtracks > self.MAX_BACKTRACKS:
                raise GenerationFailure("VERTICAL_SEARCH_LIMIT_EXCEEDED")
            raise GenerationFailure("GROUND_FLOOR_UNSOLVABLE" if self.stair_candidates_evaluated > 0 else "STAIR_CORE_UNPLACEABLE")
            
        return [self._build_result(c) for c in self.candidates]

    def _get_candidates(self, rule, target_a, bound_w, bound_l):
        import math
        candidates = []
        w = math.ceil(rule.min_width)
        while w <= bound_w and w * rule.min_length <= target_a * 1.2:
            l = round(target_a / w)
            if rule.min_length <= l <= bound_l:
                if (w / l <= rule.aspect_limit) and (l / w <= rule.aspect_limit):
                    if (w, l) not in candidates: candidates.append((w, l))
                    if (l, w) not in candidates: candidates.append((l, w))
            w += 1.0
        if not candidates:
            sq = target_a ** 0.5
            candidates.append((sq, sq))
        return candidates

    def _score_placements(self, candidates, placed, intent, offset_x, offset_y, bound_w, bound_l, support_rooms=None):
        placements = []
        for cand_w, cand_l in candidates:
            x = offset_x
            while x + cand_w <= offset_x + bound_w + 0.01:
                y = offset_y
                while y + cand_l <= offset_y + bound_l + 0.01:
                    self.global_evals += 1
                    
                    overlap = False
                    shared_wall = False if placed else True
                    
                    for p in placed:
                        if not (x >= p.x + p.width - 0.01 or x + cand_w <= p.x + 0.01 or 
                                y >= p.y + p.length - 0.01 or y + cand_l <= p.y + 0.01):
                            overlap = True
                            break
                            
                        x_overlap = min(x + cand_w, p.x + p.width) - max(x, p.x)
                        y_overlap = min(y + cand_l, p.y + p.length) - max(y, p.y)
                        
                        if x_overlap >= 2.99 and (abs(y + cand_l - p.y) < 0.01 or abs(p.y + p.length - y) < 0.01):
                            shared_wall = True
                        if y_overlap >= 2.99 and (abs(x + cand_w - p.x) < 0.01 or abs(p.x + p.width - x) < 0.01):
                            shared_wall = True

                    if overlap or not shared_wall:
                        y += 1.0
                        continue
                        
                    if support_rooms:
                        supp_area = 0.0
                        for sr in support_rooms:
                            ix_min = max(x, sr.x)
                            ix_max = min(x + cand_w, sr.x + sr.width)
                            iy_min = max(y, sr.y)
                            iy_max = min(y + cand_l, sr.y + sr.length)
                            if ix_max > ix_min and iy_max > iy_min:
                                supp_area += (ix_max - ix_min) * (iy_max - iy_min)
                        if abs(supp_area - (cand_w * cand_l)) > 0.1:
                            y += 1.0
                            continue
                            
                    score = 0.0
                    cx = x + cand_w / 2
                    cy = y + cand_l / 2
                    
                    # Massively reward sharing a wall for HIGH priority adjacencies
                    for adj in self.program.adjacencies:
                        if adj.priority == 'HIGH' and (adj.room_a == intent.id or adj.room_b == intent.id):
                            other = next((p for p in placed if p.id == adj.room_a or p.id == adj.room_b), None)
                            if other:
                                x_overlap = min(x + cand_w, other.x + other.width) - max(x, other.x)
                                y_overlap = min(y + cand_l, other.y + other.length) - max(y, other.y)
                                is_shared = False
                                if x_overlap >= 2.99 and (abs(y + cand_l - other.y) < 0.01 or abs(other.y + other.length - y) < 0.01):
                                    is_shared = True
                                if y_overlap >= 2.99 and (abs(x + cand_w - other.x) < 0.01 or abs(other.x + other.width - x) < 0.01):
                                    is_shared = True
                                if is_shared:
                                    score -= 1000.0  # Massive bonus!
                    road = self.plot.road_side.lower()
                    
                    if support_rooms:
                        min_sx = min(sr.x for sr in support_rooms)
                        max_sx = max(sr.x + sr.width for sr in support_rooms)
                        min_sy = min(sr.y for sr in support_rooms)
                        max_sy = max(sr.y + sr.length for sr in support_rooms)
                        ideal_x = min_sx + (max_sx - min_sx) / 2
                        ideal_y = min_sy + (max_sy - min_sy) / 2
                        env_x1, env_x2 = min_sx, max_sx
                        env_y1, env_y2 = min_sy, max_sy
                    else:
                        ideal_x, ideal_y = self.env_w / 2, self.env_l / 2
                        env_x1, env_x2 = 0, self.env_w
                        env_y1, env_y2 = 0, self.env_l
                        
                    if 'FRONT' in intent.preferred_position:
                        if road == 'south': ideal_y = env_y1
                        elif road == 'north': ideal_y = env_y2
                        elif road == 'west': ideal_x = env_x1
                        elif road == 'east': ideal_x = env_x2
                    elif 'REAR' in intent.preferred_position:
                        if road == 'south': ideal_y = env_y2
                        elif road == 'north': ideal_y = env_y1
                        elif road == 'west': ideal_x = env_x2
                        elif road == 'east': ideal_x = env_x1
                        
                    if 'LEFT' in intent.preferred_position:
                        ideal_x = env_x1 if road in ('south', 'north') else ideal_y
                    elif 'RIGHT' in intent.preferred_position:
                        ideal_x = env_x2 if road in ('south', 'north') else ideal_y
                        
                    dist_to_ideal = ((cx - ideal_x)**2 + (cy - ideal_y)**2)**0.5
                    score += dist_to_ideal * 10
                    
                    min_px = x
                    min_py = y
                    max_px = x + cand_w
                    max_py = y + cand_l
                    for p in placed:
                        if p.x < min_px: min_px = p.x
                        if p.y < min_py: min_py = p.y
                        if p.x + p.width > max_px: max_px = p.x + p.width
                        if p.y + p.length > max_py: max_py = p.y + p.length
                    footprint_area = (max_px - min_px) * (max_py - min_py)
                    score += footprint_area * 0.5

                    
                    for adj in self.program.adjacencies:
                        other = next((p for p in placed if p.id == adj.room_a or p.id == adj.room_b), None)
                        if other and (adj.room_a == intent.id or adj.room_b == intent.id):
                            ocx, ocy = other.x + other.width / 2, other.y + other.length / 2
                            dist = ((cx - ocx)**2 + (cy - ocy)**2)**0.5
                            weight = 50 if adj.priority == 'HIGH' else 20
                            if adj.relationship == 'ADJACENT':
                                score += dist * weight * 2
                            elif adj.relationship == 'NEAR':
                                score += dist * weight
                            elif adj.relationship == 'SEPARATE':
                                score -= dist * weight 
                                
                    placements.append((score, x, y, cand_w, cand_l))
                    y += 1.0
                x += 1.0
                
        placements.sort(key=lambda item: item[0])
        return placements

    def _build_result(self, placed: list[PlacedRoom]):
        result = DesignResult(
            floor_count=self.program.floor_count,
            foundation_type="slab",
            terrain_type="flat"
        )
        for p in placed:
            result.rooms.append(RoomLayout(
                room_id=p.id,
                room_type=p.type,
                floor=p.floor,
                x=round(p.x, 2),
                y=round(p.y, 2),
                width=round(p.width, 2),
                length=round(p.length, 2)
            ))
            
        actual_area = sum(r.width * r.length for r in result.rooms)
        result.total_built_up_area_sqft = actual_area
        ground_area = sum(r.width * r.length for r in result.rooms if r.floor == 1)
        result.ground_footprint_sqft = ground_area
        
        val_result = validate_geometry(
            result.rooms,
            expected_bedrooms=sum(1 for r in self.program.rooms if "bedroom" in r.type.lower()),
            expected_floors=self.program.floor_count,
            land_size_perches=self.plot.land_size_perches,
            plot=self.plot,
            design=None
        )
        if not val_result.passed:
            raise GenerationFailure(f"GEOMETRY_VALIDATION_FAILED: {val_result.failures[0]}")
            
        meta = {
            "floor_count": self.program.floor_count,
            "rooms_requested": len(self.program.rooms),
            "rooms_placed": len(placed),
            "candidate_evaluations": self.global_evals,
            "floor_candidate_evaluations": self.global_evals,
            "core_candidates_evaluated": self.stair_candidates_evaluated,
            "backtracks": self.global_backtracks,
            "program_target_area": sum(r.target_area_sqft for r in self.program.rooms),
            "actual_room_area": actual_area,
            "ground_footprint_area": ground_area,
            "upper_footprint_area": actual_area - ground_area if self.program.floor_count > 1 else 0,
            "buildable_area": self.env_w * self.env_l,
            "generation_ms": 0,
            "placement_score": 100,
            "failure_reason": None
        }
        return result, meta

def generate_geometry(program: SpatialProgram, plot: PlotConstraints) -> tuple[DesignResult, dict[str, Any]]:
    start_ms = time.time() * 1000
    solver = LayoutSolver(program, plot)
    candidates_results = solver.generate()
    
    from app.design.geometry.finishing import finish_generative_layout
    from app.design.quality.architectural_quality import validate_architectural_quality
    open_plan = any('open_plan' in m for m in program.notes) if hasattr(program, 'notes') else False
    
    best_candidate = None
    best_score = -1
    best_meta = None
    
    geometry_rejections = 0
    quality_rejections = 0
    circulation_rejections = 0
    repair_attempts = solver.hallways_added
    repair_strategies = ["INSERT_HALLWAY"] if repair_attempts > 0 else []
    
    for i, (res, meta) in enumerate(candidates_results):
        try:
            fin_res, fin_meta = finish_generative_layout(res, program, open_plan=open_plan)
            meta.update(fin_meta)
            
            val = validate_geometry(
                rooms=fin_res.rooms,
                expected_bedrooms=sum(1 for r in solver.program_rooms if "bedroom" in r.type.lower()),
                expected_floors=program.floor_count,
                land_size_perches=plot.land_size_perches,
                plot=plot,
                design=fin_res
            )
            
            if not val.passed:
                meta["failure_reason"] = f"GEOMETRY_INVALID: {val.failures[0]}"
                print(f"Candidate {i} failed geometry:", val.failures[0])
                if any('privacy' in str(f).lower() or 'accessibility' in str(f).lower() for f in val.failures):
                    circulation_rejections += 1
                else:
                    geometry_rejections += 1
                meta["quality_status"] = "GEOMETRICALLY_INVALID"
                continue
                
            q_result = validate_architectural_quality(fin_res, plot=plot)
            q_dict = q_result.to_dict()
            meta["quality_score"] = q_dict['score']
            meta["quality_status"] = q_dict['status']
            
            print(f"Candidate {i} quality: {q_dict['passed']}, score: {q_dict['score']}, status: {q_dict['status']}")
            
            if not q_dict['passed']:
                quality_rejections += 1
                
            if q_dict['score'] > best_score:
                best_score = q_dict['score']
                best_candidate = fin_res
                best_meta = meta
                
        except Exception as e:
            geometry_rejections += 1
            meta["failure_reason"] = f"EXCEPTION: {str(e)}"
            meta["quality_status"] = "GEOMETRICALLY_INVALID"
            
    if best_candidate is None:
        raise GenerationFailure("NO_VALID_CANDIDATE_FOUND")
        
    best_meta["complete_candidates_evaluated"] = len(candidates_results)
    best_meta["geometry_rejections"] = geometry_rejections
    best_meta["quality_rejections"] = quality_rejections
    best_meta["circulation_rejections"] = circulation_rejections
    best_meta["repair_attempts"] = repair_attempts
    best_meta["repair_strategies"] = repair_strategies
    best_meta["best_quality_score"] = best_score
    best_meta["generation_ms"] = int((time.time() * 1000) - start_ms)
    
    return best_candidate, best_meta
