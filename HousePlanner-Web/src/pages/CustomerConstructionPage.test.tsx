import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import CustomerConstructionPage from './CustomerConstructionPage';
import CustomerConstructionProgressPage from './CustomerConstructionProgressPage';
import { customerConstructionService } from '../services/customerConstructionService';

vi.mock('../services/customerConstructionService', () => ({
 customerConstructionService: {
  overview: vi.fn(), approvedDesigns: vi.fn(), constructors: vi.fn(), request: vi.fn(),
  cancelProject: vi.fn(), project: vi.fn()
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

 it('keeps setup collapsed, requires manual design selection, tests Change button, and sends the existing request', async () => {
  service.request.mockResolvedValue({});
  render(<BrowserRouter><CustomerConstructionPage/></BrowserRouter>);
  expect(await screen.findByRole('heading', { name: 'Start New Construction' })).toBeTruthy();
  expect(screen.queryByText('Step 1 — Select design')).toBeNull();

  fireEvent.click(screen.getByRole('button', { name: 'Start' }));
  expect(screen.getByText('Step 1 — Select design')).toBeTruthy();
  
  // Design is not auto-selected, so Step 2 should not be there yet
  expect(screen.queryByText('LKR 11,340,000.00')).toBeNull();

  // Click the design choice to select it
  fireEvent.click(screen.getByText((content) => content.includes('Central Core Home')));

  // Now Steps 2 and 3 should appear
  expect(screen.getByText('3 Bedrooms · 2 Bathrooms · 1 Floors')).toBeTruthy();
  expect(screen.getByText('LKR 11,340,000.00')).toBeTruthy();
  expect(screen.getByText('Real Builder')).toBeTruthy();

  // Click Change button
  fireEvent.click(screen.getByRole('button', { name: 'Change' }));
  
  // Steps 2 and 3 disappear
  expect(screen.queryByText('LKR 11,340,000.00')).toBeNull();

  // Select design again
  fireEvent.click(screen.getByText((content) => content.includes('Central Core Home')));
  expect(screen.getByText('LKR 11,340,000.00')).toBeTruthy();

  fireEvent.click(screen.getByRole('button', { name: 'Send request' }));
  await waitFor(() => expect(service.request).toHaveBeenCalledWith('d1','c1'));
 });

 it('renders compact active cards and pending request rows', async () => {
  service.overview.mockResolvedValue({
   declinedRequests: [], completedProjects: [],
   activeProjects: [{ id:'p1',status:'in_progress',createdAt:'2026-09-20',updatedAt:'2026-09-21',houseDesignId:'d1',constructorName:'Real Builder',designVersion:1,currentPhase:'Foundation',cost:null }],
   pendingRequests: [{ id:'r1',projectId:'p2',houseDesignId:'d1',constructorName:'Second Builder',status:'Pending',requestedAt:'2026-09-22',designVersion:1 }],
  });

  render(<BrowserRouter><CustomerConstructionPage/></BrowserRouter>);
  expect(await screen.findByRole('heading', { name: /Active Construction/i })).toBeTruthy();
  expect(screen.getByText((content) => content.includes('Real Builder'))).toBeTruthy();
  expect(screen.getByText((content) => content.includes('Current phase:')).parentElement?.textContent).toContain('Foundation');
  expect(screen.getByRole('link', { name: 'View Progress' })).toBeTruthy();
  expect(screen.getByText((content) => content.includes('Waiting for Second Builder'))).toBeTruthy();
 });

 it('renders friendly empty states, including no-approved-design guidance', async () => {
  service.approvedDesigns.mockResolvedValue([]);
  render(<BrowserRouter><CustomerConstructionPage/></BrowserRouter>);

  expect(await screen.findByText('Your active construction projects will appear here.')).toBeTruthy();
  expect(screen.getByText('No construction requests are waiting for a response.')).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'Start' }));
  expect(screen.getByText('No approved designs are available yet.')).toBeTruthy();
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
