export type UserRole = 'Customer' | 'Architect' | 'Constructor' | 'Admin';

/** Roles the public registration form can offer. Must mirror the backend allowlist. */
export type PublicRegistrableRole = 'Customer' | 'Architect' | 'Constructor';

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
