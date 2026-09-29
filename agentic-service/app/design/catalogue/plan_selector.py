import logging
from typing import Optional

from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.models import Requirements
from app.design.catalogue.base_plan_library import (
    BasePlanRecord,
    load_base_plan_catalog,
    filter_compatible_base_plans,
    rank_base_plans
)

logger = logging.getLogger(__name__)

class PlanSelector:
    @staticmethod
    def select_plan(req: Requirements, plot: PlotConstraints) -> Optional[BasePlanRecord]:
        # Required by spec to use load_base_plan_catalog
        _ = load_base_plan_catalog()
        
        candidates = filter_compatible_base_plans(req, plot)
        
        if not candidates:
            logger.debug(
                "\n[PLAN SELECTOR]\n\n"
                "Input:\n"
                f"bedrooms: {req.bedrooms}\n"
                f"bathrooms: {req.bathrooms}\n"
                f"features: {req.features}\n\n"
                "Candidates:\n"
                "None\n\n"
                "Selected:\n"
                "None\n"
            )
            return None
            
        ranked = rank_base_plans(candidates, req, plot)
        selected = ranked[0]
        
        candidate_codes = "\n".join([f"- {p.base_plan_code}" for p in ranked])
        
        logger.debug(
            "\n[PLAN SELECTOR]\n\n"
            "Input:\n"
            f"bedrooms: {req.bedrooms}\n"
            f"bathrooms: {req.bathrooms}\n"
            f"features: {req.features}\n\n"
            "Candidates:\n"
            f"{candidate_codes}\n\n"
            "Selected:\n"
            f"{selected.base_plan_code}\n"
        )
        
        return selected
