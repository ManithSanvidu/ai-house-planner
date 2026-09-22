import sys

filepath = "HousePlanner-Web/src/services/constructorWorkflowService.ts"
with open(filepath, 'r') as f:
    content = f.read()

# Add interfaces and API methods at the end of the file
addition = """
export interface DailyConstructionLogDto {
    id: string;
    projectId: string;
    logDate: string;
    constructionPhaseId?: string;
    phaseName?: string;
    workCompleted: string;
    challenges?: string;
    materialsUsed?: string;
    workforceCount?: number;
    weatherCondition?: string;
    safetyIssues?: string;
    progressPercentage?: number;
    tomorrowPlan?: string;
    notes?: string;
    createdAt: string;
    updatedAt: string;
}

export interface CreateDailyConstructionLogRequest {
    logDate: string;
    constructionPhaseId?: string;
    workCompleted: string;
    challenges?: string;
    materialsUsed?: string;
    workforceCount?: number;
    weatherCondition?: string;
    safetyIssues?: string;
    progressPercentage?: number;
    tomorrowPlan?: string;
    notes?: string;
}

export interface UpdateDailyConstructionLogRequest extends CreateDailyConstructionLogRequest {}

export const dailyConstructionLogService = {
    getLogs: async (projectId: string): Promise<DailyConstructionLogDto[]> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}/logs`, { headers: await getAuthHeaders() });
        return response.data;
    },
    getLog: async (projectId: string, logId: string): Promise<DailyConstructionLogDto> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}/logs/${logId}`, { headers: await getAuthHeaders() });
        return response.data;
    },
    createLog: async (projectId: string, request: CreateDailyConstructionLogRequest): Promise<DailyConstructionLogDto> => {
        const response = await axios.post(`${API_URL}/projects/${projectId}/logs`, request, { headers: await getAuthHeaders() });
        return response.data;
    },
    updateLog: async (projectId: string, logId: string, request: UpdateDailyConstructionLogRequest): Promise<DailyConstructionLogDto> => {
        const response = await axios.put(`${API_URL}/projects/${projectId}/logs/${logId}`, request, { headers: await getAuthHeaders() });
        return response.data;
    },
    deleteLog: async (projectId: string, logId: string): Promise<void> => {
        await axios.delete(`${API_URL}/projects/${projectId}/logs/${logId}`, { headers: await getAuthHeaders() });
    }
};
"""

with open(filepath, 'a') as f:
    f.write(addition)
print("Updated constructorWorkflowService.ts")
