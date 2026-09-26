import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, expect, test, vi } from 'vitest';
import AdminStaffPage from './AdminStaffPage';
import { staffService } from '../services/staffService';

vi.mock('../services/staffService',()=>({staffService:{list:vi.fn(),create:vi.fn(),update:vi.fn(),setDisabled:vi.fn()}}));
const people=[{id:'a',fullName:'Nimal Silva',email:'nimal@example.com',role:'Architect' as const,status:'Active' as const},{id:'c',fullName:'Kasun Perera',email:'kasun@example.com',role:'Constructor' as const,status:'Disabled' as const}];
beforeEach(()=>{vi.clearAllMocks();vi.mocked(staffService.list).mockResolvedValue(people);vi.mocked(staffService.create).mockResolvedValue(people[0]);vi.mocked(staffService.update).mockResolvedValue(people[0]);vi.mocked(staffService.setDisabled).mockResolvedValue(people[0])});

test('lists staff without exposing Supabase UID or passwords',async()=>{
 render(<AdminStaffPage/>);
 expect(await screen.findByText('Nimal Silva')).toBeTruthy();
 expect(screen.getByText('Kasun Perera')).toBeTruthy();
 expect(screen.queryByText(/supabase uid/i)).toBeNull();
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

test('staff filters remain visible, expose selection, and preserve filtering behavior',async()=>{
 render(<AdminStaffPage/>);
 await screen.findByText('Nimal Silva');
 const all=screen.getByRole('button',{name:'All'});
 const architects=screen.getByRole('button',{name:'Architects'});
 const constructors=screen.getByRole('button',{name:'Constructors'});

 expect(all.getAttribute('aria-pressed')).toBe('true');
 expect(all.className).toContain('bg-indigo-600');
 expect(architects.getAttribute('aria-pressed')).toBe('false');
 expect(architects.className).toContain('bg-slate-900');
 expect(constructors.getAttribute('aria-pressed')).toBe('false');

 fireEvent.click(architects);
 await waitFor(()=>expect(staffService.list).toHaveBeenLastCalledWith('Architect'));
 expect(architects.getAttribute('aria-pressed')).toBe('true');
 expect(architects.className).toContain('bg-indigo-600');

 fireEvent.click(constructors);
 await waitFor(()=>expect(staffService.list).toHaveBeenLastCalledWith('Constructor'));
 expect(constructors.getAttribute('aria-pressed')).toBe('true');

 fireEvent.click(all);
 await waitFor(()=>expect(staffService.list).toHaveBeenLastCalledWith(undefined));
 expect(all.getAttribute('aria-pressed')).toBe('true');
});

test('edits a staff member with safe pre-filled fields and preserves the current filter',async()=>{
 const updated={...people[0],fullName:'Nimal Fernando',email:'nimal.new@example.com',role:'Constructor' as const};
 vi.mocked(staffService.update).mockResolvedValue(updated);
 render(<AdminStaffPage/>);
 await screen.findByText('Nimal Silva');
 fireEvent.click(screen.getByRole('button',{name:'Architects'}));
 await waitFor(()=>expect(staffService.list).toHaveBeenLastCalledWith('Architect'));

 fireEvent.click(screen.getAllByRole('button',{name:'Edit'})[0]);
 const dialog=screen.getByRole('dialog',{name:'Edit Staff Member'});
 expect((within(dialog).getByLabelText('Full Name') as HTMLInputElement).value).toBe('Nimal Silva');
 expect((within(dialog).getByLabelText('Email') as HTMLInputElement).value).toBe('nimal@example.com');
 expect(within(dialog).getAllByRole('option').map(option=>option.textContent)).toEqual(['Architect','Constructor']);
 expect(within(dialog).queryByText(/supabase uid|roleid|password hash|token/i)).toBeNull();
 expect(dialog.querySelector('input[type="password"]')).toBeNull();

 fireEvent.change(within(dialog).getByLabelText('Full Name'),{target:{value:'Nimal Fernando'}});
 fireEvent.change(within(dialog).getByLabelText('Email'),{target:{value:'nimal.new@example.com'}});
 fireEvent.change(within(dialog).getByLabelText('Role'),{target:{value:'Constructor'}});
 vi.mocked(staffService.list).mockResolvedValue([{...updated}]);
 fireEvent.click(within(dialog).getByRole('button',{name:'Save Changes'}));

 await waitFor(()=>expect(staffService.update).toHaveBeenCalledWith('a',{
  fullName:'Nimal Fernando',email:'nimal.new@example.com',role:'Constructor'
 }));
 await screen.findByText('Nimal Fernando');
 expect(staffService.list).toHaveBeenLastCalledWith('Architect');
 expect(screen.getByRole('status').textContent).toContain('Staff account updated.');
});
