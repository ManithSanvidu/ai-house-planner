import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/auth_provider.dart';

class LoginView extends ConsumerStatefulWidget {
  const LoginView({super.key});

  @override
  ConsumerState<LoginView> createState() => _LoginViewState();
}

class _LoginViewState extends ConsumerState<LoginView> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  void _handleLogin() async {
    final notifier = ref.read(authProvider.notifier);
    await notifier.login(
      _emailController.text, 
      _passwordController.text
    );
    
    if (!mounted) return;
    
    // Check if the state is an error after login attempt
    final state = ref.read(authProvider);
    if (state.hasError && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(state.error.toString(), style: const TextStyle(color: Colors.white)),
          backgroundColor: Colors.red,
        ),
      );
    } else if (state.valueOrNull != null && mounted) {
      context.go('/dashboard');
    }
  }

  Widget _buildTextField(String hint, TextEditingController controller, {bool isPassword = false}) {
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: const Color(0xFFF6F2F4),
        borderRadius: BorderRadius.circular(24),
      ),
      child: TextField(
        controller: controller,
        obscureText: isPassword,
        style: const TextStyle(fontSize: 18),
        decoration: InputDecoration(
          hintText: hint,
          hintStyle: const TextStyle(color: Color(0xFF78767D)),
          border: InputBorder.none,
          contentPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authProvider);

    return Scaffold(
      backgroundColor: const Color(0xFFE5E1E3),
      body: SafeArea(
        child: Column(
          children: [
            // Header
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 20, 24),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back, color: Color(0xFF00000B)),
                    onPressed: () => context.go('/'),
                    style: IconButton.styleFrom(backgroundColor: Colors.white, elevation: 1),
                  ),
                  const SizedBox(width: 8),
                  const Expanded(
                    child: Text(
                      'Welcome\nBack',
                      style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, height: 1.2, color: Color(0xFF00000B)),
                    ),
                  ),
                ],
              ),
            ),
            
            Expanded(
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
                decoration: const BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.only(topLeft: Radius.circular(32), topRight: Radius.circular(32)),
                ),
                child: ListView(
                  physics: const BouncingScrollPhysics(),
                  children: [
                    const SizedBox(height: 24),

                    // Inputs
                    _buildTextField('Email Address', _emailController),
                    _buildTextField('Password', _passwordController, isPassword: true),
                    
                    // Forgot Password
                    Align(
                      alignment: Alignment.centerRight,
                      child: GestureDetector(
                        onTap: () => context.go('/register'),
                        child: const Text('Forgot Password?', style: TextStyle(color: Color(0xFF00000B), fontWeight: FontWeight.bold)),
                      ),
                    ),

                    const SizedBox(height: 48),

                    // Submit
                    SizedBox(
                      height: 64,
                      child: ElevatedButton(
                        onPressed: authState.isLoading ? null : _handleLogin,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF1A1A2E),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
                          elevation: 8,
                          shadowColor: const Color(0xFF1A1A2E).withOpacity(0.3),
                        ),
                        child: authState.isLoading 
                          ? const SizedBox(height: 24, width: 24, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                          : const Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Text('Log In', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: Colors.white)),
                                SizedBox(width: 8),
                                Icon(Icons.arrow_forward, color: Colors.white),
                              ],
                            ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Text('Don\'t have an account? ', style: TextStyle(color: Color(0xFF78767D))),
                        GestureDetector(
                          onTap: () => context.go('/register'),
                          child: const Text('Register', style: TextStyle(color: Color(0xFF00000B), fontWeight: FontWeight.bold)),
                        )
                      ],
                    ),
                    const SizedBox(height: 32),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
