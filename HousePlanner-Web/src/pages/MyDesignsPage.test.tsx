import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import MyDesignsPage from './MyDesignsPage';
import { workflowService } from '../services/workflowService';

vi.mock('../services/workflowService', () => ({ workflowService: { getMyDesigns: vi.fn(), selectDesign: vi.fn(), submitArchitectReview: vi.fn() } }));
const project = { workflowId: 'workflow-1234', status: 'design_generated', preferredHouseDesignId: null, createdAt: '2026-01-01', designs: [
  { designId: 'd2', version: 2, isCurrent: true, isPreferred: false, topology: 'L_SHAPE', bedrooms: 3, bathrooms: 2, floorCount: 1, totalBuiltUpAreaSqft: 1200, foundationType: 'slab', generationMode: 'ai_adapted_template', selectedBasePlan: 'P2', geometryFingerprint: 'b', createdAt: '2026-01-02' },
  { designId: 'd1', version: 1, isCurrent: false, isPreferred: false, topology: 'COMPACT_RECTANGLE', bedrooms: 3, bathrooms: 2, floorCount: 1, totalBuiltUpAreaSqft: 1100, foundationType: 'slab', generationMode: 'ai_adapted_template', selectedBasePlan: 'P1', geometryFingerprint: 'a', createdAt: '2026-01-01' }
] };

beforeEach(() => { vi.mocked(workflowService.getMyDesigns).mockResolvedValue([structuredClone(project)]); vi.mocked(workflowService.selectDesign).mockResolvedValue({}); });

test('refetches persisted designs and lists multiple versions after navigation or refresh', async () => {
  const { unmount } = render(<BrowserRouter><MyDesignsPage /></BrowserRouter>);
  expect(await screen.findByText('Version 2')).toBeTruthy(); expect(screen.getByText('Version 1')).toBeTruthy();
  unmount(); render(<BrowserRouter><MyDesignsPage /></BrowserRouter>);
  await screen.findByText('Version 2'); expect(workflowService.getMyDesigns).toHaveBeenCalledTimes(2);
});

test('selecting a design updates from persisted API state', async () => {
  vi.mocked(workflowService.getMyDesigns)
    .mockResolvedValueOnce([structuredClone(project)])
    .mockResolvedValueOnce([{ ...structuredClone(project), preferredHouseDesignId: 'd1', status: 'selected_by_client', designs: project.designs.map(d => ({ ...d, isPreferred: d.designId === 'd1' })) }]);
  render(<BrowserRouter><MyDesignsPage /></BrowserRouter>); await screen.findByText('Version 1');
  fireEvent.click(screen.getAllByRole('button', { name: 'Select Design' })[1]);
  await waitFor(() => expect(workflowService.selectDesign).toHaveBeenCalledWith('workflow-1234', 'd1'));
  expect(await screen.findByText('Selected')).toBeTruthy();
  expect(screen.getByRole('button', { name: 'Submit to Architect' })).toBeTruthy();
});

test('refresh retains the persisted selected design', async () => {
  const persisted = { ...structuredClone(project), preferredHouseDesignId: 'd2', status: 'selected_by_client', designs: project.designs.map(d => ({ ...d, isPreferred: d.designId === 'd2' })) };
  vi.mocked(workflowService.getMyDesigns).mockResolvedValue([persisted]);
  const view = render(<BrowserRouter><MyDesignsPage /></BrowserRouter>);
  expect(await screen.findByText('Selected')).toBeTruthy();
  view.unmount();
  render(<BrowserRouter><MyDesignsPage /></BrowserRouter>);
  expect(await screen.findByText('Selected')).toBeTruthy();
});
