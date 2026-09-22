import apiClient from './apiClient';
import type { CostSummaryDto } from './workflowService';

export interface ApprovedDesign {
  designId: string; workflowId: string; version: number; floorCount: number;
  area: number; approvedAt: string; title: string; bedrooms: number; bathrooms: number;
  layoutJson: string; cost: CostSummaryDto | null;
}
export interface ConstructorProfile { id: string; name: string; }
export interface ConstructionRequestItem {
  id: string; projectId: string; houseDesignId: string; constructorName: string;
  status: string; declineReason?: string; requestedAt: string; respondedAt?: string; designVersion: number;
}
export interface ConstructionProjectSummary {
  id: string; status: string; createdAt: string; updatedAt: string; houseDesignId: string;
  constructorName: string; designVersion: number; currentPhase?: string;
  cost: CostSummaryDto | null;
}
export interface CustomerConstruction {
  pendingRequests: ConstructionRequestItem[];
  declinedRequests: ConstructionRequestItem[];
  activeProjects: ConstructionProjectSummary[];
  completedProjects: ConstructionProjectSummary[];
}

export interface CustomerConstructionProjectDetails {
  project: ConstructionProjectSummary;
  progress: { overallProgress: number };
  phases: Array<{ id: string; phaseName: string; status: string }>;
  logs: Array<Record<string, unknown>>;
  activity: Array<{ date: string; count: number; intensity: number }>;
}

export const customerConstructionService = {
  approvedDesigns: () => apiClient.get<ApprovedDesign[]>('/customer/construction/approved-designs').then(r => r.data),
  constructors: () => apiClient.get<ConstructorProfile[]>('/customer/construction/constructors').then(r => r.data),
  request: (houseDesignId: string, constructorId: string) =>
    apiClient.post('/customer/construction/requests', { houseDesignId, constructorId }).then(r => r.data),
  overview: () => apiClient.get<CustomerConstruction>('/customer/construction').then(r => r.data),
  project: (projectId: string) => apiClient.get<CustomerConstructionProjectDetails>(`/customer/construction/projects/${projectId}`).then(r => r.data)
};

