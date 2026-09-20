import {
  GoogleAuthProvider,
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
  signInWithPopup,
  signOut as firebaseSignOut,
  type UserCredential,
  setPersistence,
  browserSessionPersistence,
} from 'firebase/auth';
import { auth } from '../../services/firebase';
import apiClient from '../../services/apiClient';
import type { PublicRegistrableRole, UserProfile } from '../../types/auth.types';

/** Shape returned by all /auth/* endpoints. */
interface BackendUserDto {
  uid: string;
  email: string;
  fullName: string;
  role: UserProfile['role'];
}

/**
 * Service to manage Firebase Authentication and backend token exchange.
 */
const authService = {
  /**
   * Registers a new user:
   *   1. Creates a Firebase identity (email + password).
   *   2. Retrieves a fresh Firebase ID token.
   *   3. Calls POST /auth/register with the requested role and full name.
   *
   * If the backend profile creation fails after Firebase creation succeeds,
   * the error is propagated so the caller can inform the user and offer retry.
   */
  register: async (
    email: string,
    password: string,
    fullName: string,
    requestedRole: PublicRegistrableRole,
  ): Promise<{ user: UserProfile; token: string }> => {
    await setPersistence(auth, browserSessionPersistence);
    const credential: UserCredential = await createUserWithEmailAndPassword(auth, email, password);
    const fbUser = credential.user;
    const token = await fbUser.getIdToken();

    try {
      const response = await apiClient.post<BackendUserDto>('/auth/register', {
        fullName,
        requestedRole,
      });
      return {
        user: {
          uid: response.data.uid,
          email: response.data.email,
          fullName: response.data.fullName,
          role: response.data.role,
        },
        token,
      };
    } catch (err) {
      // Firebase user was created but backend profile failed.
      // Sign out to leave the user in a clean state for retry.
      await firebaseSignOut(auth).catch(() => undefined);
      throw err;
    }
  },

  googleLogin: async (): Promise<{ user: UserProfile; token: string }> => {
    await setPersistence(auth, browserSessionPersistence);
    const credential = await signInWithPopup(auth, new GoogleAuthProvider());
    const token = await credential.user.getIdToken();

    const response = await apiClient.post<BackendUserDto>('/auth/session');
    return {
      user: {
        uid: response.data.uid,
        email: response.data.email,
        fullName: response.data.fullName,
        role: response.data.role,
      },
      token,
    };
  },

  /**
   * Signs in user using Firebase, retrieves the token, verifies it with the backend,
   * and returns the user's role/details from the database (not from local state).
   */
  login: async (email: string, password: string): Promise<{ user: UserProfile; token: string }> => {
    await setPersistence(auth, browserSessionPersistence);
    const credential: UserCredential = await signInWithEmailAndPassword(auth, email, password);
    const fbUser = credential.user;

    if (!fbUser) {
      throw new Error('Failed to retrieve user from Firebase Authentication.');
    }

    const token = await fbUser.getIdToken();

    // Role comes from the backend (PostgreSQL), not from client state.
    const response = await apiClient.post<BackendUserDto>('/auth/session');

    return {
      user: {
        uid: response.data.uid,
        email: response.data.email,
        fullName: response.data.fullName,
        role: response.data.role,
      },
      token,
    };
  },

  /**
   * Verifies the current Firebase session on reload.
   * Role is always reloaded from the backend.
   */
  verifySession: async (): Promise<{ user: UserProfile; token: string }> => {
    return new Promise((resolve, reject) => {
      const unsubscribe = auth.onAuthStateChanged(async (fbUser) => {
        unsubscribe();

        if (!fbUser) {
          return reject(new Error('No active session'));
        }

        try {
          const token = await fbUser.getIdToken();
          const response = await apiClient.post<BackendUserDto>('/auth/session');

          resolve({
            user: {
              uid: response.data.uid,
              email: response.data.email,
              fullName: response.data.fullName,
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
