from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI(
    title="AI House Planner Agentic Service",
    version="1.0.0",
    description="Agentic service for the AI House Planner application",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=False,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/")
async def root():
    return {
        "message": "AI House Planner Agentic Service is running",
        "version": "1.0.0",
    }


@app.get("/health")
async def health():
    return {
        "status": "ok",
        "service": "agentic-service",
    }


@app.get("/api")
async def api_status():
    return {
        "message": "AI House Planner API is working"
    }