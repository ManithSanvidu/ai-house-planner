import { describe, expect, it } from 'vitest';
import { cleanup, render } from '@testing-library/react';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { FloorPlanViewer, type FloorPlanData } from './FloorPlanViewer';

const plans = JSON.parse(readFileSync(
  resolve(process.cwd(), '../HousePlanner.API/Data/Seed/pre-designed-plans.json'), 'utf8',
)).filter((plan: { designCode: string }) => plan.designCode.startsWith('HP-CURATED-COMMON-'));

describe('common curated floor-plan rendering', () => {
  for (const plan of plans) {
    it(`renders every room and aligned floor viewport for ${plan.designCode}`, () => {
      let sharedViewBox: string | null = null;
      for (let floor = 1; floor <= plan.floors; floor++) {
        const expectedRooms = plan.layout.rooms.filter((room: { floor: number }) => room.floor === floor);
        const { container, unmount } = render(
          <div style={{ width: 1000, height: 760 }}>
            <FloorPlanViewer data={plan.layout as FloorPlanData} floorFilter={floor} />
          </div>,
        );
        const svg = container.querySelector('svg');
        expect(svg).not.toBeNull();
        expect(container.querySelectorAll('rect[stroke="#334155"]')).toHaveLength(expectedRooms.length);
        if (sharedViewBox === null) sharedViewBox = svg?.getAttribute('viewBox') ?? null;
        expect(svg?.getAttribute('viewBox')).toBe(sharedViewBox);
        if (plan.floors > 1) {
          expect(container.querySelector('[aria-label="Staircase treads"]')).not.toBeNull();
        }
        unmount();
        cleanup();
      }
    });
  }
});
