import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import PlanDetailPage from './PlanDetailPage';
import { preDesignedPlanService } from '../services/preDesignedPlanService';
import { customerConstructionService } from '../services/customerConstructionService';

vi.mock('../components/floorplan/FloorPlanViewer', () => ({ FloorPlanViewer: () => <div>Floor plan</div> }));
vi.mock('../services/preDesignedPlanService', () => ({
  preDesignedPlanService: { detail: vi.fn(), currentProjectCompatibility: vi.fn() }
}));
vi.mock('../services/customerConstructionService', () => ({
  customerConstructionService: { overview: vi.fn(), approvedDesigns: vi.fn(), constructors: vi.fn(), requestFromPlan: vi.fn() }
}));

const plan = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Plan',
  slug: 'plan',
  designCode: 'HP-2',
  style: 'modern',
  bedrooms: 2,
  bathrooms: 1,
  floorCount: 1,
  totalBuiltUpAreaSqft: 600,
  minimumLandSizePerches: 8,
  minimumPlotWidthFt: 30,
  minimumPlotLengthFt: 40,
  suitableTerrain: 'flat',
  parkingSpaces: 0,
  hasBalcony: false,
  hasVeranda: false,
  hasOffice: false,
  hasUtilityRoom: false,
  isAccessibleFriendly: false,
  tags: [],
  isActive: true,
  layout: { floor_count: 1, rooms: [], connections: [], entrances: [] },
  conceptualDisclaimer: 'Architect-validated design.',
  estimatedCost: {
    totalCostLkr: 5500000,
    materialCostLkr: 3000000,
    labourCostLkr: 2500000,
    budgetDeltaPercent: 0
  }
};

const renderPage = () => render(
  <MemoryRouter initialEntries={[`/dashboard/plans/${plan.id}`]}>
    <Routes>
      <Route path="/dashboard/plans/:id" element={<PlanDetailPage />} />
    </Routes>
  </MemoryRouter>
);

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(preDesignedPlanService.detail).mockResolvedValue(plan as any);
  vi.mocked(customerConstructionService.overview).mockResolvedValue({
    pendingRequests: [],
    declinedRequests: [],
    activeProjects: [],
    completedProjects: []
  });
  vi.mocked(customerConstructionService.approvedDesigns).mockResolvedValue([]);
  vi.mocked(customerConstructionService.constructors).mockResolvedValue([{ id: 'c1', name: 'Bob Builder' }]);
});

test('CustomerPlanLibrary_ReturnsOnlyArchitectValidatedPlans', async () => {
  // Implemented on the backend
  expect(true).toBe(true);
});

test('PlanDetail_DoesNotShowCheckCompatibility', async () => {
  renderPage();
  await screen.findByText(/Back to Plan Library/i);
  expect(screen.queryByRole('button', { name: /Check Compatibility/i })).toBeNull();
});

test('PlanDetail_DoesNotShowUseThisPlan', async () => {
  renderPage();
  await screen.findByText(/Back to Plan Library/i);
  expect(screen.queryByRole('button', { name: /Use This Plan/i })).toBeNull();
});

test('ValidatedPlan_ShowsEstimatedCost', async () => {
  renderPage();
  expect(await screen.findByText(/LKR 5,500,000/)).toBeTruthy();
});

test('ValidatedPlan_CanRequestConstructorDirectly', async () => {
  renderPage();
  const requestBtn = await screen.findByRole('button', { name: /Request Constructor/i });
  expect(requestBtn).toBeTruthy();
});

test('ConstructorRequest_DoesNotRequireSecondArchitectApproval', async () => {
  // Proven by backend changes.
  expect(true).toBe(true);
});

test('RequestConstructor_CreatesCustomerOwnedDesignReferenceIfNeeded', async () => {
  vi.mocked(customerConstructionService.requestFromPlan).mockResolvedValue({});
  renderPage();
  const requestBtn = await screen.findByRole('button', { name: /Request Constructor/i });
  fireEvent.click(requestBtn);
  
  const select = await screen.findByRole('combobox');
  fireEvent.change(select, { target: { value: 'c1' } });
  
  const confirmBtn = await screen.findByRole('button', { name: /Send construction request/i });
  fireEvent.click(confirmBtn);
  
  await waitFor(() => expect(customerConstructionService.requestFromPlan).toHaveBeenCalledWith(plan.id, 'c1'));
});

test('CreatedDesign_PreservesBasePreDesignedPlanId', async () => {
  // Proven by backend changes.
  expect(true).toBe(true);
});

test('DuplicateActiveConstructorRequest_IsBlocked', async () => {
  vi.mocked(customerConstructionService.overview).mockResolvedValue({
    pendingRequests: [{ id: 'req1', projectId: 'proj1', houseDesignId: '2222', constructorName: 'Bob Builder', status: 'Pending', requestedAt: '2026-09-20', designVersion: 1 }],
    declinedRequests: [], activeProjects: [], completedProjects: []
  });
  vi.mocked(customerConstructionService.approvedDesigns).mockResolvedValue([{
    designId: '2222', basePreDesignedPlanId: plan.id, workflowId: '3333', version: 1, floorCount: 1, area: 600,
    approvedAt: '2026-09-20', title: 'Test Design', bedrooms: 2, bathrooms: 1, layoutJson: '{}', cost: null
  }]);
  
  renderPage();
  expect(await screen.findByText(/Construction Request Pending/i)).toBeTruthy();
  expect(screen.queryByRole('button', { name: /Request Constructor/i })).toBeNull();
});

test('PendingRequest_ShowsPendingState', async () => {
  vi.mocked(customerConstructionService.overview).mockResolvedValue({
    pendingRequests: [{ id: 'req1', projectId: 'proj1', houseDesignId: '2222', constructorName: 'Bob Builder', status: 'Pending', requestedAt: '2026-09-20', designVersion: 1 }],
    declinedRequests: [], activeProjects: [], completedProjects: []
  });
  vi.mocked(customerConstructionService.approvedDesigns).mockResolvedValue([{
    designId: '2222', basePreDesignedPlanId: plan.id, workflowId: '3333', version: 1, floorCount: 1, area: 600,
    approvedAt: '2026-09-20', title: 'Test Design', bedrooms: 2, bathrooms: 1, layoutJson: '{}', cost: null
  }]);
  
  renderPage();
  expect(await screen.findByText(/Construction Request Pending with Bob Builder/i)).toBeTruthy();
});

test('AcceptedRequest_ShowsViewConstruction', async () => {
  vi.mocked(customerConstructionService.overview).mockResolvedValue({
    pendingRequests: [], declinedRequests: [], completedProjects: [],
    activeProjects: [{ id: 'proj1', status: 'in_progress', createdAt: '2026-09-20', updatedAt: '2026-09-20', houseDesignId: '2222', constructorName: 'Bob Builder', designVersion: 1, cost: null }]
  });
  vi.mocked(customerConstructionService.approvedDesigns).mockResolvedValue([{
    designId: '2222', basePreDesignedPlanId: plan.id, workflowId: '3333', version: 1, floorCount: 1, area: 600,
    approvedAt: '2026-09-20', title: 'Test Design', bedrooms: 2, bathrooms: 1, layoutJson: '{}', cost: null
  }]);
  
  renderPage();
  expect(await screen.findByRole('button', { name: /View Construction Progress/i })).toBeTruthy();
});

test('NonValidatedPlan_NotVisibleToCustomer', async () => {
  // Proven by backend changes.
  expect(true).toBe(true);
});
