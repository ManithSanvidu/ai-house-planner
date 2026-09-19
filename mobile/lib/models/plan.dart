class Plan {
  final String id;
  final String name;
  final String description;
  final String style;
  final double estimatedCost;
  final int squareFootage;
  final int bedrooms;
  final int bathrooms;
  final List<String> imageUrls;
  final String designCode;
  final String category;
  final String suitableTerrain;
  final int floorCount;
  final double totalBuiltUpAreaSqft;
  final DateTime createdAt;
  final DateTime? updatedAt;

  Plan({
    required this.id,
    required this.name,
    required this.description,
    required this.style,
    required this.estimatedCost,
    required this.squareFootage,
    required this.bedrooms,
    required this.bathrooms,
    required this.imageUrls,
    this.designCode = 'P-001',
    this.category = 'Standard',
    this.suitableTerrain = 'Flat',
    this.floorCount = 1,
    this.totalBuiltUpAreaSqft = 0.0,
    required this.createdAt,
    this.updatedAt,
  });

  factory Plan.fromJson(Map<String, dynamic> json) {
    return Plan(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      description: json['description'] ?? '',
      style: json['style'] ?? '',
      estimatedCost: (json['estimatedCost'] ?? 0).toDouble(),
      squareFootage: json['squareFootage'] ?? 0,
      bedrooms: json['bedrooms'] ?? 0,
      bathrooms: json['bathrooms'] ?? 0,
      imageUrls: List<String>.from(json['imageUrls'] ?? []),
      designCode: json['designCode'] ?? json['id']?.toString().substring(0, 5) ?? 'P-001',
      category: json['category'] ?? 'Standard',
      suitableTerrain: json['suitableTerrain'] ?? 'Flat',
      floorCount: json['floorCount'] ?? 1,
      totalBuiltUpAreaSqft: (json['totalBuiltUpAreaSqft'] ?? json['squareFootage'] ?? 0).toDouble(),
      createdAt: json['createdAt'] != null ? DateTime.parse(json['createdAt']) : DateTime.now(),
      updatedAt: json['updatedAt'] != null ? DateTime.parse(json['updatedAt']) : null,
    );
  }
}
