import { createSlice, createAsyncThunk, type PayloadAction } from '@reduxjs/toolkit';
import type { AuthState, UserProfile } from '../../types/auth.types';
import authService from './authService';

const initialState: AuthState = {
  user: null,
  token: null,
  status: 'idle',
  error: null,
};

// Async Thunk for User Registration
export const registerAsync = createAsyncThunk(
  'auth/register',
  async (
    {
      email,
      password,
      fullName,
    }: { email: string; password: string; fullName: string },
    { rejectWithValue },
  ) => {
    try {
      return await authService.register(email, password, fullName);
    } catch (error: any) {
      let message = 'Registration failed. Please try again.';
      if (error.code) {
        switch (error.code) {
          case 'auth/email-already-in-use':
            message = 'An account with this email already exists.';
            break;
          case 'auth/invalid-email':
            message = 'Invalid email address format.';
            break;
          case 'auth/weak-password':
            message = 'Password is too weak. Use at least 6 characters.';
            break;
          case 'auth/network-request-failed':
            message = 'Network error. Please check your internet connection.';
            break;
          default:
            message = error.message || message;
        }
      } else if (error.response?.data?.error) {
        message = error.response.data.error;
      } else if (error.message) {
        message = error.message;
      }
      return rejectWithValue(message);
    }
  },
);

// Async Thunk for User Login
export const loginAsync = createAsyncThunk(
  'auth/login',
  async ({ email, password }: { email: string; password: string }, { rejectWithValue }) => {
    try {
      return await authService.login(email, password);
    } catch (error: any) {
      console.log('DEBUG [Login Error Details]:', error);
      let message = 'An error occurred during authentication.';
      if (error.code) {
        switch (error.code) {
          case 'auth/invalid-email':
            message = 'Invalid email address format.';
            break;
          case 'auth/user-disabled':
            message = 'This user account has been disabled.';
            break;
          case 'auth/user-not-found':
          case 'auth/invalid-credential':
            message = 'Incorrect email or password.';
            break;
          case 'auth/wrong-password':
            message = 'Incorrect password. Please try again.';
            break;
          case 'auth/network-request-failed':
            message = 'Network error. Please check your internet connection.';
            break;
          default:
            message = error.message || message;
        }
      } else if (error.response?.data?.error) {
        message = error.response.data.error;
      } else if (error.message) {
        message = error.message;
      }
      return rejectWithValue(message);
    }
  },
);

export const googleLoginAsync = createAsyncThunk('auth/googleLogin', async (_, { rejectWithValue }) => {
  try {
    return await authService.loginWithGoogle();
  } catch (error: any) {
    return rejectWithValue(error.message || 'Google sign-in failed.');
  }
});

// Async Thunk for User Logout
export const logoutAsync = createAsyncThunk('auth/logout', async (_, { rejectWithValue }) => {
  try {
    await authService.logout();
  } catch (error: any) {
    return rejectWithValue(error.message || 'Logout failed.');
  }
});

// Async Thunk for Initial Session Verification
export const verifySessionAsync = createAsyncThunk(
  'auth/verifySession',
  async (_, { rejectWithValue }) => {
    try {
      return await authService.initializeSession();
    } catch (error: any) {
      return rejectWithValue(error.message || 'Session verification failed');
    }
  },
);

type AuthPayload = { user: UserProfile; token: string };

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    clearAuth: (state) => {
      state.user = null;
      state.token = null;
      state.status = 'idle';
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Registration
      .addCase(registerAsync.pending, (state) => {
        state.status = 'loading';
        state.error = null;
      })
      .addCase(registerAsync.fulfilled, (state, action: PayloadAction<AuthPayload>) => {
        state.status = 'succeeded';
        state.user = action.payload.user;
        state.token = action.payload.token;
        state.error = null;
      })
      .addCase(registerAsync.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.payload as string;
      })
      // Login flows
      .addCase(loginAsync.pending, (state) => {
        state.status = 'loading';
        state.error = null;
      })
      .addCase(loginAsync.fulfilled, (state, action: PayloadAction<AuthPayload>) => {
        state.status = 'succeeded';
        state.user = action.payload.user;
        state.token = action.payload.token;
        state.error = null;
      })
      .addCase(loginAsync.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.payload as string;
      })
      .addCase(googleLoginAsync.pending, (state) => {
        state.status = 'loading';
        state.error = null;
      })
      .addCase(googleLoginAsync.fulfilled, (state, action: PayloadAction<AuthPayload>) => {
        state.status = 'succeeded';
        state.user = action.payload.user;
        state.token = action.payload.token;
      })
      .addCase(googleLoginAsync.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.payload as string;
      })
      // Logout flows
      .addCase(logoutAsync.fulfilled, (state) => {
        state.user = null;
        state.token = null;
        state.status = 'idle';
        state.error = null;
      })
      // Session Verification
      .addCase(verifySessionAsync.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(verifySessionAsync.fulfilled, (state, action: PayloadAction<AuthPayload>) => {
        state.status = 'succeeded';
        state.user = action.payload.user;
        state.token = action.payload.token;
        state.error = null;
      })
      .addCase(verifySessionAsync.rejected, (state) => {
        state.status = 'failed';
        // A failed session verify just means the user isn't logged in — not an error to surface.
        state.user = null;
        state.token = null;
      });
  },
});

export const { clearAuth } = authSlice.actions;
export default authSlice.reducer;
