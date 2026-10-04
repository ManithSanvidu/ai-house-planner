from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Literal

from pydantic import BaseModel, Field, model_validator


class PlanStepStatus(str, Enum):
    PENDING = "pending"
    READY = "ready"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    SKIPPED = "skipped"
    AWAITING_APPROVAL = "awaiting_approval"


ALLOWED_AGENTS = {
    "requirement_analysis",
    "land_analysis",
    "design",
    "visualization",
    "construction_planning",
    "cost_estimation",
    "validation",
}


class WorkflowPlanStep(BaseModel):
    step_id: str
    name: str
    assigned_agent: str
    dependencies: list[str] = Field(default_factory=list)
    status: PlanStepStatus = PlanStepStatus.PENDING
    required_inputs: list[str] = Field(default_factory=list)
    produced_outputs: list[str] = Field(default_factory=list)
    error: str | None = None

    @model_validator(mode="after")
    def validate_agent(self) -> WorkflowPlanStep:
        if self.assigned_agent not in ALLOWED_AGENTS:
            raise ValueError(f"Invalid assigned_agent: {self.assigned_agent}. Must be one of {ALLOWED_AGENTS}")
        return self


class WorkflowPlan(BaseModel):
    objective: str
    steps: list[WorkflowPlanStep]
    created_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    version: int = 1

    @model_validator(mode="after")
    def validate_plan(self) -> WorkflowPlan:
        if not self.steps:
            raise ValueError("Plan must contain at least one step.")

        step_ids = [step.step_id for step in self.steps]
        
        # 1. Step IDs are unique
        if len(step_ids) != len(set(step_ids)):
            raise ValueError("Step IDs must be unique.")
            
        step_ids_set = set(step_ids)

        for step in self.steps:
            # 3. A step cannot depend on itself
            if step.step_id in step.dependencies:
                raise ValueError(f"Step {step.step_id} cannot depend on itself.")
                
            for dep in step.dependencies:
                # 2. Every dependency references an existing step ID
                if dep not in step_ids_set:
                    raise ValueError(f"Dependency {dep} for step {step.step_id} does not exist.")

        # 6. Circular dependencies must be rejected
        def has_cycle(node: str, visited: set, stack: set) -> bool:
            visited.add(node)
            stack.add(node)
            
            step = next((s for s in self.steps if s.step_id == node), None)
            if step:
                for neighbor in step.dependencies:
                    if neighbor not in visited:
                        if has_cycle(neighbor, visited, stack):
                            return True
                    elif neighbor in stack:
                        return True
                        
            stack.remove(node)
            return False

        visited: set[str] = set()
        stack: set[str] = set()

        for step in self.steps:
            if step.step_id not in visited:
                if has_cycle(step.step_id, visited, stack):
                    raise ValueError("Circular dependencies detected in plan steps.")

        return self


def create_default_house_planning_plan(objective: str = "Generate a complete house plan") -> WorkflowPlan:
    """Helper to generate the standard deterministic plan layout."""
    return WorkflowPlan(
        objective=objective,
        steps=[
            WorkflowPlanStep(
                step_id="S1",
                name="Requirement Analysis",
                assigned_agent="requirement_analysis",
                dependencies=[],
                required_inputs=["natural_language_prompt"],
                produced_outputs=["preferences"],
            ),
            WorkflowPlanStep(
                step_id="S2",
                name="Land Analysis",
                assigned_agent="land_analysis",
                dependencies=["S1"],
                required_inputs=["photo_url", "site_inputs"],
                produced_outputs=["terrain_result"],
            ),
            WorkflowPlanStep(
                step_id="S3",
                name="Design",
                assigned_agent="design",
                dependencies=["S1", "S2"],
                required_inputs=["preferences", "terrain_result"],
                produced_outputs=["design_result"],
            ),
            WorkflowPlanStep(
                step_id="S4",
                name="Visualization",
                assigned_agent="visualization",
                dependencies=["S3"],
                required_inputs=["design_result", "input_data"],
                produced_outputs=["ai_visualization"],
            ),
            WorkflowPlanStep(
                step_id="S5",
                name="Construction Planning",
                assigned_agent="construction_planning",
                dependencies=["S2", "S3"],
                required_inputs=["design_result", "terrain_result", "input_data"],
                produced_outputs=["construction_plan_result"],
            ),
            WorkflowPlanStep(
                step_id="S6",
                name="Cost Estimation",
                assigned_agent="cost_estimation",
                dependencies=["S2", "S3"],
                required_inputs=["design_result", "terrain_result", "budget"],
                produced_outputs=["cost_result"],
            ),
            WorkflowPlanStep(
                step_id="S7",
                name="Validation",
                assigned_agent="validation",
                dependencies=["S3", "S6"],
                required_inputs=["design_result", "cost_result"],
                produced_outputs=["validation_result"],
            ),
        ]
    )
