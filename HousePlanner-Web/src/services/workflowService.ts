import apiClient from './apiClient';

// ──────────────────────────────────────────────────
// Shared TypeScript interfaces matching the backend DTOs
// ──────────────────────────────────────────────────

export interface OpeningDto {
  wall: 'north' | 'south' | 'east' | 'west';
  offset: number;
  width: number;
}

export interface RoomSummaryDto {
  roomId: string;
  roomType: string;
  name: string | null;
  floorNumber: number;
  x: number;
  y: number;
  width: number;
  length: number;
  areaSqft: number;
  wallHeight: number;
  doors: OpeningDto[] | null;
  windows: OpeningDto[] | null;
}

export interface HouseDesignSummaryDto {
  designId: string;
  version: number;
  floorCount: number;
  totalBuiltUpAreaSqft: number;
  foundationType: string;
  templateId: string | null;
  terrainType: string | null;
  isCurrent: boolean;
  rooms: RoomSummaryDto[];
  templateFamily?: string | null;
  designSeed?: number | null;
  designScore?: number | null;
  geometryFingerprint?: string | null;
  groundFootprintSqft?: number | null;
  entrances?: { room_id: string; wall: OpeningDto['wall']; offset: number; width: number }[];
  plotConstraints?: { dimensions_estimated?: boolean };
  candidateSummary?: { 
    notes?: string[],
    valid_count?: number,
    rejected_count?: number,
    generated_count?: number,
    unique_valid_count?: number,
    generation_mode?: string,
    selection_method?: string
  };

}

export interface WorkflowStatusResponseDto {
  workflowId: string;
  status: string;
  terrainType: string | null;
  slopeEstimate: string | null;
  design: HouseDesignSummaryDto | null;
  cost: any | null; // Expand when Component C is integrated
  approvalStatus: string;
  failureReason?: string | null;
  constructionPlan?: any | null;
}

export interface DesignHistoryDto {
  designId: string; version: number; isCurrent: boolean; isPreferred: boolean; topology: string | null;
  bedrooms: number; bathrooms: number; floorCount: number; totalBuiltUpAreaSqft: number;
  foundationType: string; generationMode: string | null; selectedBasePlan: string | null;
  geometryFingerprint: string | null; createdAt: string;
}
export interface WorkflowDesignHistoryDto {
  workflowId: string; status: string; preferredHouseDesignId: string | null; createdAt: string;
  designs: DesignHistoryDto[];
}

export interface StartDesignRequest {
  basePreDesignedPlanId?: string;
  planSelectionMode?: 'use' | 'reference' | 'override';
  landSizePerches: number;
  manualTerrainType?: string;
  designSeed?: number;
  preferences: {
    bedrooms: number; bathrooms: number; floors: number; architecturalStyle?: string;
    openPlan?: boolean; masterEnsuite?: boolean; separateDining?: boolean;
    homeOffice?: boolean; balcony?: boolean; veranda?: boolean; utilityRoom?: boolean;
    parkingRequired?: boolean; accessibility?: boolean; spacePriority?: string;
    circulationPreference?: 'space_efficient';
  };
  plotConstraints?: {
    road_side: string; plot_width_ft?: number; plot_length_ft?: number;
    north_direction?: string; entrance_side?: string;
    setbacks?: { front?: number; rear?: number; left?: number; right?: number };
  };
}

// ──────────────────────────────────────────────────
// API calls
// ──────────────────────────────────────────────────

export const workflowService = {
  startDesign: async (request: StartDesignRequest): Promise<{ workflowId: string }> => {
    const response = await apiClient.post('/ai-generation/generate', request);
    return response.data;
  },
  getWorkflowStatus: async (id: string, designId?: string): Promise<WorkflowStatusResponseDto> => {
    const response = await apiClient.get<WorkflowStatusResponseDto>(`/workflows/${id}/status`, { params: designId ? { designId } : undefined });
    return response.data;
  },
  getMyDesigns: async (): Promise<WorkflowDesignHistoryDto[]> =>
    (await apiClient.get<WorkflowDesignHistoryDto[]>('/workflows/designs')).data,
  getDesigns: async (id: string): Promise<WorkflowDesignHistoryDto> =>
    (await apiClient.get<WorkflowDesignHistoryDto>(`/workflows/${id}/designs`)).data,
  selectDesign: async (workflowId: string, designId: string) =>
    (await apiClient.post(`/workflows/${workflowId}/designs/${designId}/select`)).data,
  submitArchitectReview: async (workflowId: string) =>
    (await apiClient.post(`/workflows/${workflowId}/submit-architect-review`)).data,

  approveWorkflow: async (id: string, decision: 'approve' | 'reject' | 'request_revision', notes?: string) => {
    const response = await apiClient.post(`/workflows/${id}/approve`, { decision, revisionNotes: notes });
    return response.data;
  }
};
