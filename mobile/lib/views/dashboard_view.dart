import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/auth_provider.dart';
import '../providers/workflow_provider.dart';

class DashboardView extends ConsumerWidget {
  const DashboardView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final email = authState.value?.email ?? 'User';
    final name = email.split('@').first;
    final displayName = name[0].toUpperCase() + name.substring(1);
    final dashboardDataState = ref.watch(customerDashboardProvider);

    return Scaffold(
      backgroundColor: const Color(0xFFF8F9FB),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Good afternoon', style: TextStyle(color: Color(0xFF8B92A5), fontSize: 14, fontWeight: FontWeight.w500)),
                      Text(displayName, style: const TextStyle(color: Color(0xFF1E2332), fontSize: 28, fontWeight: FontWeight.w800)),
                      const Text('Turn your land into a home', style: TextStyle(color: Color(0xFF8B92A5), fontSize: 14, fontWeight: FontWeight.w500)),
                    ],
                  ),
                  CircleAvatar(
                    radius: 24,
                    backgroundColor: const Color(0xFFE8E5F7),
                    child: Text(
                      displayName[0],
                      style: const TextStyle(color: Color(0xFF5D54C4), fontWeight: FontWeight.w700, fontSize: 18),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),

              // Hero Card
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: const Color(0xFFF2F1FA),
                  borderRadius: BorderRadius.circular(24),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          flex: 3,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Let’s plan your\ndream home',
                                style: TextStyle(
                                  fontSize: 26,
                                  fontWeight: FontWeight.w800,
                                  color: Color(0xFF1E2332),
                                  height: 1.1,
                                ),
                              ),
                              const SizedBox(height: 12),
                              const Text(
                                'From land details to architectural plans and construction timelines — all in one place.',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: Color(0xFF6B7280),
                                  height: 1.4,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          flex: 2,
                          child: Container(
                            height: 100,
                            decoration: const BoxDecoration(
                              shape: BoxShape.circle,
                              image: DecorationImage(
                                image: NetworkImage('https://images.unsplash.com/photo-1600585154340-be6161a56a0c?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80'),
                                fit: BoxFit.cover,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 24),
                    Row(
                      children: [
                        Expanded(
                          child: ElevatedButton(
                            onPressed: () => context.go('/intake'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFF1E2332),
                              foregroundColor: Colors.white,
                              padding: const EdgeInsets.symmetric(vertical: 16),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(16),
                              ),
                              elevation: 0,
                            ),
                            child: const Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(Icons.add, size: 18),
                                SizedBox(width: 6),
                                Text('Start new project', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: OutlinedButton(
                            onPressed: () => context.go('/plans'),
                            style: OutlinedButton.styleFrom(
                              foregroundColor: const Color(0xFF4338CA),
                              backgroundColor: Colors.transparent,
                              side: const BorderSide(color: Color(0xFFE5E7EB), width: 1.5),
                              padding: const EdgeInsets.symmetric(vertical: 16),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(16),
                              ),
                            ),
                            child: const Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(Icons.folder_outlined, size: 18),
                                SizedBox(width: 6),
                                Text('Browse library', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              // Web-like Dashboard Content
              dashboardDataState.when(
                loading: () => const Center(child: Padding(padding: EdgeInsets.all(32), child: CircularProgressIndicator())),
                error: (err, stack) => Center(child: Text('Failed to load dashboard data: $err')),
                data: (data) {
                  final List<dynamic> approvedDesigns = data['approvedDesigns'] ?? [];
                  final List<dynamic> allDesigns = data['allDesigns'] ?? [];
                  final pendingCount = allDesigns.where((p) => p['status'] == 'awaiting_architect_review').length;

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Stats Row
                      Row(
                        children: [
                          Expanded(
                            child: Container(
                              padding: const EdgeInsets.all(20),
                              decoration: BoxDecoration(
                                gradient: const LinearGradient(colors: [Color(0xFF059669), Color(0xFF10B981)], begin: Alignment.topLeft, end: Alignment.bottomRight),
                                borderRadius: BorderRadius.circular(24),
                                boxShadow: const [BoxShadow(color: Color(0x3310B981), blurRadius: 12, offset: Offset(0, 6))],
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Container(
                                    padding: const EdgeInsets.all(8),
                                    decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.2), borderRadius: BorderRadius.circular(12)),
                                    child: const Icon(Icons.check_circle_outline, color: Colors.white, size: 24),
                                  ),
                                  const SizedBox(height: 16),
                                  Text('${approvedDesigns.length}', style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800, color: Colors.white)),
                                  const Text('Approved Designs', style: TextStyle(fontSize: 12, color: Colors.white70, fontWeight: FontWeight.w500)),
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Container(
                              padding: const EdgeInsets.all(20),
                              decoration: BoxDecoration(
                                gradient: const LinearGradient(colors: [Color(0xFF4F46E5), Color(0xFF6366F1)], begin: Alignment.topLeft, end: Alignment.bottomRight),
                                borderRadius: BorderRadius.circular(24),
                                boxShadow: const [BoxShadow(color: Color(0x334F46E5), blurRadius: 12, offset: Offset(0, 6))],
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Container(
                                    padding: const EdgeInsets.all(8),
                                    decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.2), borderRadius: BorderRadius.circular(12)),
                                    child: const Icon(Icons.hourglass_empty, color: Colors.white, size: 24),
                                  ),
                                  const SizedBox(height: 16),
                                  Text('$pendingCount', style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800, color: Colors.white)),
                                  const Text('Pending Designs', style: TextStyle(fontSize: 12, color: Colors.white70, fontWeight: FontWeight.w500)),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 32),

                      // Approved Designs Header
                      const Text('Approved Designs', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800, color: Color(0xFF1E2332))),
                      const SizedBox(height: 16),

                      // Approved Designs List
                      if (approvedDesigns.isEmpty)
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(32),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(24),
                            border: Border.all(color: const Color(0xFFE5E7EB)),
                          ),
                          child: const Column(
                            children: [
                              Icon(Icons.architecture_outlined, size: 48, color: Color(0xFF9CA3AF)),
                              SizedBox(height: 16),
                              Text('No approved designs yet', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16, color: Color(0xFF4B5563))),
                              SizedBox(height: 8),
                              Text('Submit a design to your architect to get approved.', textAlign: TextAlign.center, style: TextStyle(color: Color(0xFF9CA3AF), fontSize: 13)),
                            ],
                          ),
                        )
                      else
                        Column(
                          children: approvedDesigns.take(5).map((d) {
                            return Container(
                              margin: const EdgeInsets.only(bottom: 16),
                              padding: const EdgeInsets.all(16),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(24),
                                boxShadow: const [BoxShadow(color: Color(0x08000000), blurRadius: 15, offset: Offset(0, 5))],
                                border: Border.all(color: const Color(0xFFF3F4F6)),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.all(10),
                                        decoration: BoxDecoration(
                                          color: const Color(0xFFECFDF5),
                                          borderRadius: BorderRadius.circular(12),
                                        ),
                                        child: const Icon(Icons.check, color: Color(0xFF10B981), size: 20),
                                      ),
                                      const SizedBox(width: 12),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment: CrossAxisAlignment.start,
                                          children: [
                                            Text('${d['title']}', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16, color: Color(0xFF1E2332))),
                                            const SizedBox(height: 4),
                                            Text('${d['bedrooms']} Bedrooms • ${d['bathrooms']} Baths • ${d['floorCount']} Floors', style: const TextStyle(fontSize: 12, color: Color(0xFF6B7280))),
                                          ],
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 16),
                                  Row(
                                    children: [
                                      Expanded(
                                        child: OutlinedButton(
                                          onPressed: () => context.push('/workflow/${d['workflowId']}'),
                                          style: OutlinedButton.styleFrom(
                                            foregroundColor: const Color(0xFF4B5563),
                                            side: const BorderSide(color: Color(0xFFE5E7EB)),
                                            padding: const EdgeInsets.symmetric(vertical: 12),
                                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                          ),
                                          child: const Text('View Design', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
                                        ),
                                      ),
                                      const SizedBox(width: 12),
                                      Expanded(
                                        child: ElevatedButton(
                                          onPressed: () {
                                            ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Construction flow not implemented on mobile yet.')));
                                          },
                                          style: ElevatedButton.styleFrom(
                                            backgroundColor: const Color(0xFF4F46E5),
                                            foregroundColor: Colors.white,
                                            padding: const EdgeInsets.symmetric(vertical: 12),
                                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                            elevation: 0,
                                          ),
                                          child: const Text('Find Constructor', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            );
                          }).toList(),
                        ),
                    ],
                  );
                },
              ),
              const SizedBox(height: 80), // Bottom nav padding
            ],
          ),
        ),
      ),
    );
  }

}
