import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { WorkflowReviewPage } from './WorkflowReviewPage';
import { workflowService } from '../services/workflowService';

vi.mock('../services/workflowService', () => ({
  workflowService: { getWorkflowStatus: vi.fn(), regenerateDesign: vi.fn() },
}));
vi.mock('../services/validationRequestService', () => ({
  validationRequestService: { create: vi.fn() },
}));
vi.mock('../components/floorplan/FloorPlanViewer', () => ({
  FloorPlanViewer: ({ data }: { data: { design_id: string } }) => <div>viewer:{data.design_id}</div>,
}));
vi.mock('../features/auth/useAuth', () => ({
  default: () => ({ user: { role: 'Customer' } }),
}));

const status = (version: number) => ({
  workflowId: 'workflow-1', status: 'design_generated', terrainType: 'flat', slopeEstimate: 'flat',
  approvalStatus: 'pending', cost: null, constructionPlan: null,
  design: {
    designId: `design-${version}`, version, floorCount: 1, totalBuiltUpAreaSqft: 900,
    foundationType: 'slab', templateId: 'HP-TEST', templateFamily: 'COMPACT_RECTANGLE',
    terrainType: 'flat', isCurrent: true, designScore: 91,
    rooms: [
      { roomId: 'bed-1', roomType: 'bedroom_1', name: 'Bedroom 1', floorNumber: 1, x: 0, y: 0, width: 10, length: 10, areaSqft: 100, wallHeight: 9, doors: [], windows: [] },
      { roomId: 'bed-2', roomType: 'bedroom_2', name: 'Bedroom 2', floorNumber: 1, x: 10, y: 0, width: 10, length: 10, areaSqft: 100, wallHeight: 9, doors: [], windows: [] },
      { roomId: 'bed-3', roomType: 'bedroom_3', name: 'Bedroom 3', floorNumber: 1, x: 20, y: 0, width: 10, length: 10, areaSqft: 100, wallHeight: 9, doors: [], windows: [] },
      { roomId: 'bath-1', roomType: 'bathroom_1', name: 'Bathroom', floorNumber: 1, x: 0, y: 10, width: 6, length: 8, areaSqft: 48, wallHeight: 9, doors: [], windows: [] },
    ],
  },
});

describe('WorkflowReviewPage revision refresh', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('alert', vi.fn());
  });

  it('requests Generate Another and refreshes to the newly persisted current version', async () => {
    vi.mocked(workflowService.getWorkflowStatus)
      .mockResolvedValueOnce(status(1))
      .mockResolvedValue(status(2));
    vi.mocked(workflowService.regenerateDesign).mockResolvedValue({});

    render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
      <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
    </MemoryRouter>);

    expect(await screen.findByText('viewer:design-1')).toBeDefined();
    fireEvent.click(screen.getByRole('button', { name: 'Generate Another Design' }));

    await waitFor(() => expect(workflowService.regenerateDesign)
      .toHaveBeenCalledWith('workflow-1', 'design-1'));
    expect(await screen.findByText('viewer:design-2')).toBeDefined();
    expect(screen.getByText('Compact Rectangle')).toBeDefined();
    expect(screen.getByText('Architectural quality score: 91')).toBeDefined();
  });

  it('shows a homeowner summary and friendly workflow status', async () => {
    vi.mocked(workflowService.getWorkflowStatus).mockResolvedValue(status(1));
    render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
      <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
    </MemoryRouter>);

    expect(await screen.findByText('Design Ready')).toBeTruthy();
    expect(screen.getByText(/3 Bedrooms.*1 Bathroom.*1 Floor.*900 sq ft/)).toBeTruthy();
    expect(screen.getAllByText('Bedrooms').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Bathrooms').length).toBeGreaterThan(0);
  });
});
