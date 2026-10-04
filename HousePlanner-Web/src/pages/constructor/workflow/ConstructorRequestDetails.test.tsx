import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ConstructorRequestDetails from './ConstructorRequestDetails';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';

vi.mock('../../../services/constructorWorkflowService');

const baseRequest = {
  id: 'request-1',
  status: 'Pending',
  title: 'Approved Design v1',
  customerName: 'Customer',
  requestedAt: '2026-10-03T00:00:00Z',
  bedrooms: 3,
  bathrooms: 2,
  floorCount: 1,
  area: 1200,
  cost: null,
};

const renderPage = () => render(
  <MemoryRouter initialEntries={['/constructor/requests/request-1']}>
    <Routes>
      <Route path="/constructor/requests/:requestId" element={<ConstructorRequestDetails />} />
    </Routes>
  </MemoryRouter>,
);

describe('ConstructorRequestDetails approved design', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders the saved floor plan without using a legacy image URL', async () => {
    const signedUrl = 'https://project.supabase.co/storage/v1/object/sign/ai-visualizations/file.png?token=x';
    vi.mocked(constructorWorkflowService.getConstructorRequest).mockResolvedValue({
      ...baseRequest,
      aiVisualizationUrl: signedUrl,
      aiVisualizationStatus: 'completed',
      technicalPlanImage: 'http://localhost:8001/plans/legacy.png',
      layoutJson: '{"rooms":[{"room_type":"bedroom"}]}',
    });

    renderPage();

    expect(await screen.findByText('Floor Plan')).toBeInTheDocument();
    expect(screen.queryByRole('img', { name: /AI visualization of the approved house design/i })).not.toBeInTheDocument();
    expect(document.querySelector(`[src="http://localhost:8001/plans/legacy.png"]`)).toBeNull();
  });

  it('keeps design information available when AI visualization failed', async () => {
    vi.mocked(constructorWorkflowService.getConstructorRequest).mockResolvedValue({
      ...baseRequest,
      aiVisualizationUrl: null,
      aiVisualizationStatus: 'failed',
      technicalPlanImage: '/plans/legacy.png',
      layoutJson: '{"rooms":[]}',
    });

    renderPage();

    await waitFor(() => expect(screen.getByText('Design Information')).toBeInTheDocument());
    expect(screen.getByText('Floor Plan')).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(screen.getByText('Design Information')).toBeInTheDocument();
    expect(screen.queryByText('No layout geometry available.')).not.toBeInTheDocument();
  });
});
