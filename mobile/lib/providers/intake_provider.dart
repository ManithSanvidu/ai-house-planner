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
    preferredBedrooms: 1,
    preferredFloors: 1,
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

  Future<String?> submitIntake() async {
    final data = state.value;
    if (data == null || !data.isValid) return null;

    state = const AsyncValue.loading();
    
    try {
      final payload = {
        'landSizePerches': data.landSizePerches,
        'manualTerrainType': data.manualTerrainType,
        'budgetLkr': data.budgetLkr,
        'preferences': {
          'bedrooms': data.preferredBedrooms ?? 3,
          'bathrooms': 1,
          'floors': data.preferredFloors ?? 1,
          'architecturalStyle': data.stylePreference ?? 'Modern Minimalist',
          'openPlan': false,
          'masterEnsuite': false,
          'separateDining': false,
          'homeOffice': false,
          'balcony': false,
          'veranda': false,
          'utilityRoom': false,
          'parkingRequired': false,
          'accessibility': false,
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
      return response.data['workflowId'] as String?;
    } catch (e, st) {
      state = AsyncValue.error(e, st);
      return null;
    }
  }
}
