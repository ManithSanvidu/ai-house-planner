import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, expect, test, vi } from 'vitest';
import PricingManagementPage from './PricingManagementPage';
import type { PricingItem } from '../types/pricing.types';

const { mockGetAll, mockCreate, mockUpdate, mockDeactivate, mockGetHistory } = vi.hoisted(() => ({
  mockGetAll: vi.fn(),
  mockCreate: vi.fn(),
  mockUpdate: vi.fn(),
  mockDeactivate: vi.fn(),
  mockGetHistory: vi.fn(),
}));

vi.mock('../services/pricingService', () => ({
  default: {
    getAll: mockGetAll,
    create: mockCreate,
    update: mockUpdate,
    deactivate: mockDeactivate,
    getHistory: mockGetHistory,
  },
  pricingService: {
    getAll: mockGetAll,
    create: mockCreate,
    update: mockUpdate,
    deactivate: mockDeactivate,
    getHistory: mockGetHistory,
  },
}));

const mockItems: PricingItem[] = [
  {
    id: 1,
    itemName: 'Foundation & Substructure Materials',
    category: 'material',
    displayGroup: 'Foundation',
    unitCostLkr: 8500,
    unit: 'per_sqft',
    terrainMultiplier: {
      flat: 1.0,
      hillside: 1.2,
      coastal: 1.15,
    },
    provider: 'Manual',
    sourceReference: 'Local Baseline',
    region: 'Sri Lanka',
    qualityLevel: 'Standard',
    isActive: true,
    createdAt: '2026-09-01T10:00:00Z',
    updatedByUserId: 'constructor-1',
    updatedAt: '2026-09-20T10:00:00Z',
  },
  {
    id: 2,
    itemName: 'General Construction Labour',
    category: 'labour',
    displayGroup: 'Labour',
    unitCostLkr: 0.35,
    unit: 'factor',
    terrainMultiplier: {
      flat: 1.0,
      hillside: 1.15,
      coastal: 1.1,
    },
    provider: 'Manual',
    sourceReference: 'Standard Ratio',
    region: 'Sri Lanka',
    qualityLevel: 'Standard',
    isActive: true,
    createdAt: '2026-09-01T10:00:00Z',
    updatedByUserId: 'constructor-1',
    updatedAt: '2026-09-20T10:00:00Z',
  },
];

beforeEach(() => {
  vi.clearAllMocks();
  mockGetAll.mockResolvedValue(mockItems);
  mockCreate.mockResolvedValue(mockItems[0]);
  mockUpdate.mockResolvedValue(mockItems[0]);
  mockDeactivate.mockResolvedValue({ ...mockItems[0], isActive: false });
  mockGetHistory.mockResolvedValue([]);
});

test('1. renders empty state when no pricing items exist', async () => {
  mockGetAll.mockResolvedValue([]);
  render(<PricingManagementPage />);

  expect(await screen.findByText('No pricing items are configured yet.')).toBeTruthy();
  expect(screen.getAllByRole('button', { name: /add pricing item/i }).length).toBeGreaterThanOrEqual(1);
});

test('2. "Add Pricing Item" button opens create modal', async () => {
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  const addBtn = screen.getByRole('button', { name: /add pricing item/i });
  fireEvent.click(addBtn);

  expect(screen.getByRole('heading', { name: 'Add Pricing Item' })).toBeTruthy();
  expect(screen.getByLabelText(/item name/i)).toBeTruthy();
});

test('3. Material category selection displays per-sqft semantics', async () => {
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  fireEvent.click(screen.getByRole('button', { name: /add pricing item/i }));

  const materialBtn = screen.getByRole('button', { name: /material \(per_sqft\)/i });
  fireEvent.click(materialBtn);

  expect(screen.getByText('Unit: LKR per sqft')).toBeTruthy();
  expect(screen.getAllByText(/per_sqft/i).length).toBeGreaterThan(0);
  expect(screen.getByText(/cost in lkr per square foot/i)).toBeTruthy();
});

test('4. Labour category selection displays factor semantics', async () => {
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  fireEvent.click(screen.getByRole('button', { name: /add pricing item/i }));

  const labourBtn = screen.getByRole('button', { name: /labour \(factor\)/i });
  fireEvent.click(labourBtn);

  expect(screen.getByText('Unit: Labour factor')).toBeTruthy();
  expect(screen.getAllByText(/factor/i).length).toBeGreaterThan(0);
  expect(screen.getByText(/example: 0\.35 means labour is calculated as 35% of material cost/i)).toBeTruthy();
});

test('5. Material rate formats as LKR / sqft in the table', async () => {
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  expect(screen.getByText('LKR 8,500 / sqft')).toBeTruthy();
});

test('6. Labour factor is NOT formatted as LKR', async () => {
  render(<PricingManagementPage />);
  await screen.findByText('General Construction Labour');

  // Should display 0.35 × material cost (35%) instead of LKR 0.35
  expect(screen.getByText('0.35 × material cost (35%)')).toBeTruthy();
  expect(screen.queryByText(/LKR 0\.35/)).toBeNull();
});

