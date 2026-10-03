import 'dart:io';

class LandSubmission {
  final String? landSizeCategory;
  final double? landSizePerches;
  final int? preferredBedrooms;
  final int? preferredBathrooms;
  final String? stylePreference;
  final int? targetDurationDays;
  final String? basePreDesignedPlanId;
  final String? planSelectionMode;

  LandSubmission({
    this.landSizeCategory,
    this.landSizePerches,
    this.preferredBedrooms,
    this.preferredBathrooms,
    this.stylePreference,
    this.targetDurationDays,
    this.basePreDesignedPlanId,
    this.planSelectionMode,
  });

  LandSubmission copyWith({
    String? landSizeCategory,
    double? landSizePerches,
    int? preferredBedrooms,
    int? preferredBathrooms,
    String? stylePreference,
    int? targetDurationDays,
    String? basePreDesignedPlanId,
    String? planSelectionMode,
  }) {
    return LandSubmission(
      landSizeCategory: landSizeCategory ?? this.landSizeCategory,
      landSizePerches: landSizePerches ?? this.landSizePerches,
      preferredBedrooms: preferredBedrooms ?? this.preferredBedrooms,
      preferredBathrooms: preferredBathrooms ?? this.preferredBathrooms,
      stylePreference: stylePreference ?? this.stylePreference,
      targetDurationDays: targetDurationDays ?? this.targetDurationDays,
      basePreDesignedPlanId: basePreDesignedPlanId ?? this.basePreDesignedPlanId,
      planSelectionMode: planSelectionMode ?? this.planSelectionMode,
    );
  }

  bool get isValid {
    return landSizeCategory != null &&
           preferredBedrooms != null &&
           preferredBathrooms != null &&
           stylePreference != null;
  }
}