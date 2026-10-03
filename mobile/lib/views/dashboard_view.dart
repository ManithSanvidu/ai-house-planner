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
    final displayName = name.isNotEmpty ? name[0].toUpperCase() + name.substring(1) : 'User';

    return Scaffold(
      backgroundColor: const Color(0xFFF9FAFB), // light iOS gray background
      body: SafeArea(
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Welcome Header
              Padding(
                padding: const EdgeInsets.only(left: 24, right: 24, top: 16),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Welcome back,', style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800, color: AppTokens.navy, height: 1.1)),
                          Text(displayName, style: const TextStyle(fontSize: 32, fontWeight: FontWeight.w900, color: AppTokens.primary, height: 1.1)),
                          const SizedBox(height: 12),
                          const Text("Here's an overview of your home\nplanning journey.", style: TextStyle(fontSize: 14, color: AppTokens.textSecondary, height: 1.4, fontWeight: FontWeight.w500)),
                        ],
                      ),
                    ),
                    ClipRRect(
                      borderRadius: const BorderRadius.only(topLeft: Radius.circular(80), topRight: Radius.circular(20), bottomLeft: Radius.circular(20), bottomRight: Radius.circular(20)),
                      child: Image.network(
                        'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?q=80&w=400&auto=format&fit=crop',
                        width: 140,
                        height: 110,
                        fit: BoxFit.cover,
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 24),

              // Create New Project Banner
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: GestureDetector(
                  onTap: () => context.go('/intake'),
                  child: Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(24),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        colors: [Color(0xFF3B82F6), Color(0xFF60A5FA)],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(24),
                      boxShadow: [BoxShadow(color: const Color(0xFF3B82F6).withOpacity(0.3), blurRadius: 15, offset: const Offset(0, 8))],
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Container(
                                    padding: const EdgeInsets.all(6),
                                    decoration: BoxDecoration(color: Colors.white.withOpacity(0.2), shape: BoxShape.circle),
                                    child: const Icon(Icons.auto_awesome, color: Colors.white, size: 16),
                                  ),
                                  const SizedBox(width: 8),
                                  const Text('START YOUR JOURNEY', style: TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold, letterSpacing: 1.0)),
                                ],
                              ),
                              const SizedBox(height: 12),
                              const Text('Create New Project', style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold)),
                              const SizedBox(height: 4),
                              Text('Turn your dream home into reality with\nAI-powered planning.', style: TextStyle(color: Colors.white.withOpacity(0.9), fontSize: 12, height: 1.4)),
                            ],
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
                          child: const Icon(Icons.arrow_forward, color: Color(0xFF3B82F6), size: 20),
                        ),
                      ],
                    ),
                  ),
                ),
              ),

              const SizedBox(height: 24),

              // Quick Links Grid
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Row(
                  children: [
                    _buildActionCard(context, 'Ask AI\nArchitect', 'Get instant design\nideas and guidance', Icons.calculate_outlined, const Color(0xFFECFDF5), const Color(0xFF059669), () {}),
                    const SizedBox(width: 12),
                    _buildActionCard(context, 'Browse\nPlans', 'Explore pre-designed\nhouse plans', Icons.design_services_outlined, const Color(0xFFEFF6FF), const Color(0xFF2563EB), () => context.go('/plans')),
                    const SizedBox(width: 12),
                    _buildActionCard(context, 'Construction\nGuide', 'Plan your build\nstep by step', Icons.architecture_outlined, const Color(0xFFF5F3FF), const Color(0xFF7C3AED), () => context.go('/construction')),
                    const SizedBox(width: 12),
                    _buildActionCard(context, 'My\nProjects', 'View and manage\nyour projects', Icons.folder_open_outlined, const Color(0xFFFFFBEB), const Color(0xFFD97706), () => context.go('/designs')),
                  ],
                ),
              ),

              const SizedBox(height: 32),

              // Current Project Section
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Current Project', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTokens.navy)),
                    GestureDetector(
                      onTap: () => context.go('/designs'),
                      child: const Row(
                        children: [
                          Text('View All', style: TextStyle(color: AppTokens.primary, fontWeight: FontWeight.w600, fontSize: 14)),
                          SizedBox(width: 4),
                          Icon(Icons.arrow_forward, color: AppTokens.primary, size: 16),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(24),
                    boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.03), blurRadius: 10, offset: const Offset(0, 4))],
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          ClipRRect(
                            borderRadius: BorderRadius.circular(16),
                            child: Image.network(
                              'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?q=80&w=400&auto=format&fit=crop',
                              width: 100,
                              height: 80,
                              fit: BoxFit.cover,
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                  decoration: BoxDecoration(color: const Color(0xFFECFDF5), borderRadius: BorderRadius.circular(20)),
                                  child: const Row(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      Icon(Icons.circle, color: Color(0xFF059669), size: 6),
                                      SizedBox(width: 4),
                                      Text('IN PROGRESS', style: TextStyle(color: Color(0xFF059669), fontSize: 9, fontWeight: FontWeight.bold, letterSpacing: 0.5)),
                                    ],
                                  ),
                                ),
                                const SizedBox(height: 8),
                                const Text('Approved Design v1', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTokens.navy)),
                                const SizedBox(height: 4),
                                const Row(
                                  children: [
                                    Icon(Icons.verified_outlined, size: 14, color: Color(0xFF059669)),
                                    SizedBox(width: 4),
                                    Text('Architect Approved', style: TextStyle(color: Color(0xFF059669), fontSize: 12, fontWeight: FontWeight.w600)),
                                  ],
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const Padding(
                        padding: EdgeInsets.symmetric(vertical: 20),
                        child: Divider(height: 1, color: AppTokens.line),
                      ),
                      Row(
                        children: [
                          Expanded(
                            child: Row(
                              children: [
                                const Icon(Icons.person_outline, color: AppTokens.textSecondary, size: 20),
                                const SizedBox(width: 8),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: const [
                                    Text('Constructor', style: TextStyle(fontSize: 10, color: AppTokens.textSecondary, fontWeight: FontWeight.w500)),
                                    Text('Dinuki', style: TextStyle(fontSize: 13, color: AppTokens.navy, fontWeight: FontWeight.bold)),
                                  ],
                                ),
                              ],
                            ),
                          ),
                          Container(width: 1, height: 32, color: AppTokens.line),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Row(
                              children: [
                                const Icon(Icons.calendar_today_outlined, color: AppTokens.textSecondary, size: 20),
                                const SizedBox(width: 8),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: const [
                                    Text('Current Phase', style: TextStyle(fontSize: 10, color: AppTokens.textSecondary, fontWeight: FontWeight.w500)),
                                    Text('Site Preparation', style: TextStyle(fontSize: 13, color: AppTokens.navy, fontWeight: FontWeight.bold)),
                                  ],
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 20),
                      Row(
                        children: [
                          Expanded(
                            child: ElevatedButton(
                              style: ElevatedButton.styleFrom(
                                backgroundColor: const Color(0xFF0F172A), // Almost black
                                padding: const EdgeInsets.symmetric(vertical: 14),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                                elevation: 0,
                              ),
                              onPressed: () => context.go('/designs'),
                              child: const Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  Icon(Icons.visibility_outlined, color: Colors.white, size: 16),
                                  SizedBox(width: 8),
                                  Text('View Design', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 13)),
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Container(
                            decoration: BoxDecoration(color: const Color(0xFFF1F5F9), borderRadius: BorderRadius.circular(16)),
                            child: IconButton(
                              icon: const Icon(Icons.arrow_forward, color: AppTokens.navy, size: 20),
                              onPressed: () => context.go('/construction'),
                            ),
                          )
                        ],
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 32),

              // Approved Designs
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Approved Designs', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTokens.navy)),
                    GestureDetector(
                      onTap: () => context.go('/designs'),
                      child: const Row(
                        children: [
                          Text('View All', style: TextStyle(color: AppTokens.primary, fontWeight: FontWeight.w600, fontSize: 14)),
                          SizedBox(width: 4),
                          Icon(Icons.arrow_forward, color: AppTokens.primary, size: 16),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.03), blurRadius: 10, offset: const Offset(0, 4))],
                  ),
                  child: Row(
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(12),
                        child: Image.network(
                          'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?q=80&w=400&auto=format&fit=crop',
                          width: 80,
                          height: 60,
                          fit: BoxFit.cover,
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Approved Design v1', style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppTokens.navy)),
                            const SizedBox(height: 6),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                              decoration: BoxDecoration(color: const Color(0xFFECFDF5), borderRadius: BorderRadius.circular(20)),
                              child: const Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(Icons.circle, color: Color(0xFF059669), size: 6),
                                  SizedBox(width: 4),
                                  Text('Approved', style: TextStyle(color: Color(0xFF059669), fontSize: 9, fontWeight: FontWeight.bold)),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                      const Icon(Icons.chevron_right, color: AppTokens.textSecondary),
                    ],
                  ),
                ),
              ),
              
              const SizedBox(height: 100), // Space for bottom nav
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildActionCard(BuildContext context, String title, String subtitle, IconData icon, Color bgColor, Color fgColor, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 150,
        height: 190,
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: bgColor,
          borderRadius: BorderRadius.circular(24),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(color: fgColor.withOpacity(0.1), borderRadius: BorderRadius.circular(12)),
              child: Icon(icon, color: fgColor, size: 24),
            ),
            const Spacer(),
            Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: AppTokens.navy, height: 1.2), overflow: TextOverflow.ellipsis, maxLines: 2),
            const SizedBox(height: 6),
            Text(subtitle, style: const TextStyle(fontSize: 10, color: AppTokens.textSecondary, height: 1.3), overflow: TextOverflow.ellipsis, maxLines: 2),
            const SizedBox(height: 12),
            Icon(Icons.arrow_forward, color: fgColor, size: 16),
          ],
        ),
      ),
    );
  }
}
