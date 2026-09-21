import 'dart:io';

class LandSubmission {
  final double? budgetLkr;
  final double? landSizePerches;
  final File? landPhoto;
  final String? manualTerrainType;
  final int? preferredBedrooms;
  final int? preferredFloors;
  final String? stylePreference;
  final Set<String>? selectedAmenities;

  //The value given to the constructor is put into the property of this object.
  LandSubmission({
    this.budgetLkr,
    this.landSizePerches,
    this.landPhoto,
    this.manualTerrainType,
    this.preferredBedrooms,
    this.preferredFloors,
    this.stylePreference,
    this.selectedAmenities,
  });

  //A new LandSubmission object based on current object is created, but only the values that are wanted are changed.
  LandSubmission copyWith({
    double? budgetLkr,
    double? landSizePerches,
    File? landPhoto,
    String? manualTerrainType,
    int? preferredBedrooms,
    int? preferredFloors,
    String? stylePreference,
    Set<String>? selectedAmenities,
    bool clearPhoto=false,
    bool clearManualTerrain=false
  }){
    return LandSubmission(
      budgetLkr: budgetLkr??this.budgetLkr,
      landSizePerches: landSizePerches??this.landSizePerches,
      landPhoto: clearPhoto ? null:(landPhoto ?? this.landPhoto),
      manualTerrainType: clearManualTerrain ? null :(manualTerrainType ?? this.manualTerrainType),
      preferredBedrooms: preferredBedrooms ?? this.preferredBedrooms,
      preferredFloors: preferredFloors ?? this.preferredFloors,
      stylePreference: stylePreference ?? this.stylePreference,
      selectedAmenities: selectedAmenities ?? this.selectedAmenities,
    );
  }

  //To check form is valid submission 
  bool get isValid{
    return (landSizePerches ?? 0) > 0 &&
           (preferredBedrooms ?? 0) > 0 &&
           (preferredFloors ?? 0) > 0;
  }

}