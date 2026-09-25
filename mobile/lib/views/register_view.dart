import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../providers/auth_provider.dart';
import '../core/theme/app_tokens.dart';

class RegisterView extends ConsumerStatefulWidget {
  const RegisterView({super.key});

  @override
  ConsumerState<RegisterView> createState() => _RegisterViewState();
}

class _RegisterViewState extends ConsumerState<RegisterView> {
  bool _isLoading = false;
  
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  Future<void> _register() async {
    if (_passwordController.text != _confirmController.text) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
        content: Text('Passwords do not match!', style: TextStyle(color: Colors.white)),
        backgroundColor: AppTokens.red,
      ));
      return;
    }

    setState(() => _isLoading = true);
    
    final notifier = ref.read(authProvider.notifier);
    await notifier.register(
      _emailController.text.trim(),
      _passwordController.text.trim(),
      _nameController.text.trim(),
    );

    if (!mounted) return;
    
    final state = ref.read(authProvider);
    if (state.hasError && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(state.error.toString(), style: const TextStyle(color: Colors.white)),
          backgroundColor: AppTokens.red,
        ),
      );
      setState(() => _isLoading = false);
    } else if (mounted) {
      setState(() => _isLoading = false);
      context.go('/dashboard');
    }
  }

  Widget _buildTextField(String hint, TextEditingController controller, {bool isPassword = false, IconData? prefixIcon}) {
    return Container(
      decoration: BoxDecoration(
        color: AppTokens.bg,
        borderRadius: BorderRadius.circular(AppTokens.radiusField),
        border: Border.all(color: AppTokens.line),
      ),
      child: TextField(
        controller: controller,
        obscureText: isPassword,
        style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
        decoration: InputDecoration(
          hintText: hint,
          hintStyle: const TextStyle(color: AppTokens.inkMute, fontSize: 14.5),
          border: InputBorder.none,
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          prefixIcon: prefixIcon != null ? Icon(prefixIcon, color: AppTokens.inkMute, size: 20) : null,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
            child: Container(
              padding: const EdgeInsets.all(32),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(24),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x0A000000),
                    blurRadius: 20,
                    offset: Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    'Create your HousePlanner\naccount',
                    textAlign: TextAlign.left,
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF0F172A),
                      height: 1.2,
                    ),
                  ),
                  const SizedBox(height: 12),
                  const Text(
                    'Create a customer account to explore plans\nand manage your home design.',
                    textAlign: TextAlign.left,
                    style: TextStyle(
                      fontSize: 14.5,
                      color: Color(0xFF64748B),
                    ),
                  ),
                  const SizedBox(height: 32),

                  const Text('Full Name', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Color(0xFF334155))),
                  const SizedBox(height: 8),
                  _buildTextField('Kasun Perera', _nameController, prefixIcon: Icons.person_outline),
                  const SizedBox(height: 16),

                  const Text('Email Address', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Color(0xFF334155))),
                  const SizedBox(height: 8),
                  _buildTextField('name@example.com', _emailController, prefixIcon: Icons.mail_outline),
                  const SizedBox(height: 16),

                  const Text('Password', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Color(0xFF334155))),
                  const SizedBox(height: 8),
                  _buildTextField('At least 6 characters', _passwordController, isPassword: true, prefixIcon: Icons.lock_outline),
                  const SizedBox(height: 16),

                  const Text('Confirm Password', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Color(0xFF334155))),
                  const SizedBox(height: 8),
                  _buildTextField('••••••••', _confirmController, isPassword: true, prefixIcon: Icons.lock_outline),
                  const SizedBox(height: 32),

                  // Sign Up CTA
                  ElevatedButton(
                    onPressed: _isLoading ? null : _register,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF0F172A),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      elevation: 0,
                    ),
                    child: _isLoading 
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                      : const Row(
                          mainAxisSize: MainAxisSize.min,
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text('Create Account', style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold)),
                            SizedBox(width: 8),
                            Icon(Icons.arrow_forward, size: 18),
                          ],
                        ),
                  ),
                  const SizedBox(height: 24),

                  Row(
                    children: [
                      Expanded(child: Divider(color: Colors.grey.shade200, thickness: 1.5)),
                      const Padding(
                        padding: EdgeInsets.symmetric(horizontal: 16),
                        child: Text('OR', style: TextStyle(color: Color(0xFF94A3B8), fontSize: 12, fontWeight: FontWeight.w600)),
                      ),
                      Expanded(child: Divider(color: Colors.grey.shade200, thickness: 1.5)),
                    ],
                  ),
                  const SizedBox(height: 24),

                  // Google Auth
                  OutlinedButton.icon(
                    onPressed: _isLoading ? null : () async {
                      final notifier = ref.read(authProvider.notifier);
                      await notifier.loginWithGoogle();
                      
                      if (!context.mounted) return;
                      
                      final state = ref.read(authProvider);
                      if (state.hasError && context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(
                            content: Text(state.error.toString(), style: const TextStyle(color: Colors.white)),
                            backgroundColor: AppTokens.red,
                          ),
                        );
                      }
                    },
                    icon: Image.network('https://img.icons8.com/color/48/000000/google-logo.png', width: 22, height: 22),
                    label: const Text('Continue with Google', style: TextStyle(color: Color(0xFF1E293B), fontSize: 15, fontWeight: FontWeight.w600)),
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      side: BorderSide(color: Colors.grey.shade300),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      backgroundColor: Colors.white,
                    ),
                  ),
                  const SizedBox(height: 24),
                  
                  const Text(
                    'Architect and Constructor accounts are created by the\nHousePlanner administrator.',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: Color(0xFF94A3B8),
                      fontSize: 12,
                    ),
                  ),
                  const SizedBox(height: 24),

                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text('Already have an account? ', style: TextStyle(color: Color(0xFF64748B), fontSize: 13.5)),
                      GestureDetector(
                        onTap: () => context.go('/login'),
                        child: const Text('Sign in', style: TextStyle(color: Color(0xFF2563EB), fontWeight: FontWeight.w700, fontSize: 13.5)),
                      )
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}