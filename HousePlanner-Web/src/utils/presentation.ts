type PlanPresentationInput = {
 designCode?: string | null;
 name?: string | null;
 bedrooms: number;
};

const topologyLabels: Record<string, string> = {
 COMPACT_RECTANGLE: 'Compact Rectangle',
 CENTRAL_CORE: 'Central Core',
 L_SHAPE: 'L-Shaped',
 SPLIT_ZONE: 'Split Zone',
 WIDE_SHALLOW: 'Wide Layout',
 NARROW_DEEP: 'Narrow Layout',
 LINEAR: 'Linear',
};

const statusLabels: Record<string, string> = {
 DESIGN_GENERATED: 'Design Ready',
 SELECTED_BY_CLIENT: 'Design Selected',
 AWAITING_APPROVAL: 'Ready for Your Review',
 CLIENT_REVIEW: 'Ready for Your Review',
 SUBMITTED_FOR_ARCHITECT_REVIEW: 'Sent to Architect',
 AWAITING_ARCHITECT_REVIEW: 'Sent to Architect',
 APPROVED: 'Approved',
 REVISION_REQUESTED: 'Revision Requested',
 RUNNING: 'Creating Your Design',
 PENDING: 'Creating Your Design',
 FAILED: 'Design Needs Attention',
};

const normalized = (value?: string | null) => (value || '').trim().replace(/[ -]+/g, '_').toUpperCase();

export function formatTopology(value?: string | null): string {
 const key = normalized(value);
 if (!key) return 'Practical Layout';
 return topologyLabels[key] || key.toLowerCase().split('_').map(word => word[0]?.toUpperCase() + word.slice(1)).join(' ');
}

export function getPlanTopology(plan: Pick<PlanPresentationInput, 'designCode' | 'name'>): string | null {
 const source = normalized(`${plan.designCode || ''}_${plan.name || ''}`);
 return Object.keys(topologyLabels).find(key => source.includes(key)) || null;
}

export function getCustomerPlanName(plan: PlanPresentationInput): string {
 const topology = getPlanTopology(plan);
 const bedrooms = `${plan.bedrooms} Bedroom`;
 switch (topology) {
  case 'COMPACT_RECTANGLE': return `Compact ${bedrooms} Home`;
  case 'L_SHAPE': return `${bedrooms} L-Shaped Home`;
  case 'SPLIT_ZONE': return `${bedrooms} Split-Zone Home`;
  case 'CENTRAL_CORE': return `${bedrooms} Central-Core Home`;
  case 'LINEAR': return `Linear ${bedrooms} Home`;
  case 'WIDE_SHALLOW': return `Wide ${bedrooms} Home`;
  case 'NARROW_DEEP': return `Narrow ${bedrooms} Home`;
  default: return `${bedrooms} Home`;
 }
}

export function formatWorkflowStatus(value?: string | null): string {
 const key = normalized(value);
 return statusLabels[key] || (key ? key.toLowerCase().split('_').map(word => word[0]?.toUpperCase() + word.slice(1)).join(' ') : 'Status unavailable');
}

export function formatTerrain(value?: string | null): string {
 const key = normalized(value);
 if (!key || key === 'UNKNOWN') return 'Not specified';
 if (key === 'ALL') return 'Suitable for varied sites';
 return `${key[0]}${key.slice(1).toLowerCase().replaceAll('_', ' ')}`;
}

export function formatFoundation(value?: string | null): string {
 const key = normalized(value);
 if (!key || key === 'UNKNOWN') return 'Not specified';
 return key.toLowerCase().split('_').map(word => word[0]?.toUpperCase() + word.slice(1)).join(' ');
}

export function formatArea(value: number): string {
 return `${Math.round(value).toLocaleString()} sq ft`;
}

export function countLabel(value: number, singular: string, plural = `${singular}s`): string {
 return `${value} ${value === 1 ? singular : plural}`;
}

export function formatFloorName(floor: number): string {
 if (floor === 1) return 'Ground Floor';
 if (floor === 2) return 'First Floor';
 return `Floor ${floor}`;
}

export function formatRoomName(value?: string | null): string {
 if (!value) return 'Room';
 return value.replaceAll('_', ' ').split(' ').map(word => word ? word[0].toUpperCase() + word.slice(1) : '').join(' ');
}

export function formatGenerationMode(value?: string | null): string {
 const key = normalized(value);
 if (key === 'AI_ADAPTED_TEMPLATE') return 'AI-assisted design';
 if (key === 'DETERMINISTIC_FALLBACK') return 'Validated template';
 return key ? key.toLowerCase().split('_').map(word => word[0]?.toUpperCase() + word.slice(1)).join(' ') : 'Generated design';
}
