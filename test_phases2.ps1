Continue = 'SilentlyContinue'
 = (Invoke-RestMethod -Uri "http://localhost:5265/api/v1/ai-generation/generate" -Method Post -Body '{"budgetLkr":15000000,"landSizePerches":10.0,"manualTerrainType":"flat","preferences":{"bedrooms":3,"floors":2,"architecturalStyle":"modern"}}' -ContentType "application/json").workflowId
Write-Host "Created Workflow: "

# We need to simulate the state that allows approval. The WorkflowService checks ValidationPassed.
# Wait, let's just see what the approval endpoint says:
try {
     = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/workflows//approve" -Method Post -Body '{"decision":"approve"}' -ContentType "application/json"
    Write-Host "Architect Approve Result: "
} catch {
    Write-Host "Architect Approve caught exception: "
}
