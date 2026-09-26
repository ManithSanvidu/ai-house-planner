import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import CustomerConstructionPage from './CustomerConstructionPage';
import CustomerConstructionProgressPage from './CustomerConstructionProgressPage';
import { customerConstructionService } from '../services/customerConstructionService';

vi.mock('../services/customerConstructionService', () => ({
 customerConstructionService: {
  overview: vi.fn(), approvedDesigns: vi.fn(), constructors: vi.fn(), request: vi.fn(), project: vi.fn()
 }
}));

const service = vi.mocked(customerConstructionService);

describe('customer construction lifecycle', () => {
 beforeEach(() => {
  vi.clearAllMocks();
  service.overview.mockResolvedValue({ pendingRequests: [], declinedRequests: [], activeProjects: [], completedProjects: [] });
  service.approvedDesigns.mockResolvedValue([{ designId:'d1', workflowId:'w1', version:1, floorCount:1, area:900, approvedAt:'2026-09-20', title:'Central Core Home', bedrooms:3, bathrooms:2, layoutJson:'{"rooms":[]}', cost:{materialCostLkr:8_400_000,labourCostLkr:2_940_000,totalCostLkr:11_340_000,budgetDeltaPercent:94.5} }]);
  service.constructors.mockResolvedValue([{ id:'c1', name:'Real Builder' }]);
 });

 it('lists approved designs and real constructors and sends a request', async () => {
  service.request.mockResolvedValue({});
  render(<BrowserRouter><CustomerConstructionPage/></BrowserRouter>);
  expect((await screen.findAllByText(/Central Core Home/)).length).toBeGreaterThan(0);
  expect(screen.getByText('LKR 11,340,000')).toBeTruthy();
  expect(screen.getByText('Real Builder')).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'Approve & Request Construction' }));
  await waitFor(() => expect(service.request).toHaveBeenCalledWith('d1','c1'));
 });

 it('renders customer progress and activity from stored logs', async () => {
  service.project.mockResolvedValue({
   project:{id:'project-1',createdAt:'2026-09-20',updatedAt:'2026-09-20',houseDesignId:'d1',designVersion:1,constructorName:'Real Builder',status:'active',currentPhase:'Foundation',cost:{materialCostLkr:8_400_000,labourCostLkr:2_940_000,totalCostLkr:11_340_000,budgetDeltaPercent:94.5}},
   progress:{overallProgress:24}, phases:[{id:'p1',phaseName:'Foundation',status:'in_progress'}],
   logs:[{id:'l1',date:'2026-09-20T10:00:00Z',phase:'Foundation',completedWork:'Reinforcement complete',progressPercentage:24}],
   activity:[{date:'2026-09-20',count:2,intensity:2}]
  });
  window.history.pushState({},'', '/dashboard/construction/project-1');
  render(<BrowserRouter><Routes><Route path="/dashboard/construction/:projectId" element={<CustomerConstructionProgressPage/>}/></Routes></BrowserRouter>);
  expect(await screen.findByText('Construction Progress')).toBeTruthy();
  expect(screen.getByText('Reinforcement complete')).toBeTruthy();
  expect(screen.getByText('LKR 11,340,000')).toBeTruthy();
  expect(screen.getByText(/Color intensity/)).toBeTruthy();
 });
});
