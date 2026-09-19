import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/auth_provider.dart';
import '../core/theme/app_tokens.dart';

class DashboardView extends ConsumerWidget {
  const DashboardView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final email = authState.value?.email ?? 'User';
    final name = email.split('@').first;
    final displayName = name[0].toUpperCase() + name.substring(1);

    return Scaffold(
      backgroundColor: AppTokens.bg,
      body: Stack(
        children: [
          // Soft Purple Blob Background
          Positioned(
            top: 50,
            right: -80,
            child: ImageFiltered(
              imageFilter: ImageFilter.blur(sigmaX: 50, sigmaY: 50),
              child: Container(
                width: 280,
                height: 280,
                decoration: const BoxDecoration(
                  shape: BoxShape.circle,
                  color: Color(0x287C71F2),
                ),
              ),
            ),
          ),
          
          SafeArea(
            child: Column(
              children: [
                // Header
                Padding(
                  padding: const EdgeInsets.fromLTRB(24, 24, 24, 16),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Good afternoon', style: TextStyle(color: AppTokens.inkSoft, fontSize: 13, fontWeight: FontWeight.w500)),
                          Text(displayName, style: const TextStyle(color: AppTokens.ink, fontSize: 23, fontWeight: FontWeight.w800)),
                        ],
                      ),
                      CircleAvatar(
                        radius: 20,
                        backgroundColor: AppTokens.accentSoft,
                        child: Text(
                          displayName[0],
                          style: const TextStyle(color: AppTokens.accent, fontWeight: FontWeight.bold, fontSize: 16),
                        ),
                      ),
                    ],
                  ),
                ),

                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
                    child: Column(
                      children: [
                        // Hero Glass Card
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
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Container(
                                    padding: const EdgeInsets.all(12),
                                    decoration: BoxDecoration(
                                      color: AppTokens.accent,
                                      borderRadius: BorderRadius.circular(16),
                                    ),
                                    child: const Icon(Icons.star, color: Colors.white, size: 24),
                                  ),
                                  const SizedBox(height: 24),
                                  const Text(
                                    'Ready to design something extraordinary?',
                                    style: TextStyle(
                                      fontSize: 22,
                                      fontWeight: FontWeight.w800,
                                      color: AppTokens.ink,
                                      height: 1.2,
                                    ),
                                  ),
                                  const SizedBox(height: 12),
                                  const Text(
                                    'Describe your land and preferences — the AI Architect handles the rest.',
                                    style: TextStyle(
                                      fontSize: 14.5,
                                      color: AppTokens.inkSoft,
                                      height: 1.4,
                                    ),
                                  ),
                                  const SizedBox(height: 32),
                                  
                                  // Action Buttons
                                  ElevatedButton(
                                    onPressed: () => context.go('/intake'),
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: AppTokens.ink,
                                      foregroundColor: Colors.white,
                                      padding: const EdgeInsets.symmetric(vertical: 16),
                                      shape: RoundedRectangleBorder(
                                        borderRadius: BorderRadius.circular(AppTokens.radiusButton),
                                      ),
                                      minimumSize: const Size(double.infinity, 0),
                                      elevation: 0,
                                    ),
                                    child: const Row(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: [
                                        Icon(Icons.add, size: 18),
                                        SizedBox(width: 8),
                                        Text('Start New Project', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                                      ],
                                    ),
                                  ),
                                  const SizedBox(height: 12),
                                  OutlinedButton(
                                    onPressed: () => context.go('/plans'),
                                    style: OutlinedButton.styleFrom(
                                      foregroundColor: AppTokens.ink,
                                      backgroundColor: Colors.white,
                                      side: const BorderSide(color: AppTokens.line, width: 1.4),
                                      padding: const EdgeInsets.symmetric(vertical: 16),
                                      shape: RoundedRectangleBorder(
                                        borderRadius: BorderRadius.circular(AppTokens.radiusButton),
                                      ),
                                      minimumSize: const Size(double.infinity, 0),
                                    ),
                                    child: const Text('Browse Plan Library', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(height: 24),

                        // Stats Row
                        Row(
                          children: [
                            Expanded(
                              child: _buildStatCard('3', 'Active workflows'),
                            ),
                            const SizedBox(width: 16),
                            Expanded(
                              child: _buildStatCard('2', 'Awaiting approval'),
                            ),
                          ],
                        ),
                        const SizedBox(height: 100), // padding for bottom nav
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStatCard(String value, String label) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 16),
      decoration: BoxDecoration(
        color: AppTokens.card,
        borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
        border: Border.all(color: AppTokens.line),
        boxShadow: const [
          BoxShadow(
            color: Color(0x380B0B14),
            blurRadius: 26,
            offset: Offset(0, 10),
            spreadRadius: -14,
          )
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            value,
            style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800, color: AppTokens.ink),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: const TextStyle(fontSize: 11.5, color: AppTokens.inkMute, fontWeight: FontWeight.w500),
          ),
        ],
      ),
    );
  }
}
