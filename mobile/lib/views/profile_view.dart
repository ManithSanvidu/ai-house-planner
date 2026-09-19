import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/auth_provider.dart';
import '../core/theme/app_tokens.dart';

class ProfileView extends ConsumerWidget {
  const ProfileView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final email = authState.value?.email ?? 'User';
    final name = email.split('@').first;
    final displayName = name[0].toUpperCase() + name.substring(1);

    return Scaffold(
      backgroundColor: AppTokens.bg,
      appBar: AppBar(
        backgroundColor: AppTokens.bg,
        elevation: 0,
        title: const Text('Profile', style: TextStyle(fontWeight: FontWeight.w800, color: AppTokens.ink)),
        centerTitle: false,
        actions: [
          IconButton(
            icon: const Icon(Icons.logout, color: AppTokens.ink),
            onPressed: () => ref.read(authProvider.notifier).logout(),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
        child: Column(
          children: [
            // User Card
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                border: Border.all(color: AppTokens.line),
                boxShadow: const [BoxShadow(color: Color(0x140B0B14), blurRadius: 16, offset: Offset(0, 8), spreadRadius: -8)],
              ),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 32,
                    backgroundColor: AppTokens.accentSoft,
                    child: Text(
                      displayName[0],
                      style: const TextStyle(color: AppTokens.accent, fontWeight: FontWeight.bold, fontSize: 28),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(displayName, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 20, color: AppTokens.ink)),
                      Text(email, style: const TextStyle(color: AppTokens.inkSoft, fontSize: 14)),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 32),
            
            // Activities Section
            const Align(
              alignment: Alignment.centerLeft,
              child: Text('Recent Activities', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16, color: AppTokens.ink)),
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                border: Border.all(color: AppTokens.line),
              ),
              child: const Column(
                children: [
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(Icons.architecture, color: AppTokens.accent),
                    title: Text('Viewed "Modern Oasis" plan', style: TextStyle(fontWeight: FontWeight.w600, color: AppTokens.ink)),
                    subtitle: Text('2 hours ago', style: TextStyle(color: AppTokens.inkMute, fontSize: 12)),
                  ),
                  Divider(color: AppTokens.line),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(Icons.check_circle_outline, color: AppTokens.emerald),
                    title: Text('Approved Workflow WF-A9C0', style: TextStyle(fontWeight: FontWeight.w600, color: AppTokens.ink)),
                    subtitle: Text('Yesterday', style: TextStyle(color: AppTokens.inkMute, fontSize: 12)),
                  ),
                  Divider(color: AppTokens.line),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(Icons.auto_awesome, color: AppTokens.amber),
                    title: Text('Started New Project', style: TextStyle(fontWeight: FontWeight.w600, color: AppTokens.ink)),
                    subtitle: Text('2 days ago', style: TextStyle(color: AppTokens.inkMute, fontSize: 12)),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 100),
          ],
        ),
      ),
    );
  }
}
