import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/network/api_client.dart';
import '../providers/auth_provider.dart';
import '../core/theme/app_tokens.dart';

class RegisterView extends ConsumerStatefulWidget {
  const RegisterView({super.key});

  @override
  ConsumerState<RegisterView> createState() => _RegisterViewState();
}

class _RegisterViewState extends ConsumerState<RegisterView> {
  int _selectedRoleIndex = 0;
  bool _agreedToTerms = false;
  bool _isLoading = false;
  
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _passwordController.addListener(_updatePasswordStrength);
  }

  @override
  void dispose() {
    _passwordController.removeListener(_updatePasswordStrength);
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  void _updatePasswordStrength() {
    setState(() {});
  }

  int _calculatePasswordStrength(String password) {
    if (password.isEmpty) return 0;
    int strength = 0;
    if (password.length >= 6) strength += 1;
    if (password.length >= 8) strength += 1;
    if (RegExp(r'[A-Z]').hasMatch(password) && RegExp(r'[a-z]').hasMatch(password)) strength += 1;
    if (RegExp(r'[0-9]').hasMatch(password) || RegExp(r'[^A-Za-z0-9]').hasMatch(password)) strength += 1;
    return strength.clamp(1, 4);
  }

  String _getPasswordStrengthText(int strength) {
    switch (strength) {
      case 0: return 'Enter a password';
      case 1: return 'Very Weak';
      case 2: return 'Weak';
      case 3: return 'Medium Strength';
      case 4: return 'Strong Password';
      default: return '';
    }
  }

  Future<void> _register() async {
    if (!_agreedToTerms) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
        content: Text('Please agree to the Terms of Service.', style: TextStyle(color: Colors.white)),
        backgroundColor: AppTokens.red,
      ));
      return;
    }
    if (_passwordController.text != _confirmController.text) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
        content: Text('Passwords do not match!', style: TextStyle(color: Colors.white)),
        backgroundColor: AppTokens.red,
      ));
      return;
    }

    setState(() => _isLoading = true);
    try {
      final response = await ApiClient.instance.post(
        '/auth/local/register',
        data: {
          'email': _emailController.text.trim(),
          'password': _passwordController.text.trim(),
          'fullName': _nameController.text.trim(),
          'roleId': _selectedRoleIndex + 1,
        },
      );

      if (response.statusCode == 200 && mounted) {
        await ref.read(authProvider.notifier).login(
          _emailController.text.trim(),
          _passwordController.text.trim()
        );
        if (mounted) context.go('/dashboard');
      }
    } on DioException catch (e) {
      if (mounted) {
        final message = e.response?.data?.toString() ?? e.message ?? 'Registration failed';
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $message', style: const TextStyle(color: Colors.white)), backgroundColor: AppTokens.red),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: ${e.toString()}', style: const TextStyle(color: Colors.white)), backgroundColor: AppTokens.red),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Widget _buildRoleButton(String title, int index) {
    final isSelected = _selectedRoleIndex == index;
    return Expanded(
      child: GestureDetector(
        onTap: () => setState(() => _selectedRoleIndex = index),
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 12),
          decoration: BoxDecoration(
            color: isSelected ? AppTokens.ink : Colors.transparent,
            borderRadius: BorderRadius.circular(AppTokens.radiusButton),
          ),
          alignment: Alignment.center,
          child: Text(
            title,
            style: TextStyle(
              color: isSelected ? Colors.white : AppTokens.inkMute,
              fontWeight: FontWeight.w600,
              fontSize: 12.5,
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTextField(String hint, TextEditingController controller, {bool isPassword = false}) {
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
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
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final passwordStrength = _calculatePasswordStrength(_passwordController.text);
    final strengthText = _getPasswordStrengthText(passwordStrength);

    return Scaffold(
      backgroundColor: AppTokens.bg,
      body: Stack(
        children: [
          // Decorative Blobs matching login_view
          Positioned(
            top: -50,
            left: -50,
            child: ImageFiltered(
              imageFilter: ImageFilter.blur(sigmaX: 50, sigmaY: 50),
              child: Container(
                width: 220,
                height: 220,
                decoration: const BoxDecoration(
                  shape: BoxShape.circle,
                  color: Color(0x337C71F2),
                ),
              ),
            ),
          ),
          
          SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    // Logo Header
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: AppTokens.ink,
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: const Icon(Icons.home, color: Colors.white, size: 24),
                        ),
                        const SizedBox(width: 12),
                        const Text(
                          'HOMEPLANNER AI',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.2,
                            color: AppTokens.ink,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 32),

                    // Glass Card Form
                    ClipRRect(
                      borderRadius: BorderRadius.circular(AppTokens.radiusCardGlass),
                      child: BackdropFilter(
                        filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
                        child: Container(
                          padding: const EdgeInsets.all(32),
                          decoration: BoxDecoration(
                            color: AppTokens.cardGlass,
                            borderRadius: BorderRadius.circular(AppTokens.radiusCardGlass),
                            border: Border.all(color: Colors.white.withValues(alpha: 0.7)),
                            boxShadow: const [
                              BoxShadow(
                                color: Color(0x380B0B14),
                                blurRadius: 50,
                                offset: Offset(0, 20),
                              ),
                            ],
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              const Text(
                                'Create Account',
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 23,
                                  fontWeight: FontWeight.w800,
                                  color: AppTokens.ink,
                                ),
                              ),
                              const SizedBox(height: 24),
                              
                              // Segmented Control
                              Container(
                                padding: const EdgeInsets.all(4),
                                margin: const EdgeInsets.only(bottom: 24),
                                decoration: BoxDecoration(
                                  color: AppTokens.bg,
                                  borderRadius: BorderRadius.circular(AppTokens.radiusButton + 4),
                                  border: Border.all(color: AppTokens.line),
                                ),
                                child: Row(
                                  children: [
                                    _buildRoleButton('Client', 0),
                                    _buildRoleButton('Architect', 1),
                                    _buildRoleButton('Contractor', 2),
                                  ],
                                ),
                              ),

                              const Text('Full Name', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.ink)),
                              const SizedBox(height: 8),
                              _buildTextField('John Doe', _nameController),

                              const Text('Email Address', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.ink)),
                              const SizedBox(height: 8),
                              _buildTextField('name@example.com', _emailController),

                              const Text('Password', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.ink)),
                              const SizedBox(height: 8),
                              _buildTextField('••••••••', _passwordController, isPassword: true),
                              
                              // Strength Indicator
                              Padding(
                                padding: const EdgeInsets.only(bottom: 16),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      children: List.generate(4, (index) {
                                        final isActive = index < passwordStrength;
                                        return Expanded(
                                          child: Container(
                                            height: 4,
                                            margin: EdgeInsets.only(right: index < 3 ? 4 : 0),
                                            decoration: BoxDecoration(
                                              color: isActive ? AppTokens.accent : AppTokens.line,
                                              borderRadius: BorderRadius.circular(2),
                                            ),
                                          ),
                                        );
                                      }),
                                    ),
                                    const SizedBox(height: 6),
                                    Text(strengthText, style: const TextStyle(fontSize: 11, color: AppTokens.inkMute, fontWeight: FontWeight.w600)),
                                  ],
                                ),
                              ),

                              const Text('Confirm Password', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.ink)),
                              const SizedBox(height: 8),
                              _buildTextField('••••••••', _confirmController, isPassword: true),

                              // Terms
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  SizedBox(
                                    width: 24,
                                    height: 24,
                                    child: Checkbox(
                                      value: _agreedToTerms,
                                      activeColor: AppTokens.accent,
                                      onChanged: (val) => setState(() => _agreedToTerms = val ?? false),
                                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
                                      side: const BorderSide(color: AppTokens.line),
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  const Expanded(
                                    child: Text.rich(
                                      TextSpan(
                                        text: 'I agree to the ',
                                        style: TextStyle(color: AppTokens.inkSoft, fontSize: 12.5),
                                        children: [
                                          TextSpan(text: 'Terms of Service', style: TextStyle(color: AppTokens.accent, fontWeight: FontWeight.w600)),
                                          TextSpan(text: ' and acknowledge the '),
                                          TextSpan(text: 'Privacy Policy', style: TextStyle(color: AppTokens.accent, fontWeight: FontWeight.w600)),
                                          TextSpan(text: '.'),
                                        ],
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 32),

                              // Sign Up CTA
                              ElevatedButton(
                                onPressed: _isLoading ? null : _register,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: AppTokens.ink,
                                  foregroundColor: Colors.white,
                                  padding: const EdgeInsets.symmetric(vertical: 16),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(AppTokens.radiusButton),
                                  ),
                                  elevation: 0,
                                ),
                                child: _isLoading 
                                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                                  : const Row(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: [
                                        Text('Sign Up', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                                        SizedBox(width: 8),
                                        Icon(Icons.arrow_forward, size: 16),
                                      ],
                                    ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: 32),
                    
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Text('Already have an account? ', style: TextStyle(color: AppTokens.inkMute, fontSize: 12.5)),
                        GestureDetector(
                          onTap: () => context.go('/login'),
                          child: const Text('Sign in', style: TextStyle(color: AppTokens.accent, fontWeight: FontWeight.w600, fontSize: 12.5)),
                        )
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}