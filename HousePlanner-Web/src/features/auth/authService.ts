import { GoogleAuthProvider, signInWithEmailAndPassword, signInWithPopup, signOut as firebaseSignOut, type UserCredential, setPersistence, browserSessionPersistence } from 'firebase/auth';
import { auth } from '../../services/firebase';
import apiClient from '../../services/apiClient';
import type { UserProfile } from '../../types/auth.types';

/**
 * Service to manage Firebase Authentication and backend token exchange.
 */
const authService = {
  googleLogin: async (): Promise<{ user: UserProfile; token: string }> => {
    await setPersistence(auth, browserSessionPersistence);
    const credential = await signInWithPopup(auth, new GoogleAuthProvider());
    const token = await credential.user.getIdToken();
    
    const response = await apiClient.post<{ uid:string; email:string; role:UserProfile['role'] }>('/auth/session');
    return { user:{uid:response.data.uid,email:response.data.email,role:response.data.role}, token };
  },
  /**
   * Signs in user using Firebase, retrieves the token, verifies it with the backend,
   * and returns the user's role/details.
   */
  login: async (email: string, password: string): Promise<{ user: UserProfile; token: string }> => {
    // 1. Authenticate with Firebase Authentication (Production mode)
    await setPersistence(auth, browserSessionPersistence);
    const credential: UserCredential = await signInWithEmailAndPassword(auth, email, password);
    const fbUser = credential.user;

    if (!fbUser) {
      throw new Error('Failed to retrieve user from Firebase Authentication.');
    }

    // 2. Fetch the ID token
    const token = await fbUser.getIdToken();

    // 3. Validate the bearer token and synchronize/load the application user.
    const response = await apiClient.post<{ uid: string; email: string; role: import('../../types/auth.types').UserRole }>(
      '/auth/session'
    );

    // Return the authenticated details
    return {
      user: {
        uid: response.data.uid,
        email: response.data.email,
        role: response.data.role,
      },
      token,
    };
  },

  /**
   * Verifies the current Firebase session on reload.
   */
  verifySession: async (): Promise<{ user: UserProfile; token: string }> => {
    return new Promise((resolve, reject) => {
      const unsubscribe = auth.onAuthStateChanged(async (fbUser) => {
        unsubscribe(); // Only run once

        if (!fbUser) {
          return reject(new Error('No active session'));
        }

        try {
          const token = await fbUser.getIdToken();
          const response = await apiClient.post<{ uid: string; email: string; role: import('../../types/auth.types').UserRole }>(
            '/auth/session'
          );

          resolve({
            user: {
              uid: response.data.uid,
              email: response.data.email,
              role: response.data.role,
            },
            token,
          });
        } catch (error) {
          reject(error);
        }
      });
    });
  },

  /**
   * Signs out of Firebase and clears the in-memory token.
   */
  logout: async (): Promise<void> => {
    await firebaseSignOut(auth);
  },
};

export default authService;
