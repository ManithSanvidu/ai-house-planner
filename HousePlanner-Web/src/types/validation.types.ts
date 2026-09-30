export interface ValidationRequest {
 id: string;
 clientName: string | null;
 submissionDate: string;
 status: string;
 budget: number | null;
 landSize: number | null;
 bedrooms: number | null;
 floors: number | null;
 style: string | null;
 designVersion?: number | null;
 bathrooms?: number | null;
 area?: number | null;
}

export interface ValidationRequestDetails extends ValidationRequest {
 clientEmail: string | null;
 terrainType: string | null;
 architectReview: string | null;
 decisionAt: string | null;
 cost: import('../services/workflowService').CostSummaryDto | null;
 approvalEligibility: {
  canApprove: boolean;
  reason: string | null;
  budgetStatus: 'within_budget' | 'at_budget' | 'over_budget' | 'unavailable';
 };
 design: {
  designId: string;
  version: number;
  floorCount: number;
  totalBuiltUpAreaSqft: number;
  layoutJson: string;
  rooms: Array<{
   roomId: string;
   roomType: string;
   name: string | null;
   floorNumber: number;
   x: number;
   y: number;
   width: number;
   length: number;
   wallHeight: number;
   doors: Array<{ wall: string; offset: number; width: number }>;
   windows: Array<{ wall: string; offset: number; width: number }>;
  }>;
 } | null;
}

export interface ArchitectReviewDto {
 review?: string;
}
