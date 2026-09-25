import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import HomePage from './HomePage';
import IntakeForm from './IntakeForm';
import apiClient from '../services/apiClient';

vi.mock('../services/apiClient', () => ({
  default: { post: vi.fn() },
}));

// We need to mock Three.js/Fiber since it doesn't render well in vitest/jsdom without canvas mock
vi.mock('@react-three/fiber', () => ({
  Canvas: ({ children }: any) => <div data-testid="canvas-mock">{children}</div>,
  useFrame: () => {},
}));
vi.mock('@react-three/drei', () => ({
  OrbitControls: () => null,
  Html: () => null,
  useGLTF: () => ({ nodes: {}, materials: {} }),
}));
vi.mock('framer-motion', () => ({
  motion: {
    nav: ({ children, ...props }: any) => <nav {...props}>{children}</nav>,
    div: ({ children, ...props }: any) => <div {...props}>{children}</div>,
    img: ({ children, ...props }: any) => <img {...props} />
  },
  useScroll: () => ({ scrollY: 0 }),
  useTransform: () => 0,
  AnimatePresence: ({ children }: any) => <>{children}</>,
}));

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

const renderApp = () => render(
  <MemoryRouter initialEntries={['/']}>
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/dashboard/new-project" element={<div data-testid="intake-form">Intake Form Route</div>} />
    </Routes>
  </MemoryRouter>
);

beforeEach(() => {
  vi.clearAllMocks();
});

test('Valid DESIGN_REQUEST shows Continue button and navigates', async () => {
  const mockPost = vi.mocked(apiClient.post);
  mockPost.mockResolvedValueOnce({
    data: {
      intent: 'DESIGN_REQUEST',
      reply: 'Your request is feasible. I can start the design setup with these requirements.',
      action: {
        type: 'CONTINUE_TO_DESIGN',
        payload: {
          requirements: {
            bedrooms: 4,
            bathrooms: 2,
            floors: 2,
            land_size: 25,
            land_unit: 'perches'
          },
          feasibility: {
            can_proceed: true,
            suggestions: []
          }
        }
      }
    }
  });

  renderApp();

  const input = screen.getByPlaceholderText('Describe your dream home...');
  fireEvent.change(input, { target: { value: 'Design me a 4 bedroom 2 floor modern house on 25 perch' } });
  
  const genBtn = screen.getByText('GENERATE');
  fireEvent.click(genBtn);

  await waitFor(() => {
    expect(screen.getByText('Your request is feasible. I can start the design setup with these requirements.')).toBeTruthy();
  });

  const continueBtn = screen.getByText('Continue to Design');
  fireEvent.click(continueBtn);

  // Navigates to intake form
  await waitFor(() => {
    expect(screen.getByTestId('intake-form')).toBeTruthy();
  });
});

test('Invalid DESIGN_REQUEST hides Continue button and shows suggestions', async () => {
  const mockPost = vi.mocked(apiClient.post);
  mockPost.mockResolvedValueOnce({
    data: {
      intent: 'DESIGN_REQUEST',
      reply: 'Your request is not supported with the available buildable area or catalogue.',
      action: {
        type: 'NONE',
        payload: {
          feasibility: {
            can_proceed: false,
            suggestions: [
              'Reduce bedrooms from 6 to 4',
              'Use 2 floors instead of 1'
            ]
          }
        }
      }
    }
  });

  renderApp();

  const input = screen.getByPlaceholderText('Describe your dream home...');
  fireEvent.change(input, { target: { value: 'Design me a 6 bedroom single floor house on 3 perch land' } });
  
  const genBtn = screen.getByText('GENERATE');
  fireEvent.click(genBtn);

  await waitFor(() => {
    expect(screen.getByText('Your request is not supported with the available buildable area or catalogue.')).toBeTruthy();
  });

  // Continue button should not be there
  expect(screen.queryByText('Continue to Design')).toBeNull();

  // Suggestions should be visible
  expect(screen.getByText('Reduce bedrooms from 6 to 4')).toBeTruthy();
  expect(screen.getByText('Use 2 floors instead of 1')).toBeTruthy();
});
