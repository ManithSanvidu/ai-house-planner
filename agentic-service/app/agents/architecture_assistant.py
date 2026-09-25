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
    message: str = Field(description="The parsed message or advice for the user based on intent")
    requirements: Optional[Requirements] = Field(description="Extracted requirements for DESIGN_REQUEST and LAND_FEASIBILITY_ADVICE")
    missing_required_fields: List[str] = Field(description="Missing required fields for the intent")
    assumptions: List[str] = Field(description="Assumptions made by the AI")
    user_goal: Optional[str] = Field(description="The user's inferred goal")

def interpret_user_message(message: str) -> dict:
    from app.providers.provider_factory import get_available_design_provider, get_provider
    from app.agents.feasibility_engine import check_feasibility, generate_feasibility_advice

    provider = get_available_design_provider() or get_provider("openai")
    
    system_prompt = """You are an AI Architecture Assistant.
Analyze the user message and extract the intent and structural requirements.
Supported intents:
- GENERAL_ADVICE: general architecture/design/construction question.
- LAND_FEASIBILITY_ADVICE: asking for advice based on land size / house requirements.
- DESIGN_REQUEST: asking the system to design/generate a house.
- PROJECT_QUESTION: asking a project-specific question.
- CONSTRUCTION_QUESTION: asking a construction related question.
- UNKNOWN: ambiguous text.

If intent is DESIGN_REQUEST or LAND_FEASIBILITY_ADVICE, extract structural fields into 'requirements'. Do NOT invent values unless explicitly marked as inferred/default. Put them in 'assumptions'.
Also identify 'missing_required_fields' (e.g. land_size, bedrooms) and 'user_goal'."""

    user_prompt = f'User message: "{message}"'

    try:
        if not provider:
            raise Exception("No provider available")
        res = provider.generate_json(system_prompt, user_prompt, AssistantInterpretation)

        # Run deterministic feasibility checks based on intent
        intent = res.get('intent', 'UNKNOWN')
        reqs = res.get('requirements')

        if intent == 'DESIGN_REQUEST' and reqs:
            feasibility = check_feasibility(reqs)
            res['feasibility'] = feasibility.to_dict()

        elif intent == 'LAND_FEASIBILITY_ADVICE' and reqs:
            advice = generate_feasibility_advice(reqs)
            res['feasibility_advice'] = advice

        # RAG retrieval for knowledge-based intents
        rag_intents = {'GENERAL_ADVICE', 'CONSTRUCTION_QUESTION', 'DESIGN_REQUEST', 'LAND_FEASIBILITY_ADVICE'}
        if intent in rag_intents:
            try:
                from app.knowledge.rag_pipeline import search_knowledge_as_dicts
                knowledge = search_knowledge_as_dicts(message, top_k=3)
                if knowledge:
                    res['knowledge_context'] = knowledge
                    res['response_sources'] = []
                    if intent in ('DESIGN_REQUEST', 'LAND_FEASIBILITY_ADVICE'):
                        res['response_sources'].append('system_feasibility')
                    if knowledge:
                        res['response_sources'].append('architecture_knowledge_base')
            except Exception as rag_err:
                # RAG failure is non-fatal — proceed without knowledge context
                res['knowledge_context'] = []
                res['rag_error'] = str(rag_err)

        return res
    except Exception as e:
        return {
            "intent": "UNKNOWN",
            "confidence": 0.0,
            "message": f"Failed to parse response: {e}",
            "requirements": None,
            "missing_required_fields": [],
            "assumptions": [],
            "user_goal": None
        }
