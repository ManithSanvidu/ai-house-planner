import axios from 'axios';
import { auth } from './firebase';

// Remove tokens created by the retired mock-login implementation. Firebase owns session persistence.
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
    const user = auth.currentUser;
    if (user && config.headers) {
      // Firebase refreshes an expiring ID token as needed. Never persist it ourselves.
      const idToken = await user.getIdToken();
      config.headers.Authorization = `Bearer ${idToken}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

export default apiClient;
