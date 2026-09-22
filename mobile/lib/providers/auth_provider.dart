import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:supabase_flutter/supabase_flutter.dart' as supabase;
import '../models/user.dart';
import '../core/network/api_client.dart';

final authProvider = StateNotifierProvider<AuthNotifier, AsyncValue<User?>>((ref) {
  return AuthNotifier();
});

class AuthNotifier extends StateNotifier<AsyncValue<User?>> {
  AuthNotifier() : super(const AsyncValue.data(null)) {
    _loadUser();
  }

  final _storage = const FlutterSecureStorage();
  final _supabaseAuth = supabase.Supabase.instance.client.auth;

  Future<void> _loadUser() async {
    state = const AsyncValue.loading();
    try {
      final session = _supabaseAuth.currentSession;
      if (session != null) {
        // We have a session, let's refresh user info from backend
        final token = session.accessToken;
        await _storage.write(key: 'auth_token', value: token);
        
        final response = await ApiClient.instance.get('/auth/me');
        final userData = response.data;
        
        final user = User(
          id: userData['uid'] ?? session.user.id,
          email: userData['email'] ?? session.user.email ?? '',
          role: userData['role'] ?? 'Customer',
          token: token,
        );
        state = AsyncValue.data(user);
      } else {
        state = const AsyncValue.data(null);
      }
    } catch (e, st) {
      // If backend fails, we can fallback to supabase session or log out
      state = const AsyncValue.data(null);
    }
  }

  Future<void> login(String email, String password) async {
    state = const AsyncValue.loading();
    try {
      final response = await _supabaseAuth.signInWithPassword(
        email: email,
        password: password,
      );
      
      final session = response.session;
      if (session == null) throw Exception('Session missing after sign in');
      
      await _storage.write(key: 'auth_token', value: session.accessToken);
      
      // Notify backend to create session context
      final backendResponse = await ApiClient.instance.post('/auth/session');
      final userData = backendResponse.data;

      final user = User(
        id: userData['uid'] ?? session.user.id,
        email: userData['email'] ?? session.user.email ?? '',
        role: userData['role'] ?? 'Customer',
        token: session.accessToken,
      );
      
      state = AsyncValue.data(user);
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }
  
  Future<void> register(String email, String password, String fullName) async {
    state = const AsyncValue.loading();
    try {
      final response = await _supabaseAuth.signUp(
        email: email,
        password: password,
      );
      
      final session = response.session;
      if (session == null) throw Exception('Session missing after sign up. Please confirm email if required.');
      
      await _storage.write(key: 'auth_token', value: session.accessToken);
      
      // Sync user to backend database
      final backendResponse = await ApiClient.instance.post(
        '/auth/register',
        data: {
          'fullName': fullName,
        }
      );
      
      final userData = backendResponse.data;
      final user = User(
        id: userData['uid'] ?? session.user.id,
        email: userData['email'] ?? session.user.email ?? '',
        role: userData['role'] ?? 'Customer',
        token: session.accessToken,
      );
      
      state = AsyncValue.data(user);
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }

  Future<void> loginWithGoogle() async {
    // Note: Proper Google Sign In with Supabase on mobile requires setup of 
    // deep links and Google OAuth client IDs. This is a placeholder that 
    // matches the web app's current throwing behavior.
    state = AsyncValue.error(Exception("Google OAuth not implemented for Supabase yet."), StackTrace.current);
  }

  Future<void> logout() async {
    await _storage.deleteAll();
    await _supabaseAuth.signOut();
    state = const AsyncValue.data(null);
  }
}
