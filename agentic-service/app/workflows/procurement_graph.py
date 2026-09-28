import json
import traceback
from typing import Any, Dict, List, TypedDict
from langgraph.graph import StateGraph, END
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from app.config import OPENAI_API_KEY

class ProcurementState(TypedDict):
    project_id: str
    project_data: dict
    construction_plan: list
    inventory: list
    
    current_phase: str
    upcoming_phases: list
    
    required_materials: list
    available_materials: list
    shortages: list
    
    procurement_plan: list
    procurement_priorities: list
    risks: list
    acceleration_opportunities: list
    final_recommendation: dict

llm = ChatOpenAI(model="gpt-4o", api_key=OPENAI_API_KEY, temperature=0.2)

def extract_json(content: str) -> dict | list:
    content = content.strip()
    if content.startswith("```json"):
        content = content[7:-3]
    elif content.startswith("```"):
        content = content[3:-3]
    return json.loads(content)

def analyze_project_node(state: ProcurementState) -> ProcurementState:
    plan = state.get("construction_plan", [])
    upcoming_phases = [p for p in plan if p.get("status") != "Completed"]
    return {"upcoming_phases": upcoming_phases, "current_phase": upcoming_phases[0]["name"] if upcoming_phases else "None"}

def material_requirement_node(state: ProcurementState) -> ProcurementState:
    prompt = ChatPromptTemplate.from_template(
        """Based on the following construction plan phases: {phases}, 
        identify the key required materials for these phases. 
        Also take into account the existing inventory: {inventory}.
        Return a JSON list of objects, each with:
        'phase_name', 'material_name', 'required_quantity', 'unit'.
        Output ONLY valid JSON array."""
    )
    
    inventory = state.get("inventory", [])
    required_materials = []
    
    try:
        chain = prompt | llm
        response = chain.invoke({
            "phases": json.dumps(state.get("upcoming_phases", [])),
            "inventory": json.dumps(inventory)
        })
        llm_materials = extract_json(response.content)
        if isinstance(llm_materials, list):
            required_materials = llm_materials
    except Exception as e:
        print("Error in material LLM:", e)

    # For safety, we also include everything from existing inventory
    for item in inventory:
        required_materials.append({
            "phase_name": "General", 
            "material_name": item["name"],
            "required_quantity": item.get("required", 0),
            "unit": item.get("unit", "")
        })
        
    return {"required_materials": required_materials, "available_materials": inventory}

def inventory_node(state: ProcurementState) -> ProcurementState:
    inventory = state.get("inventory", [])
    shortages = []
    for item in inventory:
        req = float(item.get("required", 0))
        avail = float(item.get("available", 0))
        ordered = float(item.get("ordered", 0))
        shortage_amt = req - (avail + ordered)
        if shortage_amt > 0:
            shortages.append({
                "material_name": item["name"],
                "shortage": shortage_amt,
                "unit": item.get("unit", ""),
                "phase_id": item.get("phase_id")
            })
    return {"shortages": shortages}

def route_shortage(state: ProcurementState) -> str:
    return "procurement"

def procurement_node(state: ProcurementState) -> ProcurementState:
    prompt = ChatPromptTemplate.from_template(
        """You are a Procurement Agent. Given the upcoming construction phases: {phases}
        and the current materials inventory: {inventory}.
        
        Calculate the required items to be ordered, their urgency, and the deadline based on typical lead times and phase start dates. 
        Categorize items into one of four priorities: "Critical", "Upcoming", "Ready", "Later".
        - If shortage > 0 and phase starts soon (or is current), it's "Critical".
        - If shortage > 0 and phase is later, "Upcoming".
        - If shortage <= 0 and it's needed soon, "Ready".
        - If not needed in current/next phase, "Later".
        
        Return a JSON object with two keys:
        'procurement_plan': list of objects with 'item', 'action', 'urgency' ("Critical" or "Upcoming"), 'deadline', 'message' (Use 🔴 for Critical, 🟠 for Upcoming)
        'procurement_priorities': list of objects with 'item', 'priority' ("Critical", "Upcoming", "Ready", "Later"), 'status'
        
        Output ONLY valid JSON object."""
    )
    
    try:
        chain = prompt | llm
        response = chain.invoke({
            "phases": json.dumps(state.get("upcoming_phases", [])),
            "inventory": json.dumps(state.get("inventory", []))
        })
        result = extract_json(response.content)
        plan = result.get("procurement_plan", [])
        priorities = result.get("procurement_priorities", [])
    except Exception as e:
        print("Error in procurement LLM:", e)
        plan = []
        priorities = []
        inventory = state.get("inventory", [])
        for item in inventory:
            req = float(item.get("required", 0))
            avail = float(item.get("available", 0))
            ordered = float(item.get("ordered", 0))
            shortage_amt = req - (avail + ordered)
            name = item.get("name", "Unknown")
            if shortage_amt > 0:
                plan.append({
                    "item": name,
                    "action": f"Order {shortage_amt} {item.get('unit', '')}",
                    "urgency": "Critical",
                    "deadline": "Immediate",
                    "message": f"🔴 {shortage_amt} {name} need to be ordered to prevent a delay."
                })
                priorities.append({"item": name, "priority": "Critical", "status": "Order immediately"})
            elif req > 0:
                priorities.append({"item": name, "priority": "Ready", "status": "Already available"})
                
    return {"procurement_plan": plan, "procurement_priorities": priorities}

