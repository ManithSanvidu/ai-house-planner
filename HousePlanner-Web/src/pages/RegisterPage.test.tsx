import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import authReducer from '../features/auth/authSlice';

vi.mock('../features/auth/authService', () => ({ default: {
 register: vi.fn(), login: vi.fn(), googleLogin: vi.fn(), verifySession: vi.fn(), logout: vi.fn(),
} }));
const mockNavigate=vi.fn();
vi.mock('react-router-dom',async()=>({...await vi.importActual('react-router-dom'),useNavigate:()=>mockNavigate}));

import RegisterPage from './RegisterPage';
import LoginPage from './LoginPage';
import authService from '../features/auth/authService';

const store=()=>configureStore({reducer:{auth:authReducer}});
const renderRegister=()=>render(<Provider store={store()}><MemoryRouter><RegisterPage/></MemoryRouter></Provider>);

describe('public Customer registration',()=>{
 beforeEach(()=>{vi.clearAllMocks()});
 it('renders one simple form without role selection',()=>{
 renderRegister();
 expect(screen.getByRole('heading',{name:'Create your HousePlanner account'})).toBeTruthy();
 expect(screen.queryByRole('radio')).toBeNull();
 expect(screen.queryByText(/^Architect$/)).toBeNull();
 expect(screen.queryByText(/^Constructor$/)).toBeNull();
 expect(screen.queryByText(/^Admin$/)).toBeNull();
 expect(screen.getByText(/Architect and Constructor accounts are created/)).toBeTruthy();
 });
 it('validates password confirmation before Supabase registration',async()=>{
 const {container}=renderRegister();
 fireEvent.change(container.querySelector('#reg-fullname')!,{target:{value:'Customer'}});
 fireEvent.change(container.querySelector('#reg-email')!,{target:{value:'customer@example.com'}});
 fireEvent.change(container.querySelector('#reg-password')!,{target:{value:'secret12'}});
 fireEvent.change(container.querySelector('#reg-confirm-password')!,{target:{value:'different'}});
 fireEvent.click(container.querySelector('#register-submit')!);
 expect(await screen.findByText('Passwords do not match.')).toBeTruthy();
 expect(authService.register).not.toHaveBeenCalled();
 });
 it('registers without sending a role decision',async()=>{
 vi.mocked(authService.register).mockResolvedValue({user:{uid:'u',email:'customer@example.com',role:'Customer'},token:'t'});
 const {container}=renderRegister();
 fireEvent.change(container.querySelector('#reg-fullname')!,{target:{value:'Customer'}});
 fireEvent.change(container.querySelector('#reg-email')!,{target:{value:'customer@example.com'}});
 fireEvent.change(container.querySelector('#reg-password')!,{target:{value:'secret12'}});
 fireEvent.change(container.querySelector('#reg-confirm-password')!,{target:{value:'secret12'}});
 fireEvent.click(container.querySelector('#register-submit')!);
 await waitFor(()=>expect(authService.register).toHaveBeenCalledWith('customer@example.com','secret12','Customer'));
 });
});

describe('LoginPage',()=>{
 it('never asks the user to choose a role',()=>{
 render(<Provider store={store()}><MemoryRouter><LoginPage/></MemoryRouter></Provider>);
 expect(screen.queryByRole('radio')).toBeNull();
 expect(screen.queryByText(/account type/i)).toBeNull();
 });
});
