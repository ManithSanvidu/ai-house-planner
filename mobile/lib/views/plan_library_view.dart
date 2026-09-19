import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/plan_provider.dart';
import '../core/theme/app_tokens.dart';

class PlanLibraryView extends ConsumerStatefulWidget {
  const PlanLibraryView({super.key});

  @override
  ConsumerState<PlanLibraryView> createState() => _PlanLibraryViewState();
}

class _PlanLibraryViewState extends ConsumerState<PlanLibraryView> {
  final _searchController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    final plansAsyncValue = ref.watch(plansProvider);

    return Scaffold(
      backgroundColor: AppTokens.bg,
      appBar: AppBar(
        backgroundColor: AppTokens.bg,
        elevation: 0,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('CONCEPTUAL PLAN LIBRARY', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, letterSpacing: 1.0, color: AppTokens.accent)),
            Text('Pre-designed plans', style: TextStyle(fontSize: 23, fontWeight: FontWeight.w800, color: AppTokens.ink)),
          ],
        ),
      ),
      body: Column(
        children: [
          // Search Bar
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 8, 24, 16),
            child: Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                border: Border.all(color: AppTokens.line),
              ),
              child: TextField(
                controller: _searchController,
                style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
                decoration: const InputDecoration(
                  hintText: 'Search name, style or tag...',
                  hintStyle: TextStyle(color: AppTokens.inkMute, fontSize: 14.5),
                  border: InputBorder.none,
                  prefixIcon: Icon(Icons.search, color: AppTokens.inkMute, size: 20),
                  contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                ),
              ),
            ),
          ),
          
          // Horizontal Pill Filters
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: Row(
              children: [
                _buildFilterChip('All terrains', isActive: true),
                const SizedBox(width: 8),
                _buildFilterChip('3 Bed'),
                const SizedBox(width: 8),
                _buildFilterChip('2 Floors'),
                const SizedBox(width: 8),
                _buildFilterChip('Parking'),
                const SizedBox(width: 8),
                _buildFilterChip('Balcony'),
              ],
            ),
          ),
          const SizedBox(height: 24),

          // Grid View
          Expanded(
            child: plansAsyncValue.when(
              loading: () => const Center(child: CircularProgressIndicator(color: AppTokens.ink)),
              error: (err, stack) => Center(child: Text('Error: $err')),
              data: (plans) {
                if (plans.isEmpty) {
                  return const Center(child: Text('No plans available.', style: TextStyle(color: AppTokens.inkMute)));
                }
                return GridView.builder(
                  padding: const EdgeInsets.fromLTRB(24, 0, 24, 100), // padding for bottom nav
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    childAspectRatio: 0.72,
                    crossAxisSpacing: 16,
                    mainAxisSpacing: 16,
                  ),
                  itemCount: plans.length,
                  itemBuilder: (context, index) {
                    final plan = plans[index];
                    return InkWell(
                      onTap: () => context.go('/plans/${plan.id}'),
                      borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                      child: Container(
                        decoration: BoxDecoration(
                          color: AppTokens.card,
                          borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                          border: Border.all(color: AppTokens.line),
                          boxShadow: const [
                            BoxShadow(
                              color: Color(0x140B0B14),
                              blurRadius: 16,
                              offset: Offset(0, 8),
                              spreadRadius: -8,
                            ),
                          ],
                        ),
                        clipBehavior: Clip.antiAlias,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            // Gradient Placeholder
                            Expanded(
                              flex: 45,
                              child: Container(
                                width: double.infinity,
                                decoration: const BoxDecoration(
                                  gradient: LinearGradient(
                                    begin: Alignment.topLeft,
                                    end: Alignment.bottomRight,
                                    colors: [Color(0xFFE4E1FB), Color(0xFFFDECC7)],
                                  ),
                                ),
                                child: Center(
                                  child: Text(
                                    plan.designCode,
                                    style: const TextStyle(
                                      fontFamily: 'monospace',
                                      color: AppTokens.ink,
                                      fontWeight: FontWeight.w700,
                                      fontSize: 12.5,
                                    ),
                                  ),
                                ),
                              ),
                            ),
                            // Details
                            Expanded(
                              flex: 55,
                              child: Padding(
                                padding: const EdgeInsets.all(12.0),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          plan.name,
                                          style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14, color: AppTokens.ink),
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                        ),
                                        const SizedBox(height: 2),
                                        Text(
                                          '${plan.style} · ${plan.suitableTerrain}',
                                          style: const TextStyle(
                                            fontSize: 10.5,
                                            fontWeight: FontWeight.w500,
                                            color: AppTokens.inkMute,
                                          ),
                                          maxLines: 2,
                                          overflow: TextOverflow.ellipsis,
                                        ),
                                      ],
                                    ),
                                    const Spacer(),
                                    Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text('🛏 ${plan.bedrooms}', style: const TextStyle(fontSize: 10.5, color: AppTokens.inkSoft, fontWeight: FontWeight.w600)),
                                        Text('🛁 ${plan.bathrooms}', style: const TextStyle(fontSize: 10.5, color: AppTokens.inkSoft, fontWeight: FontWeight.w600)),
                                        Text('▤ ${plan.floorCount}', style: const TextStyle(fontSize: 10.5, color: AppTokens.inkSoft, fontWeight: FontWeight.w600)),
                                      ],
                                    ),
                                    const SizedBox(height: 4),
                                    Row(
                                      children: [
                                        Text('▭ ${plan.totalBuiltUpAreaSqft.toStringAsFixed(0)}ft²', style: const TextStyle(fontSize: 10.5, color: AppTokens.inkSoft, fontWeight: FontWeight.w600)),
                                      ],
                                    )
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(String label, {bool isActive = false}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: isActive ? AppTokens.ink : Colors.white,
        borderRadius: BorderRadius.circular(AppTokens.radiusPill),
        border: Border.all(color: isActive ? AppTokens.ink : AppTokens.line),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: isActive ? Colors.white : AppTokens.ink,
          fontSize: 12.5,
          fontWeight: isActive ? FontWeight.w600 : FontWeight.w500,
        ),
      ),
    );
  }
}