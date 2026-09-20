import apiClient from './apiClient';

export type StaffRole = 'Architect' | 'Constructor';
export interface StaffAccount { id:string; fullName:string; email:string; role:StaffRole; status:'Active'|'Disabled'|'Unavailable' }
export interface CreateStaffRequest { fullName:string; email:string; password:string; role:StaffRole }

export const staffService = {
  list: async (role?:StaffRole) => (await apiClient.get<StaffAccount[]>('/admin/staff',{params:role?{role}:undefined})).data,
  create: async (request:CreateStaffRequest) => (await apiClient.post<StaffAccount>('/admin/staff',request)).data,
  setDisabled: async (id:string,disabled:boolean) => (await apiClient.patch<StaffAccount>(`/admin/staff/${id}/status`,{disabled})).data,
};
