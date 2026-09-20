import apiClient from './apiClient';

export interface ApprovedDesign {
  designId: string; workflowId: string; version: number; floorCount: number;
  area: number; approvedAt: string; title: string; bedrooms: number; bathrooms: number;
}
export interface ConstructorProfile { id: string; name: string; }
export interface ConstructionRequestItem {
  id: string; projectId: string; houseDesignId: string; constructorName: string;
  status: string; declineReason?: string; requestedAt: string; respondedAt?: string; designVersion: number;
}
export interface ConstructionProjectSummary {
  id: string; status: string; createdAt: string; updatedAt: string; houseDesignId: string;
  constructorName: string; designVersion: number; currentPhase?: string;
}
export interface CustomerConstruction {
  pendingRequests: ConstructionRequestItem[];
  declinedRequests: ConstructionRequestItem[];
  activeProjects: ConstructionProjectSummary[];
  completedProjects: ConstructionProjectSummary[];
}

export const customerConstructionService = {
  approvedDesigns: () => apiClient.get<ApprovedDesign[]>('/v1/customer/construction/approved-designs').then(r => r.data),
  constructors: () => apiClient.get<ConstructorProfile[]>('/v1/customer/construction/constructors').then(r => r.data),
  request: (houseDesignId: string, constructorId: string) =>
    apiClient.post('/v1/customer/construction/requests', { houseDesignId, constructorId }).then(r => r.data),
  overview: () => apiClient.get<CustomerConstruction>('/v1/customer/construction').then(r => r.data),
  project: (projectId: string) => apiClient.get(`/v1/customer/construction/projects/${projectId}`).then(r => r.data)
};

