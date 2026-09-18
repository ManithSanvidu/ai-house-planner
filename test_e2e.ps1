$ProgressPreference = 'SilentlyContinue'

Write-Host "1. Submitting Intake Form to ASP.NET"
$payload = @{
    budgetLkr = 45000000
    landSizePerches = 10.0
    manualTerrainType = "flat"
    preferences = @{
        bedrooms = 3
        floors = 1
        architecturalStyle = "modern"
    }
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/ai-generation/generate" -Method Post -Body $payload -ContentType "application/json"
$workflowId = $response.workflowId
Write-Host "Workflow ID: $workflowId"

Write-Host "2. Polling Workflow Status"
for ($i = 0; $i -lt 10; $i++) {
    Start-Sleep -Seconds 2
    $statusResponse = Invoke-RestMethod -Uri "http://localhost:5265/api/v1/workflows/$workflowId/status" -Method Get -ErrorAction SilentlyContinue
    if ($statusResponse) {
        Write-Host "Status: $($statusResponse.status)"
        if ($statusResponse.design) {
            Write-Host "Design found! Version: $($statusResponse.design.version)"
            break
        }
    } else {
        Write-Host "404 or Error"
    }
}
