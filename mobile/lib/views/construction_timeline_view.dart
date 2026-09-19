import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/workflow_provider.dart';

class ConstructionTimelineView extends ConsumerWidget {
  final String workflowId;
  const ConstructionTimelineView({super.key, required this.workflowId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final workflowState = ref.watch(workflowProvider(workflowId));

    return Scaffold(
      appBar: AppBar(title: const Text('Construction Timeline')),
      body: workflowState.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, st) => Center(child: Text('Error: $e')),
        data: (data) {
          final plan = data.constructionPlan;
          if (plan == null) return const Center(child: Text('No timeline available.'));

          final phases = plan['phases'] as List<dynamic>? ?? [];
          final summary = plan['project_summary'] as Map<String, dynamic>?;

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (summary != null) ...[
                Text('Estimated Duration: ${summary['estimated_duration_days']} days', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
                const SizedBox(height: 16),
              ],
              ...phases.map((phase) {
                return Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    leading: CircleAvatar(child: Text('${phase['id']}')),
                    title: Text(phase['name'] ?? ''),
                    subtitle: Text('Days ${phase['start_day']} - ${phase['end_day']} (${phase['duration_days']} days)'),
                  ),
                );
              }),
            ],
          );
        }
      ),
    );
  }
}
