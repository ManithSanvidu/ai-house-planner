export type UserRole = 'Customer' | 'Architect' | 'Constructor' | 'Admin';

export interface UserProfile {
  uid: string;
  email: string;
  fullName?: string;
  role: UserRole;
}

export interface AuthState {
  user: UserProfile | null;
  token: string | null; // In-memory Supabase ID Token
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error: string | null;
}
