import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { CostSummaryDto } from '../../services/workflowService';
import { CostBreakdownCard } from './CostBreakdownCard';

const cost = (budgetDeltaPercent: number | null): CostSummaryDto => ({
  materialCostLkr: 8_400_000,
  labourCostLkr: 2_940_000,
  totalCostLkr: 11_340_000,
  budgetDeltaPercent,
  breakdown: [
    {
      itemName: 'Foundation Materials',
      costHead: 'Foundation',
      category: 'material',
      unitCostLkr: 3_000,
      unit: 'per_sqft',
      appliedQuantity: 1_000,
      quantityUnit: 'sq ft',
      terrainMultiplier: 1,
      amountLkr: 3_000_000,
      sharePercent: 26.46,
    },
    {
      itemName: 'Construction Labour',
      costHead: 'Labour',
      category: 'labour',
      unitCostLkr: 0.35,
      unit: 'factor',
      appliedQuantity: 8_400_000,
      quantityUnit: 'material cost',
      terrainMultiplier: 1,
      amountLkr: 2_940_000,
      sharePercent: 25.93,
    },
  ],
});

describe('CostBreakdownCard', () => {
  it('displays material, labour, total, and budget percentage values', () => {
    render(<CostBreakdownCard cost={cost(94.5)} />);

    expect(screen.getByText('Material Cost')).toBeTruthy();
    expect(screen.getByText('LKR 8,400,000')).toBeTruthy();
    expect(screen.getByText('Labour Cost')).toBeTruthy();
    expect(screen.getAllByText('LKR 2,940,000').length).toBeGreaterThan(0);
    expect(screen.getByText('Total Estimated Cost')).toBeTruthy();
    expect(screen.getAllByText('LKR 11,340,000').length).toBeGreaterThan(0);
    expect(screen.getByText('94.50%')).toBeTruthy();
    expect(screen.getByText('Within budget')).toBeTruthy();
  });

  it('shows the over-budget state while capping visual progress at 100%', () => {
    render(<CostBreakdownCard cost={cost(127.25)} />);

    expect(screen.getByText('127.25%')).toBeTruthy();
    expect(screen.getByText('Over budget')).toBeTruthy();
    const progressbar = screen.getByRole('progressbar', { name: 'Budget used' });
    expect(progressbar.getAttribute('aria-valuenow')).toBe('100');
    expect(progressbar.firstElementChild?.getAttribute('style')).toContain('width: 100%');
  });

  it('shows the exactly-at-budget state', () => {
    render(<CostBreakdownCard cost={cost(100)} />);

    expect(screen.getByText('100.00%')).toBeTruthy();
    expect(screen.getByText('At budget')).toBeTruthy();
  });

  it('shows the cost breakdown without budget comparison when no budget was supplied', () => {
    render(<CostBreakdownCard cost={cost(null)} />);

    expect(screen.getByText('LKR 8,400,000')).toBeTruthy();
    expect(screen.getAllByText('LKR 2,940,000').length).toBeGreaterThan(0);
    expect(screen.getAllByText('LKR 11,340,000').length).toBeGreaterThan(0);
    expect(screen.queryByText('Budget Used %')).toBeNull();
    expect(screen.queryByRole('progressbar')).toBeNull();
    expect(screen.getByText('Cost-head breakdown')).toBeTruthy();
    expect(screen.getByText('Foundation')).toBeTruthy();
    expect(screen.getByText('LKR 3,000,000')).toBeTruthy();
    expect(screen.getByText('LKR 3,000/sq ft × 1,000 sq ft')).toBeTruthy();
    expect(screen.getByText('0.35 × material cost')).toBeTruthy();
  });

  it('shows an empty state when cost is null', () => {
    render(<CostBreakdownCard cost={null} />);

    expect(screen.getByText('Cost estimate is not available yet.')).toBeTruthy();
    expect(screen.queryByText('LKR 0')).toBeNull();
  });

  it('does not support the legacy estimated_total_lkr structure', () => {
    const legacyCost = { estimated_total_lkr: 9_999_999 } as unknown as CostSummaryDto;

    render(<CostBreakdownCard cost={legacyCost} />);

    expect(screen.getByText('Cost estimate is not available yet.')).toBeTruthy();
    expect(screen.queryByText('LKR 9,999,999')).toBeNull();
  });
});
