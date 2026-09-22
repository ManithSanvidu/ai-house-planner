import 'dart:io';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/land_submission.dart';
import '../core/network/api_client.dart';

// Provides the current state of the intake form
final intakeProvider = StateNotifierProvider<IntakeNotifier, AsyncValue<LandSubmission>>((ref) {
  return IntakeNotifier();
});

class IntakeNotifier extends StateNotifier<AsyncValue<LandSubmission>> {
  IntakeNotifier() : super(AsyncValue.data(LandSubmission(
    landSizePerches: 10.0,
    preferredBedrooms: 3,
    preferredFloors: 1,
    stylePreference: 'modern',
    manualTerrainType: 'flat',
  )));

  void updateField({
    double? budgetLkr,
    double? landSizePerches,
    String? manualTerrainType,
    int? preferredBedrooms,
    int? preferredFloors,
    String? stylePreference,
  }) {
    final currentData = state.value ?? LandSubmission();
    state = AsyncValue.data(currentData.copyWith(
      budgetLkr: budgetLkr,
      landSizePerches: landSizePerches,
      manualTerrainType: manualTerrainType,
      preferredBedrooms: preferredBedrooms,
      preferredFloors: preferredFloors,
      stylePreference: stylePreference,
      clearManualTerrain: manualTerrainType != null ? false : currentData.landPhoto != null,
    ));
  }

  void setPhoto(File photo) {
    final currentData = state.value ?? LandSubmission();
    // If a photo is provided, clear the manual terrain fallback
    state = AsyncValue.data(currentData.copyWith(
      landPhoto: photo,
      clearManualTerrain: true,
    ));
  }

  void clearPhoto() {
    final currentData = state.value ?? LandSubmission();
    state = AsyncValue.data(currentData.copyWith(clearPhoto: true));
  }

  void togglePreference(String preference) {
    final currentData = state.value ?? LandSubmission();
    
    switch (preference) {
      case 'openPlan':
        state = AsyncValue.data(currentData.copyWith(openPlan: !currentData.openPlan));
        break;
      case 'masterEnsuite':
        state = AsyncValue.data(currentData.copyWith(masterEnsuite: !currentData.masterEnsuite));
        break;
      case 'homeOffice':
        state = AsyncValue.data(currentData.copyWith(homeOffice: !currentData.homeOffice));
        break;
      case 'balcony':
        state = AsyncValue.data(currentData.copyWith(balcony: !currentData.balcony));
        break;
      case 'parkingRequired':
        state = AsyncValue.data(currentData.copyWith(parkingRequired: !currentData.parkingRequired));
        break;
      case 'accessibility':
        state = AsyncValue.data(currentData.copyWith(accessibility: !currentData.accessibility));
        break;
    }
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
      final payload = {
        'basePreDesignedPlanId': basePlanId,
        'planSelectionMode': mode,
        'landSizePerches': data.landSizePerches,
        'manualTerrainType': data.manualTerrainType == 'flat' ? 'Flat' : data.manualTerrainType == 'hillside' ? 'Hillside' : data.manualTerrainType == 'coastal' ? 'Coastal' : data.manualTerrainType == 'forested' ? 'Forested' : 'Flat',
        'budgetLkr': data.budgetLkr,
        'preferences': {
          'bedrooms': data.preferredBedrooms ?? 3,
          'bathrooms': 1,
          'floors': data.preferredFloors ?? 1,
          'architecturalStyle': data.stylePreference == 'modern' ? 'Modern' : data.stylePreference == 'traditional' ? 'Traditional' : data.stylePreference == 'contemporary' ? 'Contemporary' : 'Modern',
          'openPlan': data.openPlan,
          'masterEnsuite': data.masterEnsuite,
          'separateDining': false,
          'homeOffice': data.homeOffice,
          'balcony': data.balcony,
          'veranda': false,
          'utilityRoom': false,
          'parkingRequired': data.parkingRequired,
          'accessibility': data.accessibility,
          'spacePriority': 'balanced',
          'circulationPreference': 'space_efficient'
        },
        'plotConstraints': {
          'road_side': 'south',
          'north_direction': 'north',
          'entrance_side': 'south'
        },
        'designSeed': 12345,
      };

      final response = await ApiClient.instance.post('/ai-generation/generate', data: payload);
      
      state = AsyncValue.data(data); // Revert to data state on success
      return response.data['workflowId'] as String? ?? response.data['WorkflowId'] as String?;
    } catch (e) {
      state = AsyncValue.data(data); // Revert to data state so form stays visible
      rethrow;
    }
  }
}
