import time
import requests
import json

BASE_URL = "http://localhost:5265/api/v1"

def test_workflow():
    print("1. Creating new workflow...")
    payload = {
        "landSizePerches": 10.0,
        "terrainType": "flat",
        "preferences": {
            "bedrooms": 4,
            "bathrooms": 3,
            "floors": 2,
            "style": "Modern Tropical"
        },
        "plotConstraints": {
            "plot_width_ft": 40.0,
            "plot_length_ft": 60.0
        }
    }
    
    # We must authorize using a mock user since we bypassed it earlier, but wait, the endpoint might need auth.
    # Actually, the InternalWorkflowController handles this, but let's just use the API.
    # In earlier tests, we bypassed auth or the API allows anonymous if no token?
    # Let's try without token first.
    headers = {'Content-Type': 'application/json'}
    
    # If the API requires auth, we can just use the internal endpoint which design_agent uses.
    # Let's use the internal endpoint to directly trigger it if auth fails.
    resp = requests.post(f"{BASE_URL}/ai-generation/generate", json=payload, headers=headers)
    print(f"Status Code: {resp.status_code}")
    print(f"Raw Response: {resp.text}")
    
    if resp.status_code != 200:
        return
        
    workflow_id = resp.json().get("id") or resp.json().get("workflowId")
    print(f"Workflow ID: {workflow_id}")
    
    print("2. Waiting for workflow to complete...")
    for _ in range(30):
        status_resp = requests.get(f"{BASE_URL}/workflows/{workflow_id}")
        data = status_resp.json()
        if data["status"] in ["completed", "failed"]:
            break
        time.sleep(2)
        
    print(f"Final Status: {data['status']}")
    
    if data['status'] == 'completed':
        print("3. Checking visualization endpoint...")
        vis_resp = requests.get(f"{BASE_URL}/design/{workflow_id}/visualization")
        vis_data = vis_resp.json()
        print(f"Visualization status: {vis_data.get('status')}")
        print(f"AI Image URL: {vis_data.get('aiVisualizationImage')}")
        if vis_data.get('aiVisualizationImage'):
            print("SUCCESS: AI Image URL is saved and returned!")
        else:
            print("FAILED: AI Image URL is missing.")

if __name__ == "__main__":
    test_workflow()
