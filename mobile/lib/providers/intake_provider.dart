import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/land_submission.dart';
import '../core/network/api_client.dart';
import 'package:dio/dio.dart';

final intakeProvider = StateNotifierProvider<IntakeNotifier, AsyncValue<LandSubmission>>((ref) {
  return IntakeNotifier();
});

class IntakeNotifier extends StateNotifier<AsyncValue<LandSubmission>> {
  IntakeNotifier() : super(AsyncValue.data(LandSubmission()));

  void updateField({
    String? landSizeCategory,
    double? landSizePerches,
    int? preferredBedrooms,
    int? preferredBathrooms,
    String? stylePreference,
    int? targetDurationDays,
  }) {
    final currentData = state.value ?? LandSubmission();
    state = AsyncValue.data(currentData.copyWith(
      landSizeCategory: landSizeCategory,
      landSizePerches: landSizePerches,
      preferredBedrooms: preferredBedrooms,
      preferredBathrooms: preferredBathrooms,
      stylePreference: stylePreference,
      targetDurationDays: targetDurationDays,
    ));
  }

  void setPreDesignedPlan(String planId, String mode) {
    final currentData = state.value ?? LandSubmission();
    state = AsyncValue.data(currentData.copyWith(
      basePreDesignedPlanId: planId,
      planSelectionMode: mode,
    ));
  }

  Future<String?> submitIntake({String? basePlanId, String? mode}) async {
    final data = state.value;
    if (data == null || !data.isValid) return null;

    state = const AsyncValue.loading();
    
    try {
      final payload = <String, dynamic>{
        'landSizeCategory': data.landSizeCategory,
        'landSizePerches': data.landSizeCategory == 'small' ? 15 : 25,
        'bedrooms': data.preferredBedrooms,
        'bathrooms': data.preferredBathrooms,
        'houseType': data.stylePreference,
      };

      if (data.targetDurationDays != null) {
        payload['targetDurationDays'] = data.targetDurationDays;
      }

      final effectiveBasePlanId = basePlanId ?? data.basePreDesignedPlanId;
      if (effectiveBasePlanId != null && effectiveBasePlanId.isNotEmpty) {
        payload['basePreDesignedPlanId'] = effectiveBasePlanId;
        payload['planSelectionMode'] = mode ?? data.planSelectionMode ?? 'use';
      }

      final response = await ApiClient.instance.post('/ai-generation/generate', data: payload);
      
      state = AsyncValue.data(data); 
      return response.data['workflowId'] as String? ?? response.data['WorkflowId'] as String?;
    } catch (e) {
      state = AsyncValue.data(data);
      if (e is DioException && e.response?.data != null) {
        final errorData = e.response!.data;
        final message = errorData['message'] ?? errorData['Message'] ?? e.message;
        throw Exception(message);
      }
      rethrow;
    }
  }
}