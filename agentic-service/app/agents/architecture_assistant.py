from typing import Optional, List
from pydantic import BaseModel, Field, ConfigDict

class Requirements(BaseModel):
    model_config = ConfigDict(extra='forbid')
    land_size: Optional[float]
    land_unit: Optional[str]
    plot_width_ft: Optional[float]
    plot_length_ft: Optional[float]
    bedrooms: Optional[int]
    bathrooms: Optional[int]
    floors: Optional[int]
    style: Optional[str]
    parking_spaces: Optional[int]
    open_plan: Optional[bool]
    master_ensuite: Optional[bool]
    separate_dining: Optional[bool]
    office: Optional[bool]
    balcony: Optional[bool]
    veranda: Optional[bool]
    utility_room: Optional[bool]
    accessible_friendly: Optional[bool]
    terrain_type: Optional[str]
    road_side: Optional[str]
    north_direction: Optional[str]
    entrance_side: Optional[str]

class AssistantInterpretation(BaseModel):
    model_config = ConfigDict(extra='forbid')
    intent: str = Field(description="One of: GENERAL_ADVICE, LAND_FEASIBILITY_ADVICE, DESIGN_REQUEST, PROJECT_QUESTION, CONSTRUCTION_QUESTION, UNKNOWN")
    confidence: float = Field(description="Confidence score between 0.0 and 1.0")
    requirements: Optional[Requirements] = Field(description="Extracted requirements for DESIGN_REQUEST and LAND_FEASIBILITY_ADVICE")
    missing_required_fields: List[str] = Field(description="Missing required fields for the intent")
    assumptions: List[str] = Field(description="Assumptions made by the AI")
    user_goal: Optional[str] = Field(description="The user's inferred goal")

def get_current_user_project_context():
    # Stub for getting project context
    return {"status": "No active project found in context."}

def get_supported_plan_options():
    # Stub for getting supported plans
    return []

def create_design_brief(requirements: dict):
    # Stub for creating design brief
    return {"brief_id": "temp-1234", "requirements": requirements}

class AssistantAction(BaseModel):
    type: str
    payload: dict

class AgentResponse(BaseModel):
    reply: str
    intent: str
    action: AssistantAction

def interpret_user_message(message: str, history: Optional[List[dict]] = None) -> dict:
    from app.providers.provider_factory import get_available_design_provider, get_provider
    from app.agents.feasibility_engine import check_feasibility, generate_feasibility_advice
    
    provider = get_available_design_provider() or get_provider("openai")
    
    # Tool 1: interpret_user_request
    system_prompt = """You are an AI Architecture Assistant.
Analyze the user message and extract the intent and structural requirements.
Supported intents:
- GENERAL_ADVICE
- LAND_FEASIBILITY_ADVICE
- DESIGN_REQUEST
- PROJECT_QUESTION
- CONSTRUCTION_QUESTION
- UNKNOWN

If intent is DESIGN_REQUEST or LAND_FEASIBILITY_ADVICE, extract structural fields into 'requirements'. 
Put anything inferred into 'assumptions'.
Pay attention to the previous conversation history if provided, as the user might be referring to previously stated requirements."""

    history_context = ""
    if history:
        history_context = "Chat History:\n" + "\n".join([f"{msg['role'].capitalize()}: {msg['content']}" for msg in history]) + "\n\n"

    try:
        res = provider.generate_json(system_prompt, f'{history_context}User message: "{message}"', AssistantInterpretation)
        intent = res.get('intent', 'UNKNOWN')
        reqs_obj = res.get('requirements')
        reqs = reqs_obj if isinstance(reqs_obj, dict) else (reqs_obj.dict() if reqs_obj else None)
        
        context_str = f"{history_context}User Message: {message}\nIntent: {intent}\n"
        action_type = "NONE"
        action_payload = {}
        
        # Determine tools based on intent
        if intent == 'GENERAL_ADVICE':
            from app.knowledge.rag_pipeline import search_knowledge_as_dicts
            try:
                knowledge = search_knowledge_as_dicts(message, top_k=3)
                context_str += f"RAG Knowledge:\n{knowledge}\n"
            except Exception:
                pass
                
        elif intent == 'LAND_FEASIBILITY_ADVICE':
            if reqs:
                advice = generate_feasibility_advice(reqs)
                context_str += f"System Feasibility Advice:\n{advice}\n"
            from app.knowledge.rag_pipeline import search_knowledge_as_dicts
            try:
                knowledge = search_knowledge_as_dicts(message, top_k=2)
                context_str += f"RAG Knowledge:\n{knowledge}\n"
            except Exception:
                pass
                
        elif intent == 'DESIGN_REQUEST':
            if reqs:
                from app.agents.requirement_validator import validate_requirements_sanity
                sanity = validate_requirements_sanity(reqs)
                
                if sanity.status == 'INVALID':
                    context_str += f"Semantic Validation Failed:\n{sanity.to_dict()}\n"
                    action_type = "NONE"
                    action_payload = {"sanity": sanity.to_dict()}
                elif sanity.status == 'NEEDS_CONFIRMATION':
                    context_str += f"Semantic Validation Needs Confirmation:\n{sanity.to_dict()}\n"
                    action_type = "NONE"
                    action_payload = {"sanity": sanity.to_dict()}
                else:
                    feasibility = check_feasibility(reqs)
                    context_str += f"System Feasibility Result:\n{feasibility.to_dict()}\n"
                    
                    # Never navigate or create design brief if feasibility failed.
                    if getattr(feasibility, 'can_proceed', False):
                        brief = create_design_brief(reqs)
                        context_str += f"Design Brief Created:\n{brief}\n"
                        action_type = "CONTINUE_TO_DESIGN"
                        action_payload = {"requirements": reqs, "feasibility": feasibility.to_dict()}
                    else:
                        action_type = "NONE"
                        # Pass the rejection reasons and suggestions to the payload just in case frontend needs it
                        action_payload = {"feasibility": feasibility.to_dict()}
                    
        elif intent == 'PROJECT_QUESTION':
            proj_context = get_current_user_project_context()
            context_str += f"Project Context:\n{proj_context}\n"
            action_type = "OPEN_PROJECT"
            action_payload = {"context": proj_context}
            
        elif intent == 'CONSTRUCTION_QUESTION':
            from app.knowledge.rag_pipeline import search_knowledge_as_dicts
            try:
                knowledge = search_knowledge_as_dicts(message, top_k=3)
                context_str += f"RAG Knowledge:\n{knowledge}\n"
            except Exception:
                pass
            proj_context = get_current_user_project_context()
            context_str += f"Project Context:\n{proj_context}\n"
            
        # Final LLM pass to synthesize reply
        class FinalResponse(BaseModel):
            reply: str = Field(description="The final response to the user incorporating context")
            
        final_system_prompt = """You are an AI Architecture Assistant.
Generate a helpful response to the user's message based on the provided context.
RULES:
1. For general/design/construction advice, use the RAG Knowledge Context.
2. For design feasibility, NEVER override the deterministic feasibility result.
3. Do NOT claim structural or code approval. Include a brief disclaimer."""

        try:
            final_res = provider.generate_json(final_system_prompt, context_str, FinalResponse)
            reply = final_res.get('reply', 'No reply generated.')
        except Exception as e:
            reply = res.get('message', str(e))
            
        return {
            "reply": reply,
            "intent": intent,
            "action": {
                "type": action_type,
                "payload": action_payload
            }
        }
    except Exception as e:
        return {
            "reply": f"Failed to process request: {str(e)}",
            "intent": "UNKNOWN",
            "action": {
                "type": "NONE",
                "payload": {}
            }
        }
