import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../core/theme/app_tokens.dart';

class MainScaffold extends StatelessWidget {
  final Widget child;

  const MainScaffold({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    final String location = GoRouterState.of(context).matchedLocation;
    String title = 'Dashboard';
    if (location.startsWith('/plans')) title = 'Plan Library';
    if (location.startsWith('/intake')) title = 'New Project';
    if (location.startsWith('/designs')) title = 'My Designs';
    if (location.startsWith('/construction')) title = 'Construction';
    if (location.startsWith('/readiness')) title = 'Readiness Planner';
    if (location.startsWith('/profile')) title = 'Account Settings';

    return Scaffold(
      backgroundColor: AppTokens.bg,
      appBar: AppBar(
        title: Text(title, style: const TextStyle(color: Colors.black, fontSize: 18, fontWeight: FontWeight.bold)),
        backgroundColor: Colors.white,
        elevation: 0,
        iconTheme: const IconThemeData(color: Colors.black),
      ),
      drawer: Drawer(
        backgroundColor: Colors.white,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Padding(
              padding: EdgeInsets.only(top: 48.0, left: 24.0, bottom: 16.0),
              child: Text(
                'NAVIGATION',
                style: TextStyle(
                  color: Color(0xFF475569),
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.2,
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0),
              child: Column(
                children: [
                  _NavItem(icon: Icons.dashboard_outlined, label: 'Overview', route: '/dashboard', currentPath: location),
                  _NavItem(icon: Icons.add_box_outlined, label: 'New Project', route: '/intake', currentPath: location),
                  _NavItem(icon: Icons.menu_book_outlined, label: 'Plan Library', route: '/plans', currentPath: location),
                  _NavItem(icon: Icons.folder_outlined, label: 'My Designs', route: '/designs', currentPath: location),
                  _NavItem(icon: Icons.domain, label: 'Construction', route: '/construction', currentPath: location),
                  _NavItem(icon: Icons.assignment_turned_in_outlined, label: 'Readiness Planner', route: '/readiness', currentPath: location),
                ],
              ),
            ),
          ],
        ),
      ),
      body: child,
    );
  }
}

class _NavItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final String route;
  final String currentPath;

  const _NavItem({
    required this.icon,
    required this.label,
    required this.route,
    required this.currentPath,
  });

  @override
  Widget build(BuildContext context) {
    final bool isSelected = currentPath == route || (route != '/' && currentPath.startsWith(route));
    final color = isSelected ? const Color(0xFF4F46E5) : const Color(0xFF475569);
    final bgColor = isSelected ? const Color(0xFFF3F4F6) : Colors.transparent;

    return InkWell(
      onTap: () {
        Navigator.pop(context); // Close drawer
        context.go(route);
      },
      borderRadius: BorderRadius.circular(12),
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        decoration: BoxDecoration(
          color: bgColor,
          borderRadius: BorderRadius.circular(12),
          border: isSelected ? Border.all(color: const Color(0xFFE5E7EB), width: 1.5) : Border.all(color: Colors.transparent),
        ),
        child: Row(
          children: [
            Icon(icon, color: color, size: 24),
            const SizedBox(width: 16),
            Text(
              label,
              style: TextStyle(
                color: color,
                fontSize: 16,
                fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
