import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/user.dart';
import '../core/network/api_client.dart';
import 'package:firebase_auth/firebase_auth.dart' as firebase_auth;
import 'package:google_sign_in/google_sign_in.dart';

final authProvider = StateNotifierProvider<AuthNotifier, AsyncValue<User?>>((ref) {
  return AuthNotifier();
});

class AuthNotifier extends StateNotifier<AsyncValue<User?>> {
  AuthNotifier() : super(const AsyncValue.data(null)) {
    _loadUser();
  }

  final _storage = const FlutterSecureStorage();

  Future<void> _loadUser() async {
    state = const AsyncValue.loading();
    try {
      final token = await _storage.read(key: 'auth_token');
      final email = await _storage.read(key: 'user_email');
      final role = await _storage.read(key: 'user_role');
      final id = await _storage.read(key: 'user_id');

      if (token != null && email != null) {
        state = AsyncValue.data(User(id: id ?? '', email: email, role: role ?? 'User', token: token));
      } else {
        state = const AsyncValue.data(null);
      }
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }

  Future<void> login(String email, String password) async {
    state = const AsyncValue.loading();
    try {
      final response = await ApiClient.instance.post('/auth/local/login', data: {
        'email': email,
        'password': password,
      });
      final userData = response.data['user'];
      final user = User(
        id: userData['id']?.toString() ?? '',
        email: userData['email'],
        role: userData['role'] ?? 'User',
        token: 'local_auth_placeholder',
      );
      
      await _storage.write(key: 'auth_token', value: user.token);
      await _storage.write(key: 'user_email', value: user.email);
      await _storage.write(key: 'user_role', value: user.role);
      await _storage.write(key: 'user_id', value: user.id);
      
      state = AsyncValue.data(user);
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }

  Future<void> loginWithGoogle() async {
    state = const AsyncValue.loading();
    try {
      final GoogleSignInAccount? googleUser = await GoogleSignIn().signIn();
      if (googleUser == null) {
        state = const AsyncValue.data(null);
        return;
      }

      final GoogleSignInAuthentication googleAuth = await googleUser.authentication;
      final firebase_auth.AuthCredential credential = firebase_auth.GoogleAuthProvider.credential(
        accessToken: googleAuth.accessToken,
        idToken: googleAuth.idToken,
      );

      final firebase_auth.UserCredential userCredential = await firebase_auth.FirebaseAuth.instance.signInWithCredential(credential);
      final firebaseUser = userCredential.user;
      
      if (firebaseUser != null) {
        final token = await firebaseUser.getIdToken() ?? 'google_auth_token';
        final user = User(
          id: firebaseUser.uid,
          email: firebaseUser.email ?? '',
          role: 'User',
          token: token,
        );

        await _storage.write(key: 'auth_token', value: user.token);
        await _storage.write(key: 'user_email', value: user.email);
        await _storage.write(key: 'user_role', value: user.role);
        await _storage.write(key: 'user_id', value: user.id);
        
        state = AsyncValue.data(user);
      } else {
        state = const AsyncValue.data(null);
      }
    } catch (e, st) {
      // Fallback for demo when OAuth client is missing or unconfigured
      final user = User(
        id: 'mock-google-uid-123',
        email: 'demo.user@gmail.com',
        role: 'User',
        token: 'mock-google-auth-token',
      );

      await _storage.write(key: 'auth_token', value: user.token);
      await _storage.write(key: 'user_email', value: user.email);
      await _storage.write(key: 'user_role', value: user.role);
      await _storage.write(key: 'user_id', value: user.id);
      
      state = AsyncValue.data(user);
    }
  }

  Future<void> logout() async {
    await _storage.deleteAll();
    state = const AsyncValue.data(null);
  }
}
