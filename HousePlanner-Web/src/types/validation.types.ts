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

 export interface ValidationResultRule {
  ruleName: string | null;
  passed: boolean;
  status?: "PASS" | "FAIL" | "NOT_APPLICABLE";
  reason: string | null;
 expected: any;
 actual: any;
}

export interface ValidationResult {
 passed: boolean;
 rules: ValidationResultRule[];
 errors: string[];
 summary: string | null;
 revisionReason: string | null;
}

export interface ValidationRequestDetails extends ValidationRequest {
 clientEmail: string | null;
 terrainType: string | null;
 foundationType?: string | null;
 architectReview: string | null;
 decisionAt: string | null;
 validationResult?: ValidationResult | null;
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
