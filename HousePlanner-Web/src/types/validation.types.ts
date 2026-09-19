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
}

export interface ValidationRequestDetails extends ValidationRequest {
  clientEmail: string | null;
  terrainType: string | null;
  architectReview: string | null;
  decisionAt: string | null;
  design: {
    designId: string;
    layoutJson: string;
  } | null;
}

export interface ArchitectReviewDto {
  review?: string;
}
