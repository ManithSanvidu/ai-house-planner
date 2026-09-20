import apiClient from './apiClient';

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
    notes?: string[];
    valid_count?: number;
    rejected_count?: number;
    generated_count?: number;
    unique_valid_count?: number;
    generation_mode?: string;
    selection_method?: string;
  };
}

export interface CostSummaryDto {
  materialCostLkr: number;
  labourCostLkr: number;
  totalCostLkr: number;
  budgetDeltaPercent: number;
}

export interface ConstructionPhaseDto {
  id: number;
  name: string;
  description: string;
  duration_days: number;
  depends_on: number[];
  start_day: number;
  end_day: number;
  status: string;
}

export interface ConstructionPlanSummaryDto {
  project_summary: {
    estimated_duration_days: number;
    estimated_duration_months: number;
    target_duration_days: number | null;
    schedule_status: string;
  };
  phases: ConstructionPhaseDto[];
  critical_path: string[];
  assumptions: string[];
  optimization_notes: string[];
}

export interface WorkflowStatusResponseDto {
  workflowId: string;
  status: string;
  terrainType: string | null;
  slopeEstimate: string | null;
  design: HouseDesignSummaryDto | null;
  cost: CostSummaryDto | null;
  approvalStatus: string;
  failureReason?: string | null;
  preferredHouseDesignId?: string | null;
  architectReviewStatus?: string | null;
  architectFeedback?: string | null;
  constructionPlan?: ConstructionPlanSummaryDto | null;
}

export interface DesignHistoryDto {
  designId: string;
  version: number;
  isCurrent: boolean;
  isPreferred: boolean;
  isArchived: boolean;
  topology: string | null;
  bedrooms: number;
  bathrooms: number;
  floorCount: number;
  totalBuiltUpAreaSqft: number;
  foundationType: string;
  generationMode: string | null;
  selectedBasePlan: string | null;
  geometryFingerprint: string | null;
  createdAt: string;
  suitabilityScore: number | null;
  architecturalQualityScore: number | null;
  previewRooms: { roomType: string; floor: number; x: number; y: number; width: number; length: number }[];
}

export interface WorkflowDesignHistoryDto {
  workflowId: string;
  status: string;
  preferredHouseDesignId: string | null;
  createdAt: string;
  designs: DesignHistoryDto[];
  projectId?: string;
  architectReviewStatus?: string | null;
  architectFeedback?: string | null;
}

export interface StartDesignRequest {
  basePreDesignedPlanId?: string;
  planSelectionMode?: 'use';
  budgetLkr?: number;
  landSizePerches: number;
  manualTerrainType?: string;
  designSeed?: number;
  preferences: {
    bedrooms: number;
    bathrooms: number;
    floors: number;
    architecturalStyle?: string;
    openPlan?: boolean;
    masterEnsuite?: boolean;
    separateDining?: boolean;
    homeOffice?: boolean;
    balcony?: boolean;
    veranda?: boolean;
    utilityRoom?: boolean;
    parkingRequired?: boolean;
    accessibility?: boolean;
    spacePriority?: string;
    circulationPreference?: 'space_efficient';
  };
  plotConstraints?: {
    road_side: string;
    plot_width_ft?: number;
    plot_length_ft?: number;
    north_direction?: string;
    entrance_side?: string;
    setbacks?: { front?: number; rear?: number; left?: number; right?: number };
  };
}

export const workflowService = {
  startDesign: async (request: StartDesignRequest): Promise<{ workflowId: string }> => {
    const response = await apiClient.post('/ai-generation/generate', request);
    return response.data;
  },
  getWorkflowStatus: async (id: string, designId?: string): Promise<WorkflowStatusResponseDto> => {
    const response = await apiClient.get<WorkflowStatusResponseDto>(`/workflows/${id}/status`, {
      params: designId ? { designId } : undefined,
    });
    return response.data;
  },
  getMyDesigns: async (): Promise<WorkflowDesignHistoryDto[]> =>
    (await apiClient.get<WorkflowDesignHistoryDto[]>('/workflows/designs')).data,
  getDesigns: async (id: string): Promise<WorkflowDesignHistoryDto> =>
    (await apiClient.get<WorkflowDesignHistoryDto>(`/workflows/${id}/designs`)).data,
  selectDesign: async (workflowId: string, designId: string) =>
    (await apiClient.post(`/workflows/${workflowId}/designs/${designId}/select`)).data,
  clearDesignSelection: async (workflowId: string) =>
    (await apiClient.delete(`/workflows/${workflowId}/design-selection`)).data,
  removeDesign: async (workflowId: string, designId: string) =>
    (await apiClient.delete(`/workflows/${workflowId}/designs/${designId}`)).data,
  submitArchitectReview: async (workflowId: string) =>
    (await apiClient.post(`/workflows/${workflowId}/submit-architect-review`)).data,
  approveWorkflow: async (
    id: string,
    decision: 'approve' | 'reject' | 'request_revision',
    notes?: string,
  ) => {
    const response = await apiClient.post(`/workflows/${id}/approve`, {
      decision,
      revisionNotes: notes,
    });
    return response.data;
  },
};
