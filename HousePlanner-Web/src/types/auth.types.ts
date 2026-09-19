export type UserRole = 'User' | 'Admin' | 'Architect' | 'Contractor' | 'Constructor';

export interface UserProfile {
  uid: string;
  email: string;
  fullName?: string;
  role: UserRole;
}

export interface AuthState {
  user: UserProfile | null;
  token: string | null; // In-memory Firebase ID Token
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error: string | null;
}
