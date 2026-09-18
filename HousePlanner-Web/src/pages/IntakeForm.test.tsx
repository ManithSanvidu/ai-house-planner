import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import IntakeForm from './IntakeForm';
import { expect, test, vi, beforeEach } from 'vitest';
import '@testing-library/jest-dom';

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
  features: {
    open_plan: { available: true },
    balcony: { available: false, reason: 'No validated design is available for this configuration.' },
    parking: { available: true },
    accessibility: { available: true }
  }
};

beforeEach(() => {
  global.fetch = vi.fn().mockImplementation((url: string) => {
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
  }) as any;
});

test('loads options on mount and shows land range', async () => {
  render(
    <BrowserRouter>
      <IntakeForm />
    </BrowserRouter>
  );
  
  await waitFor(() => {
    expect(screen.getByText('5-8 perches (Approx. 1361-2178 sqft)')).toBeInTheDocument();
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
    expect(screen.getByText('5-8 perches (Approx. 1361-2178 sqft)')).toBeInTheDocument();
  });
  
  // Step 1 -> 2
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 2 (House Details)
  expect(screen.getByText('House Details')).toBeInTheDocument();
  
  // Try to go next without filling -> error
  fireEvent.click(screen.getByText(/Next/));
  expect(screen.getByText('Please select all house requirements.')).toBeInTheDocument();
  
  // Fill step 2
  fireEvent.change(screen.getAllByRole('combobox')[0], { target: { value: '1' } }); // floors
  fireEvent.change(screen.getAllByRole('combobox')[1], { target: { value: '2' } }); // beds
  fireEvent.change(screen.getAllByRole('combobox')[2], { target: { value: '1' } }); // baths
  fireEvent.change(screen.getAllByRole('combobox')[3], { target: { value: 'Modern Minimalist' } }); // style
  
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 3 (Priorities)
  expect(screen.getByText('Priorities')).toBeInTheDocument();
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 4 (Optional Features)
  expect(screen.getByText('Optional Features')).toBeInTheDocument();
  
  // Balcony should be disabled
  const balconyCheckbox = screen.getByRole('checkbox', { name: /Balcony/i });
  expect(balconyCheckbox).toBeDisabled();
  expect(screen.getByText('No validated design is available for this configuration.')).toBeInTheDocument();
  
  // Open Plan should be enabled
  const openPlanCheckbox = screen.getByRole('checkbox', { name: /Open-plan/i });
  expect(openPlanCheckbox).not.toBeDisabled();
  
  fireEvent.click(screen.getByText(/Next/));
  
  // Step 5 (Review)
  expect(screen.getByText('Review Configuration')).toBeInTheDocument();
  expect(screen.getByText(/5-8 perches/)).toBeInTheDocument();
  expect(screen.getByText(/1 floors, 2 beds, 1 baths/)).toBeInTheDocument();
  expect(screen.getByText(/Modern Minimalist/)).toBeInTheDocument();
  
  // Submit
  fireEvent.click(screen.getByText('Generate AI Plan'));
  
  await waitFor(() => {
    expect(screen.getByText('AI Plan Generated!')).toBeInTheDocument();
  });
});
