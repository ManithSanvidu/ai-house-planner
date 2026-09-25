import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../core/theme/app_tokens.dart';

class MainScaffold extends StatelessWidget {
  final Widget child;

  const MainScaffold({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBody: true,
      backgroundColor: AppTokens.bg,
      body: child,
      bottomNavigationBar: ClipRRect(
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: 24, sigmaY: 24),
          child: Container(
            decoration: const BoxDecoration(
              color: Color(0xB8FFFFFF), // rgba(255,255,255,0.72)
              border: Border(top: BorderSide(color: AppTokens.line, width: 1)),
            ),
            padding: EdgeInsets.only(bottom: MediaQuery.of(context).padding.bottom),
            child: SizedBox(
              height: 64,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceAround,
                children: [
                  _NavItem(
                    icon: Icons.home_outlined,
                    label: 'Home',
                    isSelected: _calculateSelectedIndex(context) == 0,
                    onTap: () => context.go('/dashboard'),
                  ),
                  _NavItem(
                    icon: Icons.add,
                    label: 'New',
                    isSelected: _calculateSelectedIndex(context) == 2,
                    onTap: () => context.go('/intake'),
                  ),
                  _NavItem(
                    icon: Icons.grid_view,
                    label: 'Plans',
                    isSelected: _calculateSelectedIndex(context) == 1,
                    onTap: () => context.go('/plans'),
                  ),
                  _NavItem(
                    icon: Icons.person_outline,
                    label: 'Profile',
                    isSelected: _calculateSelectedIndex(context) == 3,
                    onTap: () => context.go('/profile'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  static int _calculateSelectedIndex(BuildContext context) {
    final String location = GoRouterState.of(context).matchedLocation;
    if (location.startsWith('/dashboard')) return 0;
    if (location.startsWith('/plans')) return 1;
    if (location.startsWith('/intake')) return 2;
    if (location.startsWith('/profile')) return 3;
    return 0;
  }
}

class _NavItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  const _NavItem({
    required this.icon,
    required this.label,
    required this.isSelected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final color = isSelected ? AppTokens.accent : AppTokens.inkMute;
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: color, size: 24),
            const SizedBox(height: 4),
            Text(
              label,
              style: TextStyle(
                color: color,
                fontSize: 10.5,
                fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
