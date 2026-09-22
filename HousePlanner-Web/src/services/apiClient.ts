import axios from 'axios';
import { supabase } from '../lib/supabase';

// Remove tokens created by the retired mock-login implementation. Supabase owns session persistence.
localStorage.removeItem('mockToken');
localStorage.removeItem('mockUser');

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Axios request interceptor to inject the Bearer token dynamically
apiClient.interceptors.request.use(
  async (config) => {
    const { data: { session } } = await supabase.auth.getSession();
    if (session?.access_token && config.headers) {
      config.headers.Authorization = `Bearer ${session.access_token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

export default apiClient;
