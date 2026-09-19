
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import IntakeForm from './IntakeForm';
import { expect, test, vi, beforeEach } from 'vitest';

const mockOptions = {
  landRanges: [
    { id: 'LAND_5_8', label: '5-8 perches', minPerches: 5, maxPerches: 8, approxSqft: '1361-2178 sqft' },
    { id: 'LAND_8_12', label: '8-12 perches', minPerches: 8, maxPerches: 12, approxSqft: '2178-3267 sqft' }
  ],
  plotShapes: ['NARROW_DEEP', 'BALANCED', 'WIDE_SHALLOW', 'CUSTOM_DIMENSIONS'],
  floors: [1, 2],
  bedrooms: [2, 3, 4],
  bathrooms: [1, 2, 3],
  architecturalStyles: ['Modern Minimalist', 'Contemporary'],
  validatedDesignCount: 4,
  features: {
    open_plan: { available: true },
    master_ensuite: { available: true },
    separate_dining: { available: true },
    home_office: { available: false, reason: 'No validated design is available for this configuration.' },
    balcony: { available: false, reason: 'No validated design is available for this configuration.' },
    veranda: { available: true },
    utility_room: { available: true },
    parking: { available: true },
    accessibility: { available: true }
  }
};

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
    if (url.includes('/api/v1/design-options')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(mockOptions)
      });
    }
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve({ workflowId: 'test-workflow-123' })
    });
  }));
});

test('loads options on mount and shows land range', async () => {
  render(
    <BrowserRouter>
      <IntakeForm />
    </BrowserRouter>
  );
  
  await waitFor(() => {
    expect(screen.getByText('5-8 perches (Approx. 1361-2178 sqft)')).toBeTruthy();
  });
});

test('progresses through steps and shows review before submit', async () => {
  render(
    <BrowserRouter>
      <IntakeForm />
    </BrowserRouter>
  );
  
  // Wait for load
  await waitFor(() => {
    expect(screen.getByText('5-8 perches (Approx. 1361-2178 sqft)')).toBeTruthy();
  });
  
  // Step 1 -> 2
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 2 (House Details)
  expect(screen.getByText('House Details')).toBeTruthy();
  
  // Try to go next without filling -> error
  fireEvent.click(screen.getByText(/Next/));
  expect(screen.getByText('Please select all house requirements.')).toBeTruthy();
  
  // Fill step 2
  fireEvent.change(screen.getAllByRole('combobox')[0], { target: { value: '1' } }); // floors
  fireEvent.change(screen.getAllByRole('combobox')[1], { target: { value: '2' } }); // beds
  fireEvent.change(screen.getAllByRole('combobox')[2], { target: { value: '1' } }); // baths
  fireEvent.change(screen.getAllByRole('combobox')[3], { target: { value: 'Modern Minimalist' } }); // style
  
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 3 (Priorities)
  expect(screen.getByText('Priorities')).toBeTruthy();
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 4 (Optional Features)
  expect(screen.getByText('Optional Features')).toBeTruthy();
  
  // Balcony should be disabled
  const balconyCheckbox = screen.getByRole('checkbox', { name: /Balcony/i }) as HTMLInputElement;
  expect(balconyCheckbox.disabled).toBe(true);
  expect(screen.getAllByText('No validated design is available for this configuration.').length).toBeGreaterThan(0);
  
  // Open Plan should be enabled
  const openPlanCheckbox = screen.getByRole('checkbox', { name: /Open-plan/i }) as HTMLInputElement;
  expect(openPlanCheckbox.disabled).toBe(false);
  
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 5 (Review)
  expect(screen.getByText('Review Configuration')).toBeTruthy();
  expect(screen.getByText(/5-8 perches/)).toBeTruthy();
  expect(screen.getByText(/1 floors, 2 beds, 1 baths/)).toBeTruthy();
  expect(screen.getByText(/Modern Minimalist/)).toBeTruthy();
  expect(screen.getByText('Validated designs available: 4')).toBeTruthy();
  
  // Submit
  fireEvent.click(screen.getByText('Generate AI Plan'));
});

test('one floor disables balcony with a program-rule reason', async () => {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((_url: string, init?: RequestInit) => {
    const request = init?.body ? JSON.parse(String(init.body)) : {};
    const response = request.floors === 1
      ? { ...mockOptions, floors: [1], features: { ...mockOptions.features, balcony: { available: false, reason: 'Balconies require a validated multi-floor design.' } } }
      : mockOptions;
    return Promise.resolve({ ok: true, json: () => Promise.resolve(response) });
  }));
  render(<BrowserRouter><IntakeForm /></BrowserRouter>);
  await screen.findByText('5-8 perches (Approx. 1361-2178 sqft)');
  fireEvent.click(screen.getByText(/Next/));
  fireEvent.change(screen.getAllByRole('combobox')[0], { target: { name: 'floors', value: '1' } });
  fireEvent.change(screen.getAllByRole('combobox')[1], { target: { name: 'bedrooms', value: '2' } });
  fireEvent.change(screen.getAllByRole('combobox')[2], { target: { name: 'bathrooms', value: '1' } });
  fireEvent.change(screen.getAllByRole('combobox')[3], { target: { name: 'architecturalStyle', value: 'Modern Minimalist' } });
  fireEvent.click(screen.getByText(/Next/));
  fireEvent.click(screen.getByText(/Next/));
  await waitFor(() => expect(screen.getByText('Balconies require a validated multi-floor design.')).toBeTruthy());
  expect((screen.getByRole('checkbox', { name: /Balcony/i }) as HTMLInputElement).disabled).toBe(true);
});

