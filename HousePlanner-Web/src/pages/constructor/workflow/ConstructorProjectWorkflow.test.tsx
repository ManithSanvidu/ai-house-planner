import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ConstructorProjectWorkflow } from './ConstructorProjectWorkflow';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';

vi.mock('../../../services/constructorWorkflowService', () => ({
  constructorWorkflowService: {
    getProjectDetails: vi.fn(),
    getProjectProgress: vi.fn(),
    getWorkflowLogs: vi.fn(),
    setEstimatedDuration: vi.fn(),
  },
}));

const service = vi.mocked(constructorWorkflowService);

describe('ConstructorProjectWorkflow', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    service.getProjectDetails.mockResolvedValue({
      id: 'project-1234',
      workflowStateId: 'workflow-1',
      contractorId: 'constructor-1',
      status: 'active',
      createdAt: '2026-09-22T00:00:00Z',
      updatedAt: '2026-09-22T00:00:00Z',
      aiEstimatedTotalDurationDays: 30,
      plannedTotalDurationDays: 45,
      constructionPhases: [{
        id: 'phase-1', projectId: 'project-1234', phaseName: 'Foundation',
        sequenceOrder: 1, aiEstimatedDurationDays: 20, plannedDurationDays: 20, status: 'in_progress',
      }],
      design: {
        designId: 'design-1', version: 2, floorCount: 1,
        totalBuiltUpAreaSqft: 900, foundationType: 'slab',
        layoutJson: '{"design_id":"design-1","floor_count":1,"total_built_up_area_sqft":900,"rooms":[]}',
      },
      cost: {
        materialCostLkr: 8_400_000, labourCostLkr: 2_940_000,
        totalCostLkr: 11_340_000, budgetDeltaPercent: 94.5,
      },
    });
    service.getProjectProgress.mockResolvedValue({
      totalEstimatedDays: 20, daysCompleted: 1, daysRemaining: 19,
      overallProgress: 5, delayStatus: 'On Schedule',
    });
    service.getWorkflowLogs.mockResolvedValue([]);
  });

  it('shows the approved design and its persisted cost beside the constructor log', async () => {
    render(
      <MemoryRouter initialEntries={['/constructor/workflow/project-1234']}>
        <Routes>
          <Route path="/constructor/workflow/:projectId" element={<ConstructorProjectWorkflow />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('Approved Design')).toBeTruthy();
    expect(screen.getByText('LKR 11,340,000')).toBeTruthy();
    expect(screen.getByText('900 sq ft')).toBeTruthy();
    expect(screen.getAllByText('Foundation').length).toBeGreaterThan(0);
  });
});
