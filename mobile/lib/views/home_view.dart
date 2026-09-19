import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../core/theme/app_tokens.dart';

class HomeView extends StatelessWidget {
  const HomeView({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTokens.bg,
      body: Stack(
        children: [
          // Background blobs for visual interest
          Positioned(
            top: -100,
            right: -100,
            child: ImageFiltered(
              imageFilter: ImageFilter.blur(sigmaX: 80, sigmaY: 80),
              child: Container(
                width: 300,
                height: 300,
                decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0x337C71F2)),
              ),
            ),
          ),
          Positioned(
            bottom: -50,
            left: -100,
            child: ImageFiltered(
              imageFilter: ImageFilter.blur(sigmaX: 80, sigmaY: 80),
              child: Container(
                width: 300,
                height: 300,
                decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0x2210B981)),
              ),
            ),
          ),
          
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 40.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Row(
                    children: [
                      Icon(Icons.architecture, color: AppTokens.ink, size: 28),
                      SizedBox(width: 12),
                      Text('HOMEPLANNERAI', style: TextStyle(fontWeight: FontWeight.w900, fontSize: 16, letterSpacing: 1.5, color: AppTokens.ink)),
                    ],
                  ),
                  const Spacer(),
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
                          boxShadow: const [BoxShadow(color: Color(0x380B0B14), blurRadius: 50, offset: Offset(0, 20))],
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Design your\ndream home\nwith AI.',
                              style: TextStyle(fontSize: 36, fontWeight: FontWeight.w900, color: AppTokens.ink, height: 1.1),
                            ),
                            const SizedBox(height: 16),
                            const Text(
                              'Generate architectural plans and construction timelines tailored to your plot in minutes.',
                              style: TextStyle(fontSize: 15, color: AppTokens.inkSoft, height: 1.5),
                            ),
                            const SizedBox(height: 32),
                            ElevatedButton(
                              onPressed: () => context.go('/login'),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: AppTokens.ink,
                                foregroundColor: Colors.white,
                                padding: const EdgeInsets.symmetric(vertical: 18),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
                                minimumSize: const Size(double.infinity, 0),
                                elevation: 0,
                              ),
                              child: const Text('Get Started', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 32),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
