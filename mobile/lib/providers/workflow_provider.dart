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
      final status = state.value?.status;
      if (status == 'completed' || status == 'failed' || status == 'awaiting_approval' || status == 'design_generated') {
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
      throw Exception('Failed to approve/reject workflow');
    }
  }

  Future<void> submitArchitectReview(String designId) async {
    try {
      await ApiClient.instance.post('/workflows/$workflowId/submit-architect-review/$designId');
      await _fetchStatus();
    } catch (e) {
      throw Exception('Failed to submit architect review');
    }
  }

  Future<void> regenerateDesign(String designId) async {
    try {
      await ApiClient.instance.post('/workflows/$workflowId/regenerate/$designId');
      state = const AsyncValue.loading();
      _startPolling(); // Ensure polling is active
      await _fetchStatus();
    } catch (e) {
      throw Exception('Failed to regenerate design');
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}

final myDesignsProvider = StateNotifierProvider.autoDispose<MyDesignsNotifier, AsyncValue<List<dynamic>>>((ref) {
  return MyDesignsNotifier();
});

class MyDesignsNotifier extends StateNotifier<AsyncValue<List<dynamic>>> {
  MyDesignsNotifier() : super(const AsyncValue.loading()) {
    fetch();
  }

  Future<void> fetch() async {
    state = const AsyncValue.loading();
    try {
      final response = await ApiClient.instance.get('/workflows/designs');
      state = AsyncValue.data(response.data as List<dynamic>);
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }

  Future<void> selectDesign(String workflowId, String designId) async {
    await ApiClient.instance.post('/workflows/$workflowId/designs/$designId/select');
    await fetch();
  }

  Future<void> clearSelection(String workflowId) async {
    await ApiClient.instance.delete('/workflows/$workflowId/design-selection');
    await fetch();
  }

  Future<void> submitToArchitect(String workflowId, String designId) async {
    await ApiClient.instance.post('/workflows/$workflowId/submit-architect-review/$designId');
    await fetch();
  }
}

final customerDashboardProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  try {
    final approvedDesignsRes = await ApiClient.instance.get('/customer/construction/approved-designs');
    final overviewRes = await ApiClient.instance.get('/customer/construction');
    final allDesignsRes = await ApiClient.instance.get('/workflows/designs');
    
    return {
      'approvedDesigns': approvedDesignsRes.data as List<dynamic>,
      'overview': overviewRes.data as Map<String, dynamic>,
      'allDesigns': allDesignsRes.data as List<dynamic>,
    };
  } catch (e) {
    throw Exception('Failed to load dashboard data');
  }
});
