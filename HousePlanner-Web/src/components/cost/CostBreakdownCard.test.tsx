import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { CostSummaryDto } from '../../services/workflowService';
import { CostBreakdownCard } from './CostBreakdownCard';

const cost = (budgetDeltaPercent: number): CostSummaryDto => ({
  materialCostLkr: 8_400_000,
  labourCostLkr: 2_940_000,
  totalCostLkr: 11_340_000,
  budgetDeltaPercent,
});

describe('CostBreakdownCard', () => {
  it('displays material, labour, total, and budget percentage values', () => {
    render(<CostBreakdownCard cost={cost(94.5)} />);

    expect(screen.getByText('Material Cost')).toBeTruthy();
    expect(screen.getByText('LKR 8,400,000')).toBeTruthy();
    expect(screen.getByText('Labour Cost')).toBeTruthy();
    expect(screen.getByText('LKR 2,940,000')).toBeTruthy();
    expect(screen.getByText('Total Estimated Cost')).toBeTruthy();
    expect(screen.getByText('LKR 11,340,000')).toBeTruthy();
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
