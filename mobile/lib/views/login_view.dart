import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/auth_provider.dart';
import '../core/theme/app_tokens.dart';

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
    
    final state = ref.read(authProvider);
    if (state.hasError && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(state.error.toString(), style: const TextStyle(color: Colors.white)),
          backgroundColor: AppTokens.red,
        ),
      );
    }
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
    final authState = ref.watch(authProvider);

    return Scaffold(
      backgroundColor: AppTokens.bg,
      body: Stack(
        children: [
          // Decorative Blobs
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
                padding: const EdgeInsets.symmetric(horizontal: 24),
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
                    const SizedBox(height: 48),

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
                            border: Border.all(color: Colors.white.withOpacity(0.7)),
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
                                'Welcome back',
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 23,
                                  fontWeight: FontWeight.w800,
                                  color: AppTokens.ink,
                                ),
                              ),
                              const SizedBox(height: 8),
                              const Text(
                                'Sign in to access your projects or admin portal.',
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 14.5,
                                  color: AppTokens.inkSoft,
                                ),
                              ),
                              const SizedBox(height: 32),

                              const Text('Email address', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.ink)),
                              const SizedBox(height: 8),
                              _buildTextField('name@example.com', _emailController),

                              const SizedBox(height: 8),
                              const Text('Password', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.ink)),
                              const SizedBox(height: 8),
                              _buildTextField('••••••••', _passwordController, isPassword: true),

                              Align(
                                alignment: Alignment.centerRight,
                                child: TextButton(
                                  onPressed: () {}, 
                                  style: TextButton.styleFrom(
                                    foregroundColor: AppTokens.accent,
                                    padding: EdgeInsets.zero,
                                    minimumSize: Size.zero,
                                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                  ),
                                  child: const Text('Forgot password?', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 12.5)),
                                ),
                              ),
                              const SizedBox(height: 32),

                              // Sign In CTA
                              ElevatedButton(
                                onPressed: authState.isLoading ? null : _handleLogin,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: AppTokens.ink,
                                  foregroundColor: Colors.white,
                                  padding: const EdgeInsets.symmetric(vertical: 16),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(AppTokens.radiusButton),
                                  ),
                                  elevation: 0,
                                ),
                                child: authState.isLoading 
                                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                                  : const Row(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: [
                                        Text('Sign In', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                                        SizedBox(width: 8),
                                        Icon(Icons.arrow_forward, size: 16),
                                      ],
                                    ),
                              ),
                              const SizedBox(height: 24),

                              const Row(
                                children: [
                                  Expanded(child: Divider(color: AppTokens.line)),
                                  Padding(
                                    padding: EdgeInsets.symmetric(horizontal: 16),
                                    child: Text('OR', style: TextStyle(color: AppTokens.inkMute, fontSize: 11, fontWeight: FontWeight.w600)),
                                  ),
                                  Expanded(child: Divider(color: AppTokens.line)),
                                ],
                              ),
                              const SizedBox(height: 24),

                              // Google Auth
                              OutlinedButton.icon(
                                onPressed: () {},
                                icon: const Icon(Icons.g_mobiledata, size: 24),
                                label: const Text('Continue with Google', style: TextStyle(color: AppTokens.ink, fontSize: 14.5, fontWeight: FontWeight.w600)),
                                style: OutlinedButton.styleFrom(
                                  padding: const EdgeInsets.symmetric(vertical: 16),
                                  side: const BorderSide(color: AppTokens.line),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(AppTokens.radiusButton),
                                  ),
                                  backgroundColor: Colors.white,
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
                        const Text('No account? ', style: TextStyle(color: AppTokens.inkMute, fontSize: 12.5)),
                        GestureDetector(
                          onTap: () {},
                          child: const Text('Contact administrator', style: TextStyle(color: AppTokens.accent, fontWeight: FontWeight.w600, fontSize: 12.5)),
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
