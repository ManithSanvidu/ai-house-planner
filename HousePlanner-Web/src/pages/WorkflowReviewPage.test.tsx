import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { WorkflowReviewPage } from './WorkflowReviewPage';
import { workflowService } from '../services/workflowService';

vi.mock('../services/workflowService', () => ({
  workflowService: { getWorkflowStatus: vi.fn(), approveWorkflow: vi.fn() },
}));
vi.mock('../services/validationRequestService', () => ({
  validationRequestService: { create: vi.fn() },
}));
vi.mock('../components/floorplan/FloorPlanViewer', () => ({
  FloorPlanViewer: ({ data }: { data: { design_id: string } }) => <div>viewer:{data.design_id}</div>,
}));

const status = (version: number) => ({
  workflowId: 'workflow-1', status: 'awaiting_approval', terrainType: 'flat', slopeEstimate: 'flat',
  approvalStatus: 'pending', cost: null, constructionPlan: null,
  design: {
    designId: `design-${version}`, version, floorCount: 1, totalBuiltUpAreaSqft: 900,
    foundationType: 'slab', templateId: 'HP-TEST', templateFamily: 'COMPACT_RECTANGLE',
    terrainType: 'flat', isCurrent: true, designScore: 91,
    rooms: [{ roomId: 'bed-1', roomType: 'bedroom_1', name: 'Bedroom', floorNumber: 1,
      x: 0, y: 0, width: 10, length: 10, areaSqft: 100, wallHeight: 9, doors: [], windows: [] }],
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
    vi.mocked(workflowService.approveWorkflow).mockResolvedValue({});

    render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
      <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
    </MemoryRouter>);

    expect(await screen.findByText('viewer:design-1')).toBeDefined();
    fireEvent.click(screen.getByRole('button', { name: 'Generate Another' }));

    await waitFor(() => expect(workflowService.approveWorkflow)
      .toHaveBeenCalledWith('workflow-1', 'request_revision', 'Generate Another'));
    expect(await screen.findByText('viewer:design-2')).toBeDefined();
    expect(screen.getByText('COMPACT RECTANGLE')).toBeDefined();
    expect(screen.getByText('91')).toBeDefined();
  });
});
