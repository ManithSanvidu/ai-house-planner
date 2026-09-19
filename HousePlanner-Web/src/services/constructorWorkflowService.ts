import axios from 'axios';

const API_URL = 'http://localhost:5265/api/constructor/workflow';

// Add the auth token to requests
const getAuthHeaders = () => {
    const token = localStorage.getItem('token');
    return {
        Authorization: `Bearer ${token}`
    };
};

export interface ConstructionPhase {
    id: string;
    projectId: string;
    phaseName: string;
    sequenceOrder: number;
    estimatedDurationDays: number;
    status: string;
    startDate?: string;
    endDate?: string;
}

export interface ConstructorWorkflowProject {
    id: string;
    workflowStateId: string;
    contractorId?: string;
    status: string;
    createdAt: string;
    updatedAt: string;
    constructionPhases: ConstructionPhase[];
}

export interface ConstructorWorkflowLog {
    id: string;
    projectId: string;
    constructorId: string;
    constructionPhaseId?: string;
    dayNumber: number;
    date: string;
    completedWork: string;
    progressPercentage: number;
    challenges?: string;
    issues?: string;
    resolution?: string;
    tomorrowPlan?: string;
    additionalNotes?: string;
    status: string;
    createdAt: string;
    updatedAt: string;
    constructionPhase?: ConstructionPhase;
}

export interface ProjectProgress {
    totalEstimatedDays: number;
    daysCompleted: number;
    daysRemaining: number;
    overallProgress: number;
    delayStatus: string;
    latestLog?: ConstructorWorkflowLog;
}

export const constructorWorkflowService = {
    getProjects: async (): Promise<ConstructorWorkflowProject[]> => {
        const response = await axios.get(`${API_URL}/projects`, { headers: getAuthHeaders() });
        return response.data;
    },

    getProjectDetails: async (projectId: string): Promise<ConstructorWorkflowProject> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}`, { headers: getAuthHeaders() });
        return response.data;
    },

    getWorkflowLogs: async (projectId: string): Promise<ConstructorWorkflowLog[]> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}/logs`, { headers: getAuthHeaders() });
        return response.data;
    },

    getProjectProgress: async (projectId: string): Promise<ProjectProgress> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}/progress`, { headers: getAuthHeaders() });
        return response.data;
    },

    createLog: async (log: Partial<ConstructorWorkflowLog>): Promise<ConstructorWorkflowLog> => {
        const response = await axios.post(`${API_URL}/logs`, log, { headers: getAuthHeaders() });
        return response.data;
    },

    updateLog: async (logId: string, log: Partial<ConstructorWorkflowLog>): Promise<ConstructorWorkflowLog> => {
        const response = await axios.put(`${API_URL}/logs/${logId}`, log, { headers: getAuthHeaders() });
        return response.data;
    }
};
