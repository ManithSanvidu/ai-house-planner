import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/workflow_provider.dart';

class WorkflowStatusView extends ConsumerWidget {
  final String workflowId;
  const WorkflowStatusView({super.key, required this.workflowId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final workflowState = ref.watch(workflowProvider(workflowId));
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        title: const Text('Workflow Status'),
        elevation: 0,
        backgroundColor: Colors.transparent,
      ),
      body: workflowState.when(
        loading: () => const Center(child: CircularProgressIndicator(color: Colors.indigo)),
        error: (err, stack) => Center(child: Text('Error: $err', style: const TextStyle(color: Colors.red))),
        data: (data) {
          final isCompleted = data.status == 'completed';
          final isFailed = data.status == 'failed';
          
          return Center(
            child: Padding(
              padding: const EdgeInsets.all(24.0),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  if (!isCompleted && !isFailed) ...[
                    const CircularProgressIndicator(color: Colors.indigo),
                    const SizedBox(height: 24),
                    const Text('AI is orchestrating your design...', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 8),
                    Text('Status: ${data.status.toUpperCase()}', style: const TextStyle(color: Colors.grey)),
                  ],
                  if (isFailed) ...[
                    const Icon(Icons.error_outline, color: Colors.red, size: 64),
                    const SizedBox(height: 16),
                    const Text('Workflow Failed', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 8),
                    Text(data.failureReason ?? 'Unknown error occurred.', textAlign: TextAlign.center, style: const TextStyle(color: Colors.red)),
                  ],
                  if (isCompleted) ...[
                    const Icon(Icons.check_circle_outline, color: Colors.green, size: 64),
                    const SizedBox(height: 16),
                    const Text('Generation Complete!', style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 32),
                    SizedBox(
                      width: double.infinity,
                      height: 56,
                      child: ElevatedButton(
                        onPressed: () => context.go('/design/$workflowId/preview'),
                        style: ElevatedButton.styleFrom(backgroundColor: Colors.indigo, foregroundColor: Colors.white),
                        child: const Text('View Design Details', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      ),
                    ),
                    const SizedBox(height: 16),
                    if (data.constructionPlan != null)
                      SizedBox(
                        width: double.infinity,
                        height: 56,
                        child: OutlinedButton(
                          onPressed: () => context.go('/design/$workflowId/timeline'),
                          style: OutlinedButton.styleFrom(foregroundColor: Colors.indigo, side: const BorderSide(color: Colors.indigo)),
                          child: const Text('View Construction Timeline', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                        ),
                      )
                  ],
                ],
              ),
            ),
          );
        }
      ),
    );
  }
}
