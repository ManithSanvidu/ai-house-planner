import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, expect, test, vi } from 'vitest';
import AdminStaffPage from './AdminStaffPage';
import { staffService } from '../services/staffService';

vi.mock('../services/staffService',()=>({staffService:{list:vi.fn(),create:vi.fn(),setDisabled:vi.fn()}}));
const people=[{id:'a',fullName:'Nimal Silva',email:'nimal@example.com',role:'Architect' as const,status:'Active' as const},{id:'c',fullName:'Kasun Perera',email:'kasun@example.com',role:'Constructor' as const,status:'Disabled' as const}];
beforeEach(()=>{vi.clearAllMocks();vi.mocked(staffService.list).mockResolvedValue(people);vi.mocked(staffService.create).mockResolvedValue(people[0]);vi.mocked(staffService.setDisabled).mockResolvedValue(people[0])});

test('lists staff without exposing Firebase UID or passwords',async()=>{
 render(<AdminStaffPage/>);
 expect(await screen.findByText('Nimal Silva')).toBeTruthy();
 expect(screen.getByText('Kasun Perera')).toBeTruthy();
 expect(screen.queryByText(/firebase uid/i)).toBeNull();
 expect(screen.queryByText(/password hash/i)).toBeNull();
});

test('staff form only offers Architect and Constructor and creates through backend service',async()=>{
 render(<AdminStaffPage/>);await screen.findByText('Nimal Silva');
 fireEvent.click(screen.getByRole('button',{name:/add staff member/i}));
 const dialog=screen.getByRole('dialog',{name:'Add Staff Member'});
 const options=within(dialog).getAllByRole('option').map(option=>option.textContent);
 expect(options).toEqual(['Architect','Constructor']);
 const inputs=within(dialog).getAllByRole('textbox');
 fireEvent.change(inputs[0],{target:{value:'New Architect'}});fireEvent.change(inputs[1],{target:{value:'new@example.com'}});
 const passwords=dialog.querySelectorAll('input[type="password"]');
 fireEvent.change(passwords[0],{target:{value:'secret12'}});fireEvent.change(passwords[1],{target:{value:'secret12'}});
 fireEvent.click(within(dialog).getByRole('button',{name:'Create Staff Account'}));
 await waitFor(()=>expect(staffService.create).toHaveBeenCalledWith({fullName:'New Architect',email:'new@example.com',password:'secret12',role:'Architect'}));
});

test('shows an access-denied message for a forbidden response',async()=>{
 vi.mocked(staffService.list).mockRejectedValue({response:{status:403}});
 render(<AdminStaffPage/>);
 expect((await screen.findByRole('alert')).textContent).toContain('You do not have permission to manage staff accounts.');
});

test('shows the session message for an unauthenticated response',async()=>{
 vi.mocked(staffService.list).mockRejectedValue({response:{status:401}});
 render(<AdminStaffPage/>);
 expect((await screen.findByRole('alert')).textContent).toContain('Your session has expired. Please sign in again.');
});

test('shows the generic load error for a server response',async()=>{
 vi.mocked(staffService.list).mockRejectedValue({response:{status:500}});
 render(<AdminStaffPage/>);
 expect((await screen.findByRole('alert')).textContent).toContain('Could not load staff accounts.');
});

test('treats an empty successful response as an empty list',async()=>{
 vi.mocked(staffService.list).mockResolvedValue([]);
 render(<AdminStaffPage/>);
 expect(await screen.findByText('No staff accounts found.')).toBeTruthy();
 expect(screen.queryByRole('alert')).toBeNull();
});
