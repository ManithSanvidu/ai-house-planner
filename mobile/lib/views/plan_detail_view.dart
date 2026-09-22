import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/plan_provider.dart';
import '../providers/intake_provider.dart';
import '../core/theme/app_tokens.dart';
import '../widgets/floor_plan_painter.dart';

class PlanDetailView extends ConsumerWidget {
  final String planId;

  const PlanDetailView({super.key, required this.planId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final planDetailAsyncValue = ref.watch(planDetailProvider(planId));

    return Scaffold(
      backgroundColor: AppTokens.bg,
      appBar: AppBar(
        backgroundColor: AppTokens.bg,
        elevation: 0,
        leading: Padding(
          padding: const EdgeInsets.only(left: 16.0),
          child: IconButton(
            icon: const Icon(Icons.arrow_back, color: AppTokens.ink),
            onPressed: () {
              if (GoRouter.of(context).canPop()) {
                context.pop();
              } else {
                context.go('/plans');
              }
            },
            style: IconButton.styleFrom(
              backgroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: const BorderSide(color: AppTokens.line),
              ),
            ),
          ),
        ),
        title: const Text('Plan Library', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 16, color: AppTokens.ink)),
        centerTitle: true,
      ),
      body: planDetailAsyncValue.when(
        loading: () => const Center(child: CircularProgressIndicator(color: AppTokens.ink)),
        error: (err, stack) => Center(child: Text('Error: $err')),
        data: (plan) {
          // Attempt to convert whatever layout structure it has into RoomLayouts.
          // Note: Mocking an empty list if layout isn't implemented in the backend yet.
          final List<RoomLayout> rooms = [];

          return Column(
            children: [
              Expanded(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                  physics: const BouncingScrollPhysics(),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(plan.designCode.toUpperCase(), style: const TextStyle(color: AppTokens.accent, fontWeight: FontWeight.bold, fontSize: 11, letterSpacing: 1.0)),
                      const SizedBox(height: 8),
                      Text(plan.name, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 28, color: AppTokens.ink)),
                      const SizedBox(height: 12),
                      Text(
                        plan.description, // Ensure the description exists
                        style: const TextStyle(color: AppTokens.inkSoft, fontSize: 14.5, height: 1.5),
                      ),
                      const SizedBox(height: 24),
                      
                      // Floor Plan Viewer
                      Container(
                        height: 260,
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                          border: Border.all(color: AppTokens.line),
                          boxShadow: const [
                            BoxShadow(color: Color(0x140B0B14), blurRadius: 16, offset: Offset(0, 8), spreadRadius: -8)
                          ],
                        ),
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                          child: InteractiveViewer(
                            minScale: 0.5,
                            maxScale: 4.0,
                            child: rooms.isNotEmpty 
                              ? FloorPlanViewer(rooms: rooms) 
                              : const Center(child: Text('Layout JSON not provided by API yet.', style: TextStyle(color: AppTokens.inkMute))),
                          ),
                        ),
                      ),
                      const SizedBox(height: 24),

                      // Compatibility Banner
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppTokens.emeraldSoft,
                          borderRadius: BorderRadius.circular(AppTokens.radiusSection),
                          border: Border.all(color: const Color(0xFFB4E6D0)),
                        ),
                        child: const Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('Compatible with your land', style: TextStyle(color: AppTokens.emerald, fontWeight: FontWeight.bold, fontSize: 14.5)),
                            SizedBox(height: 4),
                            Text('Plot width exceeds the minimum required by 6 ft.', style: TextStyle(color: Color(0xFF1B6B48), fontSize: 12.5)),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),

                      // Specs Table
                      Container(
                        padding: const EdgeInsets.all(20),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                          border: Border.all(color: AppTokens.line),
                          boxShadow: const [BoxShadow(color: Color(0x140B0B14), blurRadius: 16, offset: Offset(0, 8), spreadRadius: -8)],
                        ),
                        child: Column(
                          children: [
                            _buildSpecRow('Bedrooms', '${plan.bedrooms}'),
                            const Divider(color: AppTokens.line, height: 24),
                            _buildSpecRow('Bathrooms', '${plan.bathrooms}'),
                            const Divider(color: AppTokens.line, height: 24),
                            _buildSpecRow('Built-up area', '${plan.totalBuiltUpAreaSqft.toStringAsFixed(0)} ft²'),
                            const Divider(color: AppTokens.line, height: 24),
                            _buildSpecRow('Minimum land', '8 perches'), // Mocking 8 perches for now
                            const Divider(color: AppTokens.line, height: 24),
                            _buildSpecRow('Terrain', plan.suitableTerrain),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),
                      const Text(
                        'Conceptual planning only — verify structural details with a licensed engineer before construction.',
                        style: TextStyle(color: AppTokens.amber, fontSize: 11.5, fontWeight: FontWeight.w500),
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 80),
                    ],
                  ),
                ),
              ),

              // Bottom Sticky Actions
              Container(
                padding: const EdgeInsets.all(24),
                decoration: const BoxDecoration(
                  color: AppTokens.bg,
                  border: Border(top: BorderSide(color: AppTokens.line)),
                ),
                child: Row(
                  children: [
                    Expanded(
                      flex: 2,
                      child: ElevatedButton(
                        onPressed: () {
                          ref.read(intakeProvider.notifier).setPreDesignedPlan(planId, 'use');
                          context.go('/intake');
                        },
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTokens.ink,
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
                          elevation: 0,
                        ),
                        child: const Text('Use this plan', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      flex: 1,
                      child: OutlinedButton(
                        onPressed: () {
                          ref.read(intakeProvider.notifier).setPreDesignedPlan(planId, 'adapt');
                          context.go('/intake');
                        },
                        style: OutlinedButton.styleFrom(
                          foregroundColor: AppTokens.ink,
                          backgroundColor: Colors.white,
                          side: const BorderSide(color: AppTokens.line, width: 1.4),
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
                        ),
                        child: const Text('Adapt', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildSpecRow(String label, String value) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: const TextStyle(fontSize: 14, color: AppTokens.inkSoft, fontWeight: FontWeight.w500)),
        Text(value, style: const TextStyle(fontSize: 14, color: AppTokens.ink, fontWeight: FontWeight.bold)),
      ],
    );
  }
}
