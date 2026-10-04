import axios from 'axios';
import { supabase } from '../lib/supabase';

// Remove tokens created by the retired mock-login implementation. Supabase owns session persistence.
localStorage.removeItem('mockToken');
localStorage.removeItem('mockUser');

const apiClient = axios.create({
 baseURL: import.meta.env.VITE_API_BASE_URL,
 timeout: 300000, // 5 minutes to survive cold starts and long generation
 headers: {
  'Content-Type': 'application/json',
 },
});

// Axios request interceptor to inject the Bearer token dynamically
apiClient.interceptors.request.use(
 async (config) => {
  let { data: { session } } = await supabase.auth.getSession();
  const expiresSoon = session?.expires_at && session.expires_at * 1000 <= Date.now() + 60_000;
  if (expiresSoon) {
   const { data } = await supabase.auth.refreshSession();
   session = data.session;
  }
  if (session?.access_token && config.headers) {
   config.headers.Authorization = `Bearer ${session.access_token}`;
  }
  return config;
 },
 (error) => {
  return Promise.reject(error);
 }
);

// Axios errors are frequently logged by page-level handlers. Remove the bearer
// credential from the rejected config so developer tools cannot expose it.
apiClient.interceptors.response.use(
 response => response,
 (error) => {
  if (error?.config?.headers) {
   delete error.config.headers.Authorization;
   delete error.config.headers.authorization;
  }
  return Promise.reject(error);
 },
);

export default apiClient;
