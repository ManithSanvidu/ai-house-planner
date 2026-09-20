import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import PlanLibraryPage from './PlanLibraryPage';
import { preDesignedPlanService } from '../services/preDesignedPlanService';

vi.mock('../services/preDesignedPlanService', () => ({
  preDesignedPlanService: { list: vi.fn() },
}));

const plan = {
  id: 'plan-1', name: 'Curated COMMON-2B1B-1F-CENTRAL CORE', slug: 'plan-1',
  designCode: 'HP-CURATED-COMMON-2B1B-1F-CENTRAL_CORE', style: 'Modern Minimalist',
  bedrooms: 2, bathrooms: 1, floorCount: 1, totalBuiltUpAreaSqft: 784,
  minimumLandSizePerches: 8, suitableTerrain: 'flat', parkingSpaces: 0,
  hasBalcony: false, hasVeranda: false, hasOffice: false, isAccessibleFriendly: false,
  tags: [], isActive: true,
};

describe('PlanLibraryPage customer presentation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(preDesignedPlanService.list).mockResolvedValue([plan]);
  });

  it('shows customer-friendly names, readable facts, and no prominent internal code', async () => {
    render(<MemoryRouter><PlanLibraryPage /></MemoryRouter>);

    expect(await screen.findByRole('heading', { name: '2 Bedroom Central-Core Home' })).toBeTruthy();
    expect(screen.queryByText(plan.designCode)).toBeNull();
    expect(screen.getAllByText('2 Bedrooms').length).toBeGreaterThan(0);
    expect(screen.getByText('1 Bathroom')).toBeTruthy();
    expect(screen.getAllByText('1 Floor').length).toBeGreaterThan(0);
    expect(screen.getByText('784 sq ft')).toBeTruthy();
    expect(screen.getAllByText('Central Core').length).toBeGreaterThan(0);
  });

  it('keeps Plan Detail navigation working', async () => {
    render(<MemoryRouter initialEntries={['/dashboard/plans']}><Routes>
      <Route path="/dashboard/plans" element={<PlanLibraryPage />} />
      <Route path="/dashboard/plans/:id" element={<p>Plan detail route</p>} />
    </Routes></MemoryRouter>);

    fireEvent.click(await screen.findByRole('link', { name: 'View 2 Bedroom Central-Core Home' }));
    expect(await screen.findByText('Plan detail route')).toBeTruthy();
  });
});