def risk_node(state: ProcurementState) -> ProcurementState:
    prompt = ChatPromptTemplate.from_template(
        """You are a Risk Agent. Check if missing items can affect the schedule based on dependencies.
        Phases: {phases}
        Shortages: {shortages}
        
        If an item is missing, determine what phases will be delayed and build a dependency chain (e.g. "Tiles unavailable" -> "Flooring delayed" -> "Painting delayed").
        Return a JSON list of objects, each with:
        'issue' (string, e.g. "Tiles unavailable"),
        'chain' (list of strings representing the cascade effect),
        'impact' (string: "High", "Medium", "Low")
        
        If no immediate risks, return an empty list []. Output ONLY a valid JSON array."""
    )
    
    try:
        chain = prompt | llm
        response = chain.invoke({
            "phases": json.dumps(state.get("upcoming_phases", [])),
            "shortages": json.dumps(state.get("shortages", []))
        })
        risks = extract_json(response.content)
    except Exception as e:
        print("Error in risk LLM:", e)
        risks = []
        
    return {"risks": risks}

def acceleration_node(state: ProcurementState) -> ProcurementState:
    prompt = ChatPromptTemplate.from_template(
        """Given a project with phases {phases} and current shortages {shortages}, 
        suggest actionable steps to accelerate the construction schedule without compromising quality.
        Estimate the current completion and potential optimized completion dates.
        Return as a JSON object with:
        'current_estimated_completion' (string),
        'potential_optimized_completion' (string),
        'potential_schedule_improvement' (string),
        'actions' (list of strings).
        Output ONLY valid JSON object."""
    )
    try:
        chain = prompt | llm
        response = chain.invoke({
            "phases": json.dumps(state.get("upcoming_phases", [])),
            "shortages": json.dumps(state.get("shortages", []))
        })
        accel = extract_json(response.content)
    except:
        accel = {
            "current_estimated_completion": "Pending Analysis",
            "potential_optimized_completion": "Pending Analysis",
            "potential_schedule_improvement": "N/A",
            "actions": ["Optimize material delivery schedule", "Parallelize independent tasks"]
        }
        
    return {"acceleration_opportunities": accel}

def recommendation_node(state: ProcurementState) -> ProcurementState:
    return {"final_recommendation": {
        "status": "Ready" if not state.get("shortages") else "At Risk",
        "action_required": len(state.get("shortages", [])) > 0,
        "summary": "Review procurement plan and acceleration opportunities to ensure on-time delivery."
    }}

# Build Graph
workflow = StateGraph(ProcurementState)
workflow.add_node("analyze_project", analyze_project_node)
workflow.add_node("material_requirement", material_requirement_node)
workflow.add_node("inventory", inventory_node)
workflow.add_node("procurement", procurement_node)
workflow.add_node("risk", risk_node)
workflow.add_node("acceleration", acceleration_node)
workflow.add_node("recommendation", recommendation_node)

workflow.set_entry_point("analyze_project")
workflow.add_edge("analyze_project", "material_requirement")
workflow.add_edge("material_requirement", "inventory")
workflow.add_conditional_edges("inventory", route_shortage, {"procurement": "procurement", "risk": "risk"})
workflow.add_edge("procurement", "risk")
workflow.add_edge("risk", "acceleration")
workflow.add_edge("acceleration", "recommendation")
workflow.add_edge("recommendation", END)

readiness_graph = workflow.compile()

def run_readiness_workflow(input_state: dict) -> dict:
    result = readiness_graph.invoke(input_state)
    return result
