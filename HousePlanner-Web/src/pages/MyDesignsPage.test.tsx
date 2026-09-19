import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import MyDesignsPage from './MyDesignsPage';
import { workflowService } from '../services/workflowService';

vi.mock('../services/workflowService', () => ({ workflowService: {
  getMyDesigns: vi.fn(), selectDesign: vi.fn(), clearDesignSelection: vi.fn(), removeDesign: vi.fn(), submitArchitectReview: vi.fn()
} }));
const room = { roomType: 'living_room', floor: 1, x: 0, y: 0, width: 12, length: 10 };
const project = { workflowId: 'workflow-1234', status: 'design_generated', preferredHouseDesignId: null, createdAt: '2026-01-01', designs: [
  { designId: 'd2', version: 2, isCurrent: true, isPreferred: false, isArchived: false, topology: 'L_SHAPE', bedrooms: 3, bathrooms: 2, floorCount: 1, totalBuiltUpAreaSqft: 1200, foundationType: 'slab', generationMode: 'ai_adapted_template', selectedBasePlan: 'P2', geometryFingerprint: 'b', suitabilityScore: 92, architecturalQualityScore: 88, previewRooms: [room], createdAt: '2026-01-02' },
  { designId: 'd1', version: 1, isCurrent: false, isPreferred: false, isArchived: false, topology: 'COMPACT_RECTANGLE', bedrooms: 3, bathrooms: 2, floorCount: 1, totalBuiltUpAreaSqft: 1100, foundationType: 'slab', generationMode: 'ai_adapted_template', selectedBasePlan: 'P1', geometryFingerprint: 'a', suitabilityScore: 89, architecturalQualityScore: 90, previewRooms: [room], createdAt: '2026-01-01' }
] };
const selectedProject = () => ({ ...structuredClone(project), status: 'selected_by_client', preferredHouseDesignId: 'd2', designs: project.designs.map(d => ({ ...d, isPreferred: d.designId === 'd2' })) });

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(workflowService.getMyDesigns).mockResolvedValue([structuredClone(project)]);
  vi.mocked(workflowService.selectDesign).mockResolvedValue({});
  vi.mocked(workflowService.clearDesignSelection).mockResolvedValue({});
  vi.mocked(workflowService.removeDesign).mockResolvedValue({});
});
const renderPage = () => render(<BrowserRouter><MyDesignsPage /></BrowserRouter>);

test('project grid renders multiple persisted versions and submit is disabled without selection', async () => {
  renderPage();
  expect(await screen.findByText('Version 2')).toBeTruthy();
  expect(screen.getByText('Version 1')).toBeTruthy();
  expect(screen.getByText('2 saved designs')).toBeTruthy();
  expect((screen.getByRole('button', { name: 'Submit Selected to Architect' }) as HTMLButtonElement).disabled).toBe(true);
});

test('selected card shows Unselect and no disabled Select control', async () => {
  vi.mocked(workflowService.getMyDesigns).mockResolvedValue([selectedProject()]);
  renderPage();
  expect(await screen.findByText('Selected')).toBeTruthy();
  expect(screen.getByRole('button', { name: 'Unselect' })).toBeTruthy();
  expect(screen.queryByRole('button', { name: 'Select', hidden: true })?.hasAttribute('disabled')).not.toBe(true);
});

test('unselect clears persisted selected state after refetch', async () => {
  vi.mocked(workflowService.getMyDesigns).mockResolvedValueOnce([selectedProject()]).mockResolvedValueOnce([structuredClone(project)]);
  renderPage(); await screen.findByText('Selected');
  fireEvent.click(screen.getByRole('button', { name: 'Unselect' }));
  await waitFor(() => expect(workflowService.clearDesignSelection).toHaveBeenCalledWith('workflow-1234'));
  await screen.findByText('No design selected');
  expect(screen.queryByText('Selected')).toBeNull();
});

test('compare requires exactly two designs and renders both in compare view', async () => {
  renderPage(); await screen.findByText('Version 2');
  const addButtons = screen.getAllByRole('button', { name: 'Add to Compare' });
  fireEvent.click(addButtons[0]);
  expect((screen.getByRole('button', { name: 'Compare Now' }) as HTMLButtonElement).disabled).toBe(true);
  fireEvent.click(screen.getAllByRole('button', { name: 'Add to Compare' })[0]);
  const compareNow = screen.getByRole('button', { name: 'Compare Now' }) as HTMLButtonElement;
  expect(compareNow.disabled).toBe(false); fireEvent.click(compareNow);
  const dialog = screen.getByRole('dialog', { name: 'Compare designs' });
  expect(within(dialog).getByText('Version 2')).toBeTruthy();
  expect(within(dialog).getByText('Version 1')).toBeTruthy();
  expect(within(dialog).getAllByText('Suitability score')).toHaveLength(2);
});

test('delete action shows confirmation and refetches after confirmation', async () => {
  vi.mocked(workflowService.getMyDesigns).mockResolvedValueOnce([structuredClone(project)]).mockResolvedValueOnce([{ ...structuredClone(project), designs: [project.designs[1]] }]);
  renderPage(); await screen.findByText('Version 2');
  fireEvent.click(screen.getByRole('button', { name: 'Delete Version 2' }));
  const dialog = screen.getByRole('dialog', { name: 'Delete Version 2' });
  expect(within(dialog).getByText('Delete Version 2?')).toBeTruthy();
  fireEvent.click(within(dialog).getByRole('button', { name: 'Delete' }));
  await waitFor(() => expect(workflowService.removeDesign).toHaveBeenCalledWith('workflow-1234', 'd2'));
  await waitFor(() => expect(screen.queryByText('Version 2')).toBeNull());
});

test('refresh retains selection from persisted API state', async () => {
  vi.mocked(workflowService.getMyDesigns).mockResolvedValue([selectedProject()]);
  const view = renderPage(); expect(await screen.findByText('Selected')).toBeTruthy();
  view.unmount(); renderPage(); expect(await screen.findByText('Selected')).toBeTruthy();
  expect(workflowService.getMyDesigns).toHaveBeenCalledTimes(2);
});