test('7. DisplayGroup is shown separately from Category', async () => {
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  // DisplayGroup badge
  expect(screen.getByText('Foundation')).toBeTruthy();
  // Machine Category badges
  const materialBadges = screen.getAllByText('material');
  expect(materialBadges.length).toBeGreaterThan(0);
  const labourBadges = screen.getAllByText('labour');
  expect(labourBadges.length).toBeGreaterThan(0);
});

test('8. Invalid numeric values are rejected in create modal', async () => {
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  fireEvent.click(screen.getByRole('button', { name: /add pricing item/i }));

  const nameInput = screen.getByLabelText(/item name/i);
  fireEvent.change(nameInput, { target: { value: 'Test Material' } });

  const rateInput = screen.getByLabelText(/unit cost/i);
  fireEvent.change(rateInput, { target: { value: '-10' } });

  fireEvent.click(screen.getByRole('button', { name: /create item/i }));

  expect(await screen.findByText('Must be a positive number.')).toBeTruthy();
  expect(mockCreate).not.toHaveBeenCalled();
});

test('9. Successful create request adds/refreshes item', async () => {
  const newItem: PricingItem = {
    id: 3,
    itemName: 'Roofing Sheet Materials',
    category: 'material',
    displayGroup: 'Roofing',
    unitCostLkr: 6200,
    unit: 'per_sqft',
    terrainMultiplier: {
      flat: 1.0,
      hillside: 1.1,
      coastal: 1.25,
    },
    provider: 'Manual',
    sourceReference: 'Supplier Quote A',
    region: 'Sri Lanka',
    qualityLevel: 'Standard',
    isActive: true,
    createdAt: '2026-09-22T10:00:00Z',
    updatedAt: '2026-09-22T10:00:00Z',
  };
  mockCreate.mockResolvedValue(newItem);

  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  fireEvent.click(screen.getByRole('button', { name: /add pricing item/i }));

  fireEvent.change(screen.getByLabelText(/item name/i), { target: { value: 'Roofing Sheet Materials' } });
  fireEvent.change(screen.getByLabelText(/display group/i), { target: { value: 'Roofing' } });
  fireEvent.change(screen.getByLabelText(/unit cost/i), { target: { value: '6200' } });
  fireEvent.change(screen.getByLabelText(/^hillside/i), { target: { value: '1.1' } });
  fireEvent.change(screen.getByLabelText(/^coastal/i), { target: { value: '1.25' } });
  fireEvent.change(screen.getByLabelText(/source reference/i), { target: { value: 'Supplier Quote A' } });

  fireEvent.click(screen.getByRole('button', { name: /create item/i }));

  await waitFor(() => {
    expect(mockCreate).toHaveBeenCalledWith({
      itemName: 'Roofing Sheet Materials',
      category: 'material',
      displayGroup: 'Roofing',
      unitCostLkr: 6200,
      terrainMultiplier: {
        flat: 1.0,
        hillside: 1.1,
        coastal: 1.25,
      },
      sourceReference: 'Supplier Quote A',
      region: 'Sri Lanka',
      qualityLevel: 'Standard',
    });
  });

  expect(await screen.findByText('Roofing Sheet Materials')).toBeTruthy();
  expect(screen.getByText(/added new pricing item/i)).toBeTruthy();
});

test('10. Existing edit functionality still works', async () => {
  const updatedItem: PricingItem = {
    ...mockItems[0],
    unitCostLkr: 9200,
  };
  mockUpdate.mockResolvedValue(updatedItem);

  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  const editBtns = screen.getAllByRole('button', { name: /edit/i });
  fireEvent.click(editBtns[0]);

  expect(screen.getByText('Edit Pricing')).toBeTruthy();
  const editRateInput = screen.getByDisplayValue('8500');
  fireEvent.change(editRateInput, { target: { value: '9200' } });

  fireEvent.click(screen.getByRole('button', { name: /save changes/i }));

  await waitFor(() => {
    expect(mockUpdate).toHaveBeenCalledWith(1, {
      unitCostLkr: 9200,
      terrainMultiplier: {
        flat: 1.0,
        hillside: 1.2,
        coastal: 1.15,
      },
      reason: undefined,
    });
  });

  expect(await screen.findByText('LKR 9,200 / sqft')).toBeTruthy();
  expect(screen.getByText(/updated rate for/i)).toBeTruthy();
});

test('11. constructor can deactivate an active price after confirmation', async () => {
  vi.spyOn(window, 'confirm').mockReturnValue(true);
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  fireEvent.click(screen.getByRole('button', { name: 'Deactivate Foundation & Substructure Materials' }));

  await waitFor(() => expect(mockDeactivate).toHaveBeenCalledWith(1, expect.any(String)));
  expect(await screen.findByText(/deactivated pricing item/i)).toBeTruthy();
});

test('12. constructor can display pricing history', async () => {
  mockGetHistory.mockResolvedValue([{
    id: 'history-1', pricingDataId: 1, previousValue: 8000, newValue: 8500,
    changedByUserId: 'constructor-1', changedAt: '2026-09-20T10:00:00Z', reason: 'Supplier update',
  }]);
  render(<PricingManagementPage />);
  await screen.findByText('Foundation & Substructure Materials');

  fireEvent.click(screen.getByRole('button', { name: 'History for Foundation & Substructure Materials' }));

  expect(await screen.findByText('8,000 → 8,500')).toBeTruthy();
  expect(screen.getByText('Supplier update')).toBeTruthy();
});