test('changing floors to one clears an invalid balcony selection', async () => {
  const compatible = { ...mockOptions, features: { ...mockOptions.features, balcony: { available: true } } };
  vi.stubGlobal('fetch', vi.fn().mockImplementation((_url: string, init?: RequestInit) => {
    const request = init?.body ? JSON.parse(String(init.body)) : {};
    const response = request.floors === 1
      ? { ...mockOptions, features: { ...mockOptions.features, balcony: { available: false, reason: 'Balconies require a validated multi-floor design.' } } }
      : compatible;
    return Promise.resolve({ ok: true, json: () => Promise.resolve(response) });
  }));
  render(<BrowserRouter><IntakeForm /></BrowserRouter>);
  await screen.findByText('5-8 perches (Approx. 1361-2178 sqft)');
  fireEvent.click(screen.getByText(/Next/));
  fireEvent.change(screen.getAllByRole('combobox')[0], { target: { name: 'floors', value: '2' } });
  fireEvent.change(screen.getAllByRole('combobox')[1], { target: { name: 'bedrooms', value: '2' } });
  fireEvent.change(screen.getAllByRole('combobox')[2], { target: { name: 'bathrooms', value: '1' } });
  fireEvent.change(screen.getAllByRole('combobox')[3], { target: { name: 'architecturalStyle', value: 'Modern Minimalist' } });
  fireEvent.click(screen.getByText(/Next/)); fireEvent.click(screen.getByText(/Next/));
  const balcony = screen.getByRole('checkbox', { name: /Balcony/i }) as HTMLInputElement;
  fireEvent.click(balcony);
  expect(balcony.checked).toBe(true);
  fireEvent.click(screen.getByText(/Back/)); fireEvent.click(screen.getByText(/Back/));
  fireEvent.change(screen.getAllByRole('combobox')[0], { target: { name: 'floors', value: '1' } });
  fireEvent.click(screen.getByText(/Next/)); fireEvent.click(screen.getByText(/Next/));
  await waitFor(() => expect((screen.getByRole('checkbox', { name: /Balcony/i }) as HTMLInputElement).checked).toBe(false));
});

test('zero compatible designs disables generation', async () => {
  const noMatches = { ...mockOptions, validatedDesignCount: 0 };
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve(noMatches) }));
  render(<BrowserRouter><IntakeForm /></BrowserRouter>);
  await screen.findByText('5-8 perches (Approx. 1361-2178 sqft)');
  fireEvent.click(screen.getByText(/Next/));
  fireEvent.change(screen.getAllByRole('combobox')[0], { target: { value: '1' } });
  fireEvent.change(screen.getAllByRole('combobox')[1], { target: { value: '2' } });
  fireEvent.change(screen.getAllByRole('combobox')[2], { target: { value: '1' } });
  fireEvent.change(screen.getAllByRole('combobox')[3], { target: { value: 'Modern Minimalist' } });
  fireEvent.click(screen.getByText(/Next/)); fireEvent.click(screen.getByText(/Next/)); fireEvent.click(screen.getByText(/Next/));
  expect(screen.getByText('Validated designs available: 0')).toBeTruthy();
  expect((screen.getByText('Generate AI Plan') as HTMLButtonElement).disabled).toBe(true);
});

test('shows suggestions on UNSUPPORTED_DESIGN_CONFIGURATION 400 error', async () => {
  // Override fetch for the generate endpoint to return 400
  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
    if (url.includes('/api/v1/design-options')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(mockOptions)
      });
    }
    if (url.includes('/api/v1/ai-generation/generate')) {
      return Promise.resolve({
        ok: false,
        json: () => Promise.resolve({
          code: 'UNSUPPORTED_DESIGN_CONFIGURATION',
          message: 'No validated design currently supports this exact configuration.',
          conflicts: ['parking'],
          suggestions: [{ field: 'parkingRequired', value: false, label: 'Continue without parking' }]
        })
      });
    }
    return Promise.resolve({ ok: true, json: () => Promise.resolve({}) });
  }));

  render(
    <BrowserRouter>
      <IntakeForm />
    </BrowserRouter>
  );

  // Skip to step 5
  await waitFor(() => expect(screen.getByText('5-8 perches (Approx. 1361-2178 sqft)')).toBeTruthy());
  
  fireEvent.click(screen.getByText(/Next/));
  fireEvent.change(screen.getAllByRole('combobox')[0], { target: { value: '1' } });
  fireEvent.change(screen.getAllByRole('combobox')[1], { target: { value: '2' } });
  fireEvent.change(screen.getAllByRole('combobox')[2], { target: { value: '1' } });
  fireEvent.change(screen.getAllByRole('combobox')[3], { target: { value: 'Modern Minimalist' } });
  fireEvent.click(screen.getByText(/Next/));
  fireEvent.click(screen.getByText(/Next/));
  fireEvent.click(screen.getByText(/Next/));
  
  // Submit
  fireEvent.click(screen.getByText('Generate AI Plan'));
  
  // Wait for the suggestion UI
  await waitFor(() => {
    expect(screen.getByText('This combination is not currently available.')).toBeTruthy();
    expect(screen.getByText('Continue without parking')).toBeTruthy();
  });

  // Clicking suggestion should update form and hide error
  fireEvent.click(screen.getByText('Continue without parking'));
  
  await waitFor(() => {
    // Should clear the error box
    expect(screen.queryByText('This combination is not currently available.')).toBeNull();
  });
});
