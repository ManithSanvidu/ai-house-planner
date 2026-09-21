import { describe, expect, it } from 'vitest';
import { formatTopology, formatWorkflowStatus, getCustomerPlanName } from './presentation';

describe('customer presentation helpers', () => {
  it('formats topology and workflow values without changing source data', () => {
    expect(formatTopology('CENTRAL_CORE')).toBe('Central Core');
    expect(formatTopology('L_SHAPE')).toBe('L-Shaped');
    expect(formatWorkflowStatus('DESIGN_GENERATED')).toBe('Design Ready');
  });

  it('derives a friendly catalogue name from the stable design code', () => {
    expect(getCustomerPlanName({
      designCode: 'HP-CURATED-COMMON-2B1B-1F-COMPACT_RECTANGLE',
      name: 'Curated COMMON-2B1B-1F-COMPACT RECTANGLE', bedrooms: 2,
    })).toBe('Compact 2 Bedroom Home');
  });
});
