import { supabase } from '../../lib/supabase';
import apiClient from '../../services/apiClient';
import type { UserProfile } from '../../types/auth.types';

interface BackendUserDto {
 uid: string;
 email: string;
 fullName: string;
 role: UserProfile['role'];
}

const authService = {
 register: async (
  email: string,
  password: string,
  fullName: string,
 ): Promise<{ user: UserProfile; token: string }> => {
  const { data: authData, error: signUpError } = await supabase.auth.signUp({
   email,
   password,
  });
  
  if (signUpError) throw signUpError;
  if (!authData.session) throw new Error("Session missing after signup");
  
  const token = authData.session.access_token;
  
  const response = await apiClient.post<BackendUserDto>('/auth/register', {
   fullName,
  });
  
  const userProfile: UserProfile = {
   uid: response.data.uid,
   email: response.data.email,
   fullName: response.data.fullName,
   role: response.data.role,
  };

  return { user: userProfile, token };
 },

 login: async (email: string, password: string): Promise<{ user: UserProfile; token: string }> => {
  const { data, error } = await supabase.auth.signInWithPassword({ email, password });
  if (error) throw error;
  if (!data.session) throw new Error("Session missing after signin");
  
  const token = data.session.access_token;

  const response = await apiClient.post<BackendUserDto>('/auth/session');
  return {
   token,
   user: {
    uid: response.data.uid,
    email: response.data.email,
    fullName: response.data.fullName,
    role: response.data.role,
   },
  };
 },

 loginWithGoogle: async (): Promise<{ user: UserProfile; token: string }> => {
  throw new Error("Google OAuth not implemented for Supabase yet.");
 },

 logout: async (): Promise<void> => {
  localStorage.clear();
  sessionStorage.clear();
  await supabase.auth.signOut();
 },

 initializeSession: async (): Promise<{ user: UserProfile; token: string }> => {
  const { data: { session } } = await supabase.auth.getSession();
  if (!session) throw new Error('No persistent session found.');
  const token = session.access_token;
  const response = await apiClient.get<BackendUserDto>('/auth/me');
  return {
   token,
   user: {
    uid: response.data.uid,
    email: response.data.email,
    fullName: response.data.fullName,
    role: response.data.role,
   },
  };
 },
};

export default authService;
