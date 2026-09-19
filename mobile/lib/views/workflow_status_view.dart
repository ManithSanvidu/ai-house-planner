import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/workflow_provider.dart';
import '../widgets/floor_plan_painter.dart';
import '../core/theme/app_tokens.dart';

class WorkflowStatusView extends ConsumerStatefulWidget {
  final String workflowId;
  const WorkflowStatusView({super.key, required this.workflowId});

  @override
  ConsumerState<WorkflowStatusView> createState() => _WorkflowStatusViewState();
}

class _WorkflowStatusViewState extends ConsumerState<WorkflowStatusView> {
  int _selectedTab = 0; // 0 = Floor Plan, 1 = Construction Plan
  int _selectedFloor = 1;

  @override
  Widget build(BuildContext context) {
    final workflowState = ref.watch(workflowProvider(widget.workflowId));

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
                context.go('/dashboard');
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
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Design Review', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18, color: AppTokens.ink)),
            Text('WF-${widget.workflowId.toUpperCase().take(8)}', style: const TextStyle(fontSize: 12, color: AppTokens.inkMute, fontFamily: 'monospace')),
          ],
        ),
        actions: [
          Center(
            child: Container(
              margin: const EdgeInsets.only(right: 16),
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: AppTokens.amberSoft,
                borderRadius: BorderRadius.circular(AppTokens.radiusPill),
              ),
              child: const Text(
                'AWAITING APPROVAL',
                style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, letterSpacing: 0.5, color: AppTokens.amber),
              ),
            ),
          ),
        ],
      ),
      body: workflowState.when(
        loading: () => const Center(child: CircularProgressIndicator(color: AppTokens.ink)),
        error: (err, stack) => Center(child: Text('Error: $err', style: const TextStyle(color: AppTokens.red))),
        data: (data) {
          final isCompleted = data.status == 'completed' || data.status == 'awaiting_approval';
          if (!isCompleted) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const CircularProgressIndicator(color: AppTokens.accent),
                  const SizedBox(height: 24),
                  const Text('AI is orchestrating your design...', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTokens.ink)),
                  const SizedBox(height: 8),
                  Text('Status: ${data.status.toUpperCase()}', style: const TextStyle(color: AppTokens.inkMute)),
                ],
              ),
            );
          }

          final design = data.design;
          if (design == null) return const Center(child: Text('Design data missing'));

          // Normalize rooms
          final List<RoomLayout> rooms = (design['rooms'] as List<dynamic>?)?.map((r) => RoomLayout.fromJson(r as Map<String, dynamic>)).toList() ?? [];

          return Column(
            children: [
              // Segmented Tabs
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                child: Container(
                  padding: const EdgeInsets.all(4),
                  decoration: BoxDecoration(
                    color: const Color(0xFFEFEFEF),
                    borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                  ),
                  child: Row(
                    children: [
                      Expanded(
                        child: GestureDetector(
                          onTap: () => setState(() => _selectedTab = 0),
                          child: Container(
                            padding: const EdgeInsets.symmetric(vertical: 10),
                            decoration: BoxDecoration(
                              color: _selectedTab == 0 ? Colors.white : Colors.transparent,
                              borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                              boxShadow: _selectedTab == 0 ? [const BoxShadow(color: Color(0x11000000), blurRadius: 4, offset: Offset(0, 2))] : [],
                            ),
                            child: Center(
                              child: Text('Floor Plan', style: TextStyle(
                                fontSize: 13,
                                fontWeight: _selectedTab == 0 ? FontWeight.w700 : FontWeight.w600,
                                color: _selectedTab == 0 ? AppTokens.ink : AppTokens.inkMute,
                              )),
                            ),
                          ),
                        ),
                      ),
                      Expanded(
                        child: GestureDetector(
                          onTap: () => setState(() => _selectedTab = 1),
                          child: Container(
                            padding: const EdgeInsets.symmetric(vertical: 10),
                            decoration: BoxDecoration(
                              color: _selectedTab == 1 ? Colors.white : Colors.transparent,
                              borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                              boxShadow: _selectedTab == 1 ? [const BoxShadow(color: Color(0x11000000), blurRadius: 4, offset: Offset(0, 2))] : [],
                            ),
                            child: Center(
                              child: Text('Construction Plan', style: TextStyle(
                                fontSize: 13,
                                fontWeight: _selectedTab == 1 ? FontWeight.w700 : FontWeight.w600,
                                color: _selectedTab == 1 ? AppTokens.ink : AppTokens.inkMute,
                              )),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              // Floor plan vs Construction
              Expanded(
                child: _selectedTab == 0
                    ? _buildFloorPlanTab(design, rooms)
                    : const Center(child: Text('Construction Plan Placeholder', style: TextStyle(color: AppTokens.inkMute))),
              ),

              // Bottom Sticky Action Bar
              Container(
                padding: const EdgeInsets.all(24),
                decoration: const BoxDecoration(
                  color: AppTokens.bg,
                  border: Border(top: BorderSide(color: AppTokens.line)),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    ElevatedButton(
                      onPressed: () {},
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppTokens.emerald,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
                        minimumSize: const Size(double.infinity, 0),
                        elevation: 0,
                      ),
                      child: const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.check, size: 18),
                          SizedBox(width: 8),
                          Text('Approve Design', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                        ],
                      ),
                    ),
                    const SizedBox(height: 12),
                    OutlinedButton(
                      onPressed: () {},
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppTokens.ink,
                        backgroundColor: Colors.white,
                        side: const BorderSide(color: AppTokens.line, width: 1.4),
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
                        minimumSize: const Size(double.infinity, 0),
                      ),
                      child: const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.refresh, size: 18),
                          SizedBox(width: 8),
                          Text('Request Revision', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                        ],
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

  Widget _buildFloorPlanTab(dynamic design, List<RoomLayout> rooms) {
    return SingleChildScrollView(
      physics: const BouncingScrollPhysics(),
      child: Column(
        children: [
          // Floor selector
          if (design['floorCount'] != null && design['floorCount'] > 1)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
              child: Row(
                children: List.generate(design['floorCount'] as int, (index) {
                  final floor = index + 1;
                  final isSel = _selectedFloor == floor;
                  return GestureDetector(
                    onTap: () => setState(() => _selectedFloor = floor),
                    child: Container(
                      margin: const EdgeInsets.only(right: 8),
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      decoration: BoxDecoration(
                        color: isSel ? AppTokens.ink : Colors.white,
                        borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                        border: Border.all(color: isSel ? AppTokens.ink : AppTokens.line),
                      ),
                      child: Text('Floor $floor', style: TextStyle(color: isSel ? Colors.white : AppTokens.ink, fontWeight: FontWeight.w600, fontSize: 12.5)),
                    ),
                  );
                }),
              ),
            ),
          
          // Canvas
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            child: Container(
              height: 320,
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
                  child: FloorPlanViewer(
                    rooms: rooms,
                    floorFilter: _selectedFloor,
                  ),
                ),
              ),
            ),
          ),

          // Specs Grid
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: Row(
              children: [
                Expanded(child: _buildSpecTile('Terrain', design['terrainType']?.toString() ?? 'Flat / Urban')),
                const SizedBox(width: 12),
                Expanded(child: _buildSpecTile('Foundation', design['foundationType']?.toString() ?? 'Strip footing')),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: Row(
              children: [
                Expanded(child: _buildSpecTile('Floors', '${design['floorCount'] ?? 1}')),
                const SizedBox(width: 12),
                Expanded(child: _buildSpecTile('Area', '${design['totalBuiltUpAreaSqft']?.toStringAsFixed(0) ?? 0} sqft')),
              ],
            ),
          ),
          
          // Rooms List
          Padding(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: rooms.map((r) => Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Row(
                  children: [
                    Container(width: 4, height: 24, decoration: BoxDecoration(color: AppTokens.accent, borderRadius: BorderRadius.circular(2))),
                    const SizedBox(width: 12),
                    Expanded(child: Text(r.name ?? r.roomType, style: const TextStyle(fontWeight: FontWeight.w600, color: AppTokens.ink, fontSize: 14))),
                    Text("${r.width.toStringAsFixed(0)}'×${r.length.toStringAsFixed(0)}' (F${r.floor})", style: const TextStyle(color: AppTokens.inkMute, fontSize: 12.5)),
                  ],
                ),
              )).toList(),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSpecTile(String label, String value) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 12),
      decoration: BoxDecoration(
        color: AppTokens.card,
        borderRadius: BorderRadius.circular(AppTokens.radiusSection),
        border: Border.all(color: AppTokens.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label.toUpperCase(), style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, letterSpacing: 1.0, color: AppTokens.inkMute)),
          const SizedBox(height: 4),
          Text(value, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppTokens.ink)),
        ],
      ),
    );
  }
}

extension StringExtension on String {
  String take(int count) {
    if (length <= count) return this;
    return substring(0, count);
  }
}
