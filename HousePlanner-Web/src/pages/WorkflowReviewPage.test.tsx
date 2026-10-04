import { act, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { WorkflowReviewPage } from './WorkflowReviewPage';
import { workflowService } from '../services/workflowService';

vi.mock('../services/workflowService', () => ({
 workflowService: {
  getWorkflowStatus: vi.fn(),
  regenerateDesign: vi.fn(),
  getDesignVisualization: vi.fn(),
 },
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
  vi.mocked(workflowService.getDesignVisualization).mockResolvedValue({ status: 'generating', imageUrl: null });
  vi.stubGlobal('alert', vi.fn());
 });

 it('displays current home requirements without offering another design generation', async () => {
  vi.mocked(workflowService.getWorkflowStatus)
   .mockResolvedValueOnce(status(1));
  vi.mocked(workflowService.regenerateDesign).mockResolvedValue({});

  render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
   <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
  </MemoryRouter>);

  expect(await screen.findByText('Architectural Visualization Preview')).toBeDefined();
  expect(screen.getByText('Your Home Requirements')).toBeDefined();
  
  // Checking requirement values
  expect(screen.getAllByText('Bedrooms').length).toBeGreaterThan(0);
  expect(screen.getAllByText('Bathrooms').length).toBeGreaterThan(0);

  expect(screen.queryByRole('button', { name: 'Generate Another Design' })).toBeNull();
  expect(workflowService.regenerateDesign).not.toHaveBeenCalled();
 });

 it('shows a homeowner summary and friendly workflow status', async () => {
  vi.mocked(workflowService.getWorkflowStatus).mockResolvedValue(status(1));
  render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
   <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
  </MemoryRouter>);

  expect(await screen.findByText('Your Home Requirements')).toBeTruthy();
  expect(screen.getAllByText('Bedrooms').length).toBeGreaterThan(0);
  expect(screen.getAllByText('Bathrooms').length).toBeGreaterThan(0);
 });

 it('renders the persisted visualization URL when generation completes', async () => {
  vi.mocked(workflowService.getWorkflowStatus).mockResolvedValue(status(1));
  vi.mocked(workflowService.getDesignVisualization).mockResolvedValue({
   status: 'completed', imageUrl: 'https://project.supabase.co/storage/v1/object/sign/ai-visualizations/example.png?token=test',
  });
  render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
   <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
  </MemoryRouter>);

  const image = await screen.findByRole('img');
  expect(image.getAttribute('alt')).toBe('AI Visualization');
  expect(image.getAttribute('src')).toBe('https://project.supabase.co/storage/v1/object/sign/ai-visualizations/example.png?token=test');
  expect(screen.queryByText('Generating AI visualization...')).toBeNull();
 });

 it('hides technical coordinates and room summaries from customers', async () => {
  vi.mocked(workflowService.getWorkflowStatus).mockResolvedValue(status(1));
  vi.mocked(workflowService.getDesignVisualization).mockResolvedValue({
   status: 'completed', imageUrl: 'https://project.supabase.co/storage/v1/object/sign/ai-visualizations/example.png?token=test',
  });
  render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
   <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
  </MemoryRouter>);

  expect(await screen.findByRole('img')).toBeTruthy();
  expect(screen.queryByText('Technical Floor Plan')).toBeNull();
  expect(screen.queryByText('viewer:design-1')).toBeNull();
  expect(screen.queryByText('Rooms')).toBeNull();
 });

 it('stops polling and offers retry when workflow fails', async () => {
  vi.useFakeTimers();
  vi.mocked(workflowService.getWorkflowStatus).mockResolvedValue({
   ...status(1), design: null, status: 'failed',
   failureReason: 'Workflow execution did not complete.',
  } as any);

  render(<MemoryRouter initialEntries={['/dashboard/workflows/workflow-1']}>
   <Routes><Route path="/dashboard/workflows/:id" element={<WorkflowReviewPage />} /></Routes>
  </MemoryRouter>);

  await act(async () => { await Promise.resolve(); await Promise.resolve(); });
  expect(screen.getByText('Workflow execution did not complete.')).toBeTruthy();
  expect(screen.getByRole('link', { name: 'Try Again' }).getAttribute('href')).toBe('/dashboard/new-project');

  await act(async () => { await vi.advanceTimersByTimeAsync(6_000); });
  expect(workflowService.getWorkflowStatus).toHaveBeenCalledTimes(1);
  vi.useRealTimers();
 });
});
