import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import IntakeForm from './IntakeForm';

const { startDesign } = vi.hoisted(() => ({
 startDesign: vi.fn(),
}));

vi.mock('../services/workflowService', () => ({
 workflowService: { startDesign },
}));

const renderPage = () => render(
 <BrowserRouter>
  <IntakeForm />
 </BrowserRouter>,
);

const input = (name: string) =>
 document.querySelector(`[name="${name}"]`) as HTMLInputElement;

beforeEach(() => {
 startDesign.mockReset();
 startDesign.mockResolvedValue({ workflowId: 'workflow-1' });
});

test('renders every intake section on one page', () => {
 renderPage();

 expect(screen.getByText('Land Details')).toBeTruthy();
 expect(screen.getByText('Terrain & Topography')).toBeTruthy();
 expect(screen.getByText('Plot Constraints (Optional)')).toBeTruthy();
 expect(screen.getByText('Design Preferences')).toBeTruthy();
 expect(screen.queryByText(/Step 1 of/)).toBeNull();
});

test('one-floor projects can clear an incompatible balcony selection', () => {
 renderPage();

 const balcony = screen.getByRole('checkbox', { name: 'Balcony' }) as HTMLInputElement;
 fireEvent.click(balcony);
 expect(balcony.checked).toBe(true);

 fireEvent.change(input('floors'), { target: { value: '1' } });
 fireEvent.click(balcony);
 expect(balcony.checked).toBe(false);
});

test('submits the current project requirements and shows success', async () => {
 renderPage();
 fireEvent.change(input('landSize'), { target: { value: '10' } });
 fireEvent.change(input('bedrooms'), { target: { value: '2' } });
 fireEvent.change(input('bathrooms'), { target: { value: '1' } });
 fireEvent.change(input('floors'), { target: { value: '1' } });

 fireEvent.click(screen.getByRole('button', { name: 'Generate AI Plan' }));

 await screen.findByText('AI Plan Generated!');
 expect(startDesign).toHaveBeenCalledOnce();
 expect(startDesign).toHaveBeenCalledWith(expect.objectContaining({
  landSizePerches: 10,
  preferences: expect.objectContaining({
   bedrooms: 2,
   bathrooms: 1,
   floors: 1,
  }),
 }));
});

test('submits without any budget field in payload', async () => {
 renderPage();
 fireEvent.change(input('landSize'), { target: { value: '8' } });

 fireEvent.click(screen.getByRole('button', { name: 'Generate AI Plan' }));

 await waitFor(() => expect(startDesign).toHaveBeenCalledOnce());
 const payload = startDesign.mock.calls[0][0];
 expect(payload).not.toHaveProperty('budgetLkr');
});
