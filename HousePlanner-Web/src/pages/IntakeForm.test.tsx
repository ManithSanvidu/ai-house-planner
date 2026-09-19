import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import IntakeForm from './IntakeForm';

const options = {
  landRanges: [{ id: 'LAND_5_8', label: '5–8 perches', minPerches: 5, maxPerches: 8, approxSqft: '1,361–2,178 sq ft' }],
  plotShapes: ['BALANCED'], floors: [1, 2], bedrooms: [2, 3], bathrooms: [1, 2],
  architecturalStyles: ['Modern'], validatedDesignCount: 3,
  features: { open_plan: { available: true }, master_ensuite: { available: true }, separate_dining: { available: true }, home_office: { available: true }, balcony: { available: true }, veranda: { available: true }, utility_room: { available: true }, parking: { available: true }, accessibility: { available: true } }
};

beforeEach(() => vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string, init?: RequestInit) => {
  if (url.includes('ai-generation')) return Promise.resolve({ ok: true, json: () => Promise.resolve({ workflowId: 'workflow-1' }) });
  const body = init?.body ? JSON.parse(String(init.body)) : {};
  const result = body.floors === 1 ? { ...options, features: { ...options.features, balcony: { available: false, reason: 'Balconies require a validated multi-floor design.' } } } : options;
  return Promise.resolve({ ok: true, json: () => Promise.resolve(result) });
})));

const renderPage = () => render(<BrowserRouter><IntakeForm /></BrowserRouter>);

test('renders every intake section on one page', async () => {
  renderPage();
  expect(await screen.findByText('1. Land Details')).toBeTruthy();
  expect(screen.getByText('2. House Requirements')).toBeTruthy();
  expect(screen.getByText('3. Priority')).toBeTruthy();
  expect(screen.getByText('4. Optional Features')).toBeTruthy();
  expect(screen.queryByText(/Step 1 of/)).toBeNull();
});

test('compatibility restrictions disable and clear balcony for one floor', async () => {
  renderPage(); await screen.findByText('1. Land Details');
  fireEvent.click(screen.getByRole('button', { name: '2 floors' }));
  await waitFor(() => expect((screen.getByRole('checkbox', { name: 'Balcony' }) as HTMLInputElement).disabled).toBe(false));
  fireEvent.click(screen.getByText('Balcony'));
  fireEvent.click(screen.getByRole('button', { name: '1 floor' }));
  await waitFor(() => {
    const balcony = screen.getByRole('checkbox', { name: /Balcony/ }) as HTMLInputElement;
    expect(balcony.disabled).toBe(true); expect(balcony.checked).toBe(false);
  });
  expect(screen.getByText('Balconies require a validated multi-floor design.')).toBeTruthy();
});

test('zero matches disables generation and a valid configuration submits', async () => {
  const fetchMock = vi.mocked(fetch);
  renderPage(); await screen.findByText('1. Land Details');
  fireEvent.click(screen.getByRole('button', { name: '1 floor' }));
  fireEvent.click(screen.getByRole('button', { name: '2 bed' }));
  fireEvent.click(screen.getByRole('button', { name: '1 bath' }));
  fireEvent.change(screen.getByLabelText('Architectural style'), { target: { value: 'Modern' } });
  await waitFor(() => expect(screen.getByText('Validated designs available: 3')).toBeTruthy());
  fireEvent.click(screen.getByRole('button', { name: 'Generate Design' }));
  await screen.findByText('Design generation started');
  expect(fetchMock.mock.calls.some(([url]) => String(url).includes('ai-generation/generate'))).toBe(true);
});

test('zero compatible designs disables Generate Design', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ ...options, validatedDesignCount: 0 }) }));
  renderPage();
  expect(await screen.findByText('Validated designs available: 0')).toBeTruthy();
  expect((screen.getByRole('button', { name: 'Generate Design' }) as HTMLButtonElement).disabled).toBe(true);
});
