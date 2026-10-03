import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/auth_provider.dart';
import '../core/theme/app_tokens.dart';

class MainScaffold extends ConsumerWidget {
  final Widget child;

  const MainScaffold({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final String location = GoRouterState.of(context).matchedLocation;
    
    final authState = ref.watch(authProvider);
    final email = authState.value?.email ?? 'User';

    int selectedIndex = 0;
    if (location.startsWith('/designs')) selectedIndex = 1;
    if (location.startsWith('/plans')) selectedIndex = 3;
    if (location.startsWith('/profile')) selectedIndex = 4;
    return Scaffold(
      backgroundColor: const Color(0xFFF9FAFB),
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppTokens.navy),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 24.0),
            child: Container(
              width: 36,
              height: 36,
              decoration: BoxDecoration(color: const Color(0xFFEFF6FF), shape: BoxShape.circle),
              child: const Icon(Icons.person, color: Color(0xFF2563EB), size: 20),
            ),
          )
        ],
      ),
      drawer: _buildDrawer(context, ref, location, email),
      body: child,
      extendBody: true, // Needed for floating bottom bar
      bottomNavigationBar: Container(
        margin: const EdgeInsets.only(left: 24, right: 24, bottom: 24),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(32),
          boxShadow: [
            BoxShadow(color: Colors.black.withValues(alpha: 0.05), blurRadius: 20, offset: const Offset(0, 10)),
          ],
        ),
        child: SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _buildBottomNavItem(context, 'Home', Icons.home_filled, 0, selectedIndex, () => context.go('/dashboard')),
                _buildBottomNavItem(context, 'Projects', Icons.folder_outlined, 1, selectedIndex, () => context.go('/designs')),
                
                // Floating Action Button in middle
                GestureDetector(
                  onTap: () => context.go('/intake'),
                  child: Container(
                    width: 56,
                    height: 56,
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(colors: [Color(0xFF60A5FA), Color(0xFF3B82F6)], begin: Alignment.topLeft, end: Alignment.bottomRight),
                      shape: BoxShape.circle,
                      boxShadow: [BoxShadow(color: const Color(0xFF3B82F6).withValues(alpha: 0.4), blurRadius: 12, offset: const Offset(0, 6))],
                    ),
                    child: const Icon(Icons.add, color: Colors.white, size: 28),
                  ),
                ),
                
                _buildBottomNavItem(context, 'Plans', Icons.menu_book_outlined, 3, selectedIndex, () => context.go('/plans')),
                _buildBottomNavItem(context, 'Profile', Icons.person_outline, 4, selectedIndex, () => context.go('/profile')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildBottomNavItem(BuildContext context, String label, IconData icon, int index, int selectedIndex, VoidCallback onTap, {bool hasBadge = false}) {
    final isSelected = index == selectedIndex;
    final color = isSelected ? const Color(0xFF2563EB) : const Color(0xFF94A3B8);

    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        behavior: HitTestBehavior.opaque,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Stack(
              clipBehavior: Clip.none,
              children: [
                Icon(icon, color: color, size: 24),
                if (hasBadge)
                  Positioned(
                    right: -2,
                    top: -2,
                    child: Container(
                      width: 8,
                      height: 8,
                      decoration: BoxDecoration(color: const Color(0xFFEF4444), shape: BoxShape.circle, border: Border.all(color: Colors.white, width: 1.5)),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 4),
            Text(label, style: TextStyle(color: color, fontSize: 10, fontWeight: isSelected ? FontWeight.bold : FontWeight.w500)),
            if (isSelected) ...[
              const SizedBox(height: 4),
              Container(width: 12, height: 3, decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(2))),
            ] else
              const SizedBox(height: 7),
          ],
        ),
      ),
    );
  }

  Widget _buildDrawer(BuildContext context, WidgetRef ref, String location, String email) {
    return Drawer(
      backgroundColor: Colors.white,
      surfaceTintColor: Colors.transparent,
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 24.0),
              child: Row(
                children: [
                  const Icon(Icons.architecture, color: AppTokens.navy, size: 32),
                  const SizedBox(width: 12),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('HOMEPLANNER AI', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w900, color: AppTokens.navy, letterSpacing: 1.2)),
                      Text('CONSOLE', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: AppTokens.primary.withValues(alpha: 0.8), letterSpacing: 1.5)),
                    ],
                  ),
                ],
              ),
            ),
            const Divider(height: 1, color: AppTokens.line),
            Padding(
              padding: const EdgeInsets.all(24.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(email, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14, color: AppTokens.textPrimary)),
                  const SizedBox(height: 4),
                  const Text('Customer', style: TextStyle(fontSize: 12, color: AppTokens.textSecondary)),
                  const SizedBox(height: 12),
                  InkWell(
                    onTap: () {
                      ref.read(authProvider.notifier).logout();
                    },
                    child: const Text('Sign Out', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: Colors.red)),
                  ),
                ],
              ),
            ),
            const Divider(height: 1, color: AppTokens.line),
            const SizedBox(height: 24),
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 24.0),
              child: Text('NAVIGATION', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppTokens.textSecondary, letterSpacing: 1.0)),
            ),
            const SizedBox(height: 16),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.symmetric(horizontal: 16.0),
                children: [
                  _buildDrawerNavItem(context, 'Overview', Icons.dashboard_outlined, '/dashboard', location),
                  _buildDrawerNavItem(context, 'New Project', Icons.add_box_outlined, '/intake', location),
                  _buildDrawerNavItem(context, 'Plan Library', Icons.menu_book_outlined, '/plans', location),
                  _buildDrawerNavItem(context, 'My Designs', Icons.folder_outlined, '/designs', location),
                  _buildDrawerNavItem(context, 'Construction', Icons.business_outlined, '/construction', location),
                  _buildDrawerNavItem(context, 'Readiness Planner', Icons.assignment_turned_in_outlined, '/readiness', location),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDrawerNavItem(BuildContext context, String title, IconData icon, String targetRoute, String currentLocation) {
    bool isActive = false;
    if (targetRoute == '/dashboard' && currentLocation == '/dashboard') {
      isActive = true;
    } else if (targetRoute != '/dashboard' && currentLocation.startsWith(targetRoute)) {
      isActive = true;
    }

    final Color color = isActive ? AppTokens.primary : const Color(0xFF475569);
    final Color bgColor = isActive ? const Color(0xFFF0F5FF) : Colors.transparent;

    return Padding(
      padding: const EdgeInsets.only(bottom: 8.0),
      child: InkWell(
        onTap: () {
          Navigator.pop(context);
          context.go(targetRoute);
        },
        borderRadius: BorderRadius.circular(12),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          decoration: BoxDecoration(color: bgColor, borderRadius: BorderRadius.circular(12)),
          child: Row(
            children: [
              Icon(icon, color: color, size: 24),
              const SizedBox(width: 16),
              Text(
                title,
                style: TextStyle(color: color, fontWeight: isActive ? FontWeight.w600 : FontWeight.w500, fontSize: 16),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
