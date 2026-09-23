import 'dart:io';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/land_submission.dart';
import '../core/network/api_client.dart';

final intakeProvider = StateNotifierProvider<IntakeNotifier, AsyncValue<LandSubmission>>((ref) {
  return IntakeNotifier();
});

class IntakeNotifier extends StateNotifier<AsyncValue<LandSubmission>> {
  IntakeNotifier() : super(AsyncValue.data(LandSubmission(
    landSizePerches: 10.0,
    preferredBedrooms: 3,
    preferredBathrooms: 1,
    preferredFloors: 1,
    stylePreference: 'modern',
    spacePriority: 'balanced',
    manualTerrainType: 'flat',
  )));

  void updateField({
    double? budgetLkr,
    double? landSizePerches,
    String? manualTerrainType,
    
    double? plotWidth,
    double? plotLength,
    String? roadSide,
    String? northOrientation,
    String? entranceSide,
    String? plotSetbacks,

    int? preferredBedrooms,
    int? preferredBathrooms,
    int? preferredFloors,
    String? stylePreference,
    String? spacePriority,
  }) {
    final currentData = state.value ?? LandSubmission();
    state = AsyncValue.data(currentData.copyWith(
      budgetLkr: budgetLkr,
      landSizePerches: landSizePerches,
      manualTerrainType: manualTerrainType,
      
      plotWidth: plotWidth,
      plotLength: plotLength,
      roadSide: roadSide,
      northOrientation: northOrientation,
      entranceSide: entranceSide,
      plotSetbacks: plotSetbacks,

      preferredBedrooms: preferredBedrooms,
      preferredBathrooms: preferredBathrooms,
      preferredFloors: preferredFloors,
      stylePreference: stylePreference,
      spacePriority: spacePriority,
      
      clearManualTerrain: manualTerrainType != null ? false : currentData.landPhoto != null,
    ));
  }

  void setPhoto(File photo) {
    final currentData = state.value ?? LandSubmission();
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
      case 'separateDining':
        state = AsyncValue.data(currentData.copyWith(separateDining: !currentData.separateDining));
        break;
      case 'homeOffice':
        state = AsyncValue.data(currentData.copyWith(homeOffice: !currentData.homeOffice));
        break;
      case 'balcony':
        state = AsyncValue.data(currentData.copyWith(balcony: !currentData.balcony));
        break;
      case 'veranda':
        state = AsyncValue.data(currentData.copyWith(veranda: !currentData.veranda));
        break;
      case 'utilityLaundry':
        state = AsyncValue.data(currentData.copyWith(utilityLaundry: !currentData.utilityLaundry));
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
        'basePreDesignedPlanId': basePlanId ?? data.basePreDesignedPlanId,
        'planSelectionMode': mode ?? data.planSelectionMode,
        'landSizePerches': data.landSizePerches,
        'manualTerrainType': data.manualTerrainType == 'flat' ? 'Flat' : data.manualTerrainType == 'hillside' ? 'Hillside' : data.manualTerrainType == 'coastal' ? 'Coastal' : data.manualTerrainType == 'forested' ? 'Forested' : 'Flat',
        'budgetLkr': data.budgetLkr,
        'preferences': {
          'bedrooms': data.preferredBedrooms ?? 3,
          'bathrooms': data.preferredBathrooms ?? 1,
          'floors': data.preferredFloors ?? 1,
          'architecturalStyle': data.stylePreference == 'modern' ? 'Modern' : data.stylePreference == 'traditional' ? 'Traditional' : data.stylePreference == 'contemporary' ? 'Contemporary' : 'Modern',
          'openPlan': data.openPlan,
          'masterEnsuite': data.masterEnsuite,
          'separateDining': data.separateDining,
          'homeOffice': data.homeOffice,
          'balcony': data.balcony,
          'veranda': data.veranda,
          'utilityRoom': data.utilityLaundry,
          'parkingRequired': data.parkingRequired,
          'accessibility': data.accessibility,
          'spacePriority': data.spacePriority ?? 'balanced',
          'circulationPreference': 'space_efficient'
        },
        'plotConstraints': {
          'width_ft': data.plotWidth,
          'length_ft': data.plotLength,
          'road_side': data.roadSide ?? 'south',
          'north_direction': data.northOrientation ?? 'north',
          'entrance_side': data.entranceSide ?? 'south',
          'setbacks': data.plotSetbacks
        },
        'designSeed': 12345,
      };

      final response = await ApiClient.instance.post('/ai-generation/generate', data: payload);
      
      state = AsyncValue.data(data); 
      return response.data['workflowId'] as String? ?? response.data['WorkflowId'] as String?;
    } catch (e) {
      state = AsyncValue.data(data);
      rethrow;
    }
  }
}
