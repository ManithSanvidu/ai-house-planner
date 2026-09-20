import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import authReducer from '../features/auth/authSlice';

// Mock framer-motion so AnimatePresence renders children immediately (no CSS transitions in jsdom)
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.HTMLAttributes<HTMLDivElement>) =>
      <div {...props}>{children}</div>,
  },
  AnimatePresence: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

// Mock the auth service so we don't actually hit Firebase or the backend
vi.mock('../features/auth/authService', () => ({
  default: {
    register: vi.fn(),
    login: vi.fn(),
    googleLogin: vi.fn(),
    verifySession: vi.fn(),
    logout: vi.fn(),
  },
}));

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...(actual as object), useNavigate: () => mockNavigate };
});

import RegisterPage from '../pages/RegisterPage';
import authService from '../features/auth/authService';
import LoginPage from '../pages/LoginPage';

function makeStore() {
  return configureStore({ reducer: { auth: authReducer } });
}

function renderRegisterPage() {
  return render(
    <Provider store={makeStore()}>
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>
    </Provider>,
  );
}

describe('RegisterPage — step 1: account type selection', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  it('renders both Customer and Architect role options', () => {
    const { container } = renderRegisterPage();
    expect(container.querySelector('#role-option-customer')).toBeTruthy();
    expect(container.querySelector('#role-option-architect')).toBeTruthy();
  })
;

  it('does NOT render an Admin option', () => {
    renderRegisterPage();
    expect(screen.queryByRole('radio', { name: /admin/i })).toBeNull();
  });

  it('does NOT render any SuperAdmin option', () => {
    renderRegisterPage();
    expect(screen.queryByText(/superadmin/i)).toBeNull();
  });

  it('Continue button is disabled until a role is selected', () => {
    renderRegisterPage();
    const continueBtn = screen.getByRole('button', { name: /continue as/i });
    expect((continueBtn as HTMLButtonElement).disabled).toBe(true);
  });

  it('enables Continue button after selecting Customer', () => {
    const { container } = renderRegisterPage();
    fireEvent.click(container.querySelector('#role-option-customer')!);
    const btn = screen.getByRole('button', { name: /continue as customer/i });
    expect((btn as HTMLButtonElement).disabled).toBe(false);
  });

  it('enables Continue button after selecting Architect', () => {
    const { container } = renderRegisterPage();
    fireEvent.click(container.querySelector('#role-option-architect')!);
    const btn = screen.getByRole('button', { name: /continue as architect/i });
    expect((btn as HTMLButtonElement).disabled).toBe(false);
  });

  it('advances to step 2 after selecting a role and clicking Continue', () => {
    const { container } = renderRegisterPage();
    fireEvent.click(container.querySelector('#role-option-customer')!);
    fireEvent.click(screen.getByRole('button', { name: /continue as customer/i }));
    // After advancing, step-2 inputs appear in DOM
    expect(container.querySelector('#reg-email')).toBeTruthy();
    expect(container.querySelector('#register-submit')).toBeTruthy();
  });
});

describe('RegisterPage — step 2: form details', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    vi.clearAllMocks();
  });

  async function goToStep2(role: 'Customer' | 'Architect' = 'Customer') {
    const { container } = renderRegisterPage();
    const roleId = role === 'Customer' ? '#role-option-customer' : '#role-option-architect';
    fireEvent.click(container.querySelector(roleId)!);
    fireEvent.click(screen.getByRole('button', { name: new RegExp(`continue as ${role}`, 'i') }));
  }

  it('displays the selected role as a badge on step 2', async () => {
    await goToStep2('Architect');
    // The badge has the role name text
    const allArchitect = screen.getAllByText('Architect');
    expect(allArchitect.length).toBeGreaterThan(0);
  });

  it('allows user to go back and change role by clicking Change', async () => {
    const { container } = renderRegisterPage();
    const roleId = '#role-option-customer';
    fireEvent.click(container.querySelector(roleId)!);
    fireEvent.click(screen.getByRole('button', { name: /continue as customer/i }));
    // Click the Change button — it's a plain <button> element
    const changeBtn = container.querySelector('button.text-xs.text-gray-400') as HTMLButtonElement
      ?? Array.from(container.querySelectorAll('button')).find(b => b.textContent?.includes('Change'));
    expect(changeBtn).toBeTruthy();
    fireEvent.click(changeBtn!);
    // After going back, the role option cards should still be in the DOM
    expect(container.querySelector('#role-option-customer')).toBeTruthy();
  });

  it('shows an error when passwords do not match', async () => {
    const { container } = await (() => {
      const result = renderRegisterPage();
      fireEvent.click(result.container.querySelector('#role-option-customer')!);
      fireEvent.click(screen.getByRole('button', { name: /continue as customer/i }));
      return Promise.resolve(result);
    })();
    fireEvent.change(container.querySelector('#reg-fullname')!, { target: { value: 'Test User' } });
    fireEvent.change(container.querySelector('#reg-email')!, { target: { value: 'test@example.com' } });
    fireEvent.change(container.querySelector('#reg-password')!, { target: { value: 'password123' } });
    fireEvent.change(container.querySelector('#reg-confirm-password')!, { target: { value: 'different' } });
    fireEvent.click(container.querySelector('#register-submit')!);
    const err = await screen.findByText(/passwords do not match/i);
    expect(err).toBeTruthy();
    expect(authService.register).not.toHaveBeenCalled();
  });

  it('calls authService.register with the correct role on valid submission', async () => {
    (authService.register as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      user: { uid: 'uid-1', email: 'test@example.com', fullName: 'Test User', role: 'Customer' },
      token: 'mock-token',
    });

    const { container } = renderRegisterPage();
    fireEvent.click(container.querySelector('#role-option-customer')!);
    fireEvent.click(screen.getByRole('button', { name: /continue as customer/i }));
    fireEvent.change(container.querySelector('#reg-fullname')!, { target: { value: 'Test User' } });
    fireEvent.change(container.querySelector('#reg-email')!, { target: { value: 'test@example.com' } });
    fireEvent.change(container.querySelector('#reg-password')!, { target: { value: 'password123' } });
    fireEvent.change(container.querySelector('#reg-confirm-password')!, { target: { value: 'password123' } });
    fireEvent.click(container.querySelector('#register-submit')!);

    await waitFor(() => {
      expect(authService.register).toHaveBeenCalledWith(
        'test@example.com',
        'password123',
        'Test User',
        'Customer',
      );
    });
  });
});

describe('LoginPage — must NOT ask for account type', () => {
  it('login page has no role selection elements', () => {
    render(
      <Provider store={makeStore()}>
        <MemoryRouter>
          <LoginPage />
        </MemoryRouter>
      </Provider>,
    );
    expect(screen.queryByRole('radio')).toBeNull();
    expect(screen.queryByText(/account type/i)).toBeNull();
    // Has email and password fields only
    expect(screen.getByPlaceholderText(/name@example.com/i)).toBeTruthy();
    expect(screen.getByPlaceholderText(/••••••••/)).toBeTruthy();
  });
});
