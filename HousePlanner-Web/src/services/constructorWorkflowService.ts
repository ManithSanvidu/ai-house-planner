import axios from 'axios';
import { supabase } from '../lib/supabase';

const API_URL = 'http://localhost:5265/api/constructor/workflow';

// Add the auth token to requests
const getAuthHeaders = async () => {
    const { data: { session } } = await supabase.auth.getSession();
    if (!session) return {};
    return {
        Authorization: `Bearer ${session.access_token}`
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
        const response = await axios.get(`${API_URL}/projects`, { headers: await getAuthHeaders() });
        return response.data;
    },

    getProjectDetails: async (projectId: string): Promise<ConstructorWorkflowProject> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}`, { headers: await getAuthHeaders() });
        return response.data;
    },

    getWorkflowLogs: async (projectId: string): Promise<ConstructorWorkflowLog[]> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}/logs`, { headers: await getAuthHeaders() });
        return response.data;
    },

    getProjectProgress: async (projectId: string): Promise<ProjectProgress> => {
        const response = await axios.get(`${API_URL}/projects/${projectId}/progress`, { headers: await getAuthHeaders() });
        return response.data;
    },

    createLog: async (log: Partial<ConstructorWorkflowLog>): Promise<ConstructorWorkflowLog> => {
        const response = await axios.post(`${API_URL}/logs`, log, { headers: await getAuthHeaders() });
        return response.data;
    },

    updateLog: async (logId: string, log: Partial<ConstructorWorkflowLog>): Promise<ConstructorWorkflowLog> => {
        const response = await axios.put(`${API_URL}/logs/${logId}`, log, { headers: await getAuthHeaders() });
        return response.data;
    },

    searchProject: async (projectId: string) => {
        const response = await axios.get(`${API_URL}/search/${projectId}`, { headers: await getAuthHeaders() });
        return response.data;
    },

    requestProject: async (projectId: string) => {
        const response = await axios.post(`${API_URL}/request/${projectId}`, {}, { headers: await getAuthHeaders() });
        return response.data;
    },

    approveRequest: async (requestId: string) => {
        const response = await axios.post(`${API_URL}/approve/${requestId}`, {}, { headers: await getAuthHeaders() });
        return response.data;
    },

    getProjectRequests: async (projectId: string) => {
        const response = await axios.get(`${API_URL}/requests/project/${projectId}`, { headers: await getAuthHeaders() });
        return response.data;
    },

    getConstructorRequests: async () => {
        const response = await axios.get(`${API_URL}/requests/constructor`, { headers: await getAuthHeaders() });
        return response.data;
    },

    acceptRequest: async (requestId: string) => {
        const response = await axios.post(`${API_URL}/requests/${requestId}/accept`, {}, { headers: await getAuthHeaders() });
        return response.data;
    },

    declineRequest: async (requestId: string, reason?: string) => {
        const response = await axios.post(`${API_URL}/requests/${requestId}/decline`, { reason }, { headers: await getAuthHeaders() });
        return response.data;
    },

    setEstimatedDuration: async (projectId: string, estimatedDays: number) => {
        const response = await axios.post(`${API_URL}/projects/${projectId}/duration`, estimatedDays, { 
            headers: {
                ...await getAuthHeaders(),
                'Content-Type': 'application/json'
            }
        });
        return response.data;
    }
};
