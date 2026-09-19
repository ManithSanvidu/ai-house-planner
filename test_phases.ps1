Continue = 'SilentlyContinue'

# 1. Create a new workflow (simulating validation PASS)
 = @{
    budgetLkr = 15000000
    landSizePerches = 10.0
    manualTerrainType = "flat"
    preferences = @{ bedrooms = 3; floors = 2; architecturalStyle = "modern" }
} | ConvertTo-Json

 = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/ai-generation/generate" -Method Post -Body  -ContentType "application/json"
 = .workflowId
Write-Host "Created Workflow: "

# We need to set ValidationPassed = true. In reality, the AI agent does this. 
# We'll just assume it's true because we know from earlier tests that ValidationPassed is tied to ApprovalStatus = pending/approved.
# Wait, let's check what GetWorkflowStatus returns.
 = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/workflows//status" -Method Get
Write-Host "Status: , Approval: "

# 2. Architect Approve
 = @{ decision = "approve" } | ConvertTo-Json
 = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/workflows//approve" -Method Post -Body  -ContentType "application/json"
Write-Host "Architect Approve Result: , Decision: "

# 3. Client Approve
 = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/workflows//approve" -Method Post -Body  -ContentType "application/json"
Write-Host "Client Approve Result: , Decision: , ProjectId: "

# 4. Duplicate Client Approve
try {
     = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/workflows//approve" -Method Post -Body  -ContentType "application/json"
    Write-Host "Duplicate Approve Result: "
} catch {
    Write-Host "Duplicate Approve caught exception: "
}

# 5. Project Tracking Endpoint
 = .projectId
if () {
     = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/projects/" -Method Get
    Write-Host "Project Phases Count: 0"
    if (.phases.Count -gt 0) {
        Write-Host "First phase: "
    }
}
