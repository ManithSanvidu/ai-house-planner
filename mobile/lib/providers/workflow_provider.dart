import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/network/api_client.dart';
import '../models/workflow_state.dart';

final workflowProvider = StateNotifierProvider.family<WorkflowNotifier, AsyncValue<WorkflowState>, String>((ref, workflowId) {
  return WorkflowNotifier(workflowId);
});

class WorkflowNotifier extends StateNotifier<AsyncValue<WorkflowState>> {
  final String workflowId;
  Timer? _timer;

  WorkflowNotifier(this.workflowId) : super(const AsyncValue.loading()) {
    _fetchStatus();
    _startPolling();
  }

  void _startPolling() {
    _timer = Timer.periodic(const Duration(seconds: 3), (timer) {
      if (state.value?.status == 'completed' || state.value?.status == 'failed') {
        timer.cancel();
      } else {
        _fetchStatus();
      }
    });
  }

  Future<void> _fetchStatus() async {
    try {
      final response = await ApiClient.instance.get('/workflows/$workflowId/status');
      if (response.statusCode == 200) {
        state = AsyncValue.data(WorkflowState.fromJson(response.data));
      }
    } catch (e, st) {
      // Don't override state with error if we already have data, just log it
      if (!state.hasValue) {
        state = AsyncValue.error(e, st);
      }
    }
  }

  Future<void> approveWorkflow(String decision, {String? notes}) async {
    try {
      await ApiClient.instance.post('/workflows/$workflowId/approve', data: {
        'decision': decision,
        'revisionNotes': notes,
      });
      await _fetchStatus(); // Refresh immediately
    } catch (e) {
      // Handle error (e.g. show toast in UI)
      throw Exception('Failed to approve/reject workflow');
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}
