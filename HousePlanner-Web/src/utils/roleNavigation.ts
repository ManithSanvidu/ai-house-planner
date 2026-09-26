import type { UserRole } from '../types/auth.types';

export const roleHomePath = (role:UserRole) => {
 if(role==='Architect') return '/architect/dashboard';
 if(role==='Constructor') return '/constructor/dashboard';
 return '/dashboard';
};
