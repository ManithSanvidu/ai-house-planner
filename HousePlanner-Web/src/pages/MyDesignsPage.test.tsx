import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import MyDesignsPage from './MyDesignsPage';
import { workflowService } from '../services/workflowService';

vi.mock('../services/workflowService', () => ({ workflowService: {
  getMyDesigns: vi.fn(), removeDesign: vi.fn(), submitArchitectReview: vi.fn(),
} }));

const room = { roomType: 'living_room', floor: 1, x: 0, y: 0, width: 12, length: 10 };
const project = { workflowId: 'workflow-1234', status: 'design_generated', preferredHouseDesignId: null, createdAt: '2026-01-01', designs: [
  { designId: 'd2', version: 2, isCurrent: true, isPreferred: false, isArchived: false, isArchitectApproved: false, topology: 'L_SHAPE', bedrooms: 3, bathrooms: 2, floorCount: 1, totalBuiltUpAreaSqft: 1200, foundationType: 'slab', generationMode: 'ai_adapted_template', selectedBasePlan: 'P2', geometryFingerprint: 'b', suitabilityScore: 92, architecturalQualityScore: 88, previewRooms: [room], createdAt: '2026-01-02' },
  { designId: 'd1', version: 1, isCurrent: false, isPreferred: false, isArchived: false, isArchitectApproved: false, topology: 'COMPACT_RECTANGLE', bedrooms: 3, bathrooms: 2, floorCount: 1, totalBuiltUpAreaSqft: 1100, foundationType: 'slab', generationMode: 'ai_adapted_template', selectedBasePlan: 'P1', geometryFingerprint: 'a', suitabilityScore: 89, architecturalQualityScore: 90, previewRooms: [room], createdAt: '2026-01-01' },
] };

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(workflowService.getMyDesigns).mockResolvedValue([structuredClone(project)]);
  vi.mocked(workflowService.removeDesign).mockResolvedValue({});
  vi.mocked(workflowService.submitArchitectReview).mockResolvedValue({});
});

const renderPage = () => render(
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    <BrowserRouter><MyDesignsPage /></BrowserRouter>
  </QueryClientProvider>,
);

test('lists persisted design versions with their available actions', async () => {
  renderPage();
  expect((await screen.findAllByText(/Version 2/)).length).toBeGreaterThan(0);
  expect(screen.getAllByText(/Version 1/).length).toBeGreaterThan(0);
  expect(screen.getByText('2 Designs')).toBeTruthy();
  expect(screen.getAllByRole('button', { name: 'Send to Architect' })).toHaveLength(2);
  expect(screen.queryByRole('button', { name: 'Add to Compare' })).toBeNull();
});

test('sends the selected design version to the architect', async () => {
  renderPage();
  await screen.findByText('2 Designs');
  fireEvent.click(screen.getAllByRole('button', { name: 'Send to Architect' })[0]);
  await waitFor(() => expect(workflowService.submitArchitectReview).toHaveBeenCalledWith('workflow-1234', 'd2'));
});

test('confirms removal and refetches the saved designs', async () => {
  vi.mocked(workflowService.getMyDesigns)
    .mockResolvedValueOnce([structuredClone(project)])
    .mockResolvedValueOnce([{ ...structuredClone(project), designs: [project.designs[1]] }]);
  renderPage();
  await screen.findByText('2 Designs');
  fireEvent.click(screen.getByRole('button', { name: 'Delete Version 2' }));
  const dialog = screen.getByRole('dialog', { name: 'Delete Version 2' });
  expect(within(dialog).getByText('Delete Version 2?')).toBeTruthy();
  fireEvent.click(within(dialog).getByRole('button', { name: 'Remove Design' }));
  await waitFor(() => expect(workflowService.removeDesign).toHaveBeenCalledWith('workflow-1234', 'd2'));
  expect(await screen.findByText('1 Design')).toBeTruthy();
});

test('approved design exposes constructor handoff and locks management actions', async () => {
  const approved = structuredClone(project);
  approved.status = 'approved';
  approved.designs[0].isArchitectApproved = true;
  vi.mocked(workflowService.getMyDesigns).mockResolvedValue([approved]);
  renderPage();
  expect((await screen.findAllByText('✓ Architect Approved')).length).toBeGreaterThan(0);
  expect(screen.getByRole('link', { name: 'Find Constructor' })).toBeTruthy();
  expect(screen.queryByRole('button', { name: /Delete Version/ })).toBeNull();
  expect(screen.queryByRole('button', { name: 'Send to Architect' })).toBeNull();
});
