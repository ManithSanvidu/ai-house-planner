import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/workflow_provider.dart';
import '../widgets/floor_plan_painter.dart';

class DesignPreviewView extends ConsumerStatefulWidget {
  final String workflowId;

  const DesignPreviewView({super.key, required this.workflowId});

  @override
  ConsumerState<DesignPreviewView> createState() => _DesignPreviewViewState();
}

class _DesignPreviewViewState extends ConsumerState<DesignPreviewView> {
  int _selectedFloor = 1;

  void _handleApproval(String decision) async {
    try {
      await ref.read(workflowProvider(widget.workflowId).notifier).approveWorkflow(decision);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Design $decision submitted successfully', style: const TextStyle(color: Colors.white)), backgroundColor: Colors.green),
        );
        Navigator.pop(context);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final workflowState = ref.watch(workflowProvider(widget.workflowId));

    return workflowState.when(
      loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
      error: (e, st) => Scaffold(body: Center(child: Text('Error: $e'))),
      data: (data) {
        final design = data.design;
        if (design == null) {
          return const Scaffold(body: Center(child: Text('Design not available yet.')));
        }

        final version = design['version'] ?? 1;
        final terrainType = data.design?['terrainType'] ?? 'N/A';
        final floorCount = design['floorCount'] ?? 1;
        final score = design['designScore']?.toString() ?? 'N/A';
        
        final rooms = (design['rooms'] as List? ?? []).map((r) {
          final entrances = (design['entrances'] as List? ?? []).where((e) => e['room_id'] == r['roomId']);
          return RoomLayout(
            entrance: entrances.isEmpty ? null : Opening.fromJson(entrances.first),
            roomId: r['roomId'] ?? '',
            roomType: r['roomType'] ?? 'unknown',
            name: r['name'],
            floor: r['floorNumber'] ?? 1,
            x: (r['x'] as num).toDouble(),
            y: (r['y'] as num).toDouble(),
            width: (r['width'] as num).toDouble(),
            length: (r['length'] as num).toDouble(),
            doors: (r['doors'] as List?)?.map((d) => Opening(wall: d['wall'], offset: (d['offset'] as num).toDouble(), width: (d['width'] as num).toDouble())).toList() ?? [],
            windows: (r['windows'] as List?)?.map((w) => Opening(wall: w['wall'], offset: (w['offset'] as num).toDouble(), width: (w['width'] as num).toDouble())).toList() ?? [],
          );
        }).toList();

        return Scaffold(
          backgroundColor: Colors.white,
          appBar: AppBar(
            title: Text('Your Home Design (v$version)', style: const TextStyle(color: Colors.black87, fontWeight: FontWeight.bold)),
            backgroundColor: Colors.white,
            elevation: 0,
            iconTheme: const IconThemeData(color: Colors.black87),
          ),
          body: Column(
            children: [
              // Specs Card
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                color: Colors.grey[50],
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  children: [
                    _buildSpecItem('Score', score),
                    _buildSpecItem('Area', '${design['totalBuiltUpAreaSqft']} sqft'),
                    _buildSpecItem('Foundation', (design['foundationType'] ?? '').toString().toUpperCase()),
                    _buildSpecItem('Terrain', terrainType.toString().toUpperCase()),
                  ],
                ),
              ),

              // Floor Tabs (only show for multi-floor)
              if (floorCount > 1)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  color: Colors.white,
                  child: Row(
                    children: List.generate(floorCount, (i) {
                      final floor = i + 1;
                      final isSelected = _selectedFloor == floor;
                      return Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: Text('Floor $floor'),
                          selected: isSelected,
                          selectedColor: Colors.indigo,
                          labelStyle: TextStyle(
                            color: isSelected ? Colors.white : Colors.black87,
                            fontWeight: FontWeight.w600,
                          ),
                          onSelected: (_) => setState(() => _selectedFloor = floor),
                        ),
                      );
                    }),
                  ),
                ),

              // Floor Plan Interactive View
              Expanded(
                child: InteractiveViewer(
                  minScale: 0.5,
                  maxScale: 3.0,
                  child: FloorPlanViewer(rooms: rooms, floorFilter: _selectedFloor),
                ),
              ),

              // Action Buttons
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: Colors.white,
                  boxShadow: [
                    BoxShadow(color: Colors.black.withValues(alpha: 0.05), blurRadius: 10, offset: const Offset(0, -5))
                  ],
                ),
                child: Column(
                  children: [
                    SizedBox(
                      width: double.infinity,
                      height: 50,
                      child: ElevatedButton(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: Colors.indigo,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                        ),
                        onPressed: () => _handleApproval('approve'),
                        child: const Text('Approve & Proceed to Costing', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      ),
                    ),
                    const SizedBox(height: 12),
                    SizedBox(
                      width: double.infinity,
                      height: 50,
                      child: OutlinedButton(
                        style: OutlinedButton.styleFrom(
                          foregroundColor: Colors.red,
                          side: const BorderSide(color: Colors.red),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                        ),
                        onPressed: () => _handleApproval('reject'),
                        child: const Text('Reject Design'),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      }
    );
  }

  Widget _buildSpecItem(String label, String value) {
    return Column(
      children: [
        Text(label, style: const TextStyle(color: Colors.grey, fontSize: 11, fontWeight: FontWeight.w600)),
        const SizedBox(height: 4),
        Text(value, style: const TextStyle(color: Colors.black87, fontSize: 14, fontWeight: FontWeight.bold)),
      ],
    );
  }
}
