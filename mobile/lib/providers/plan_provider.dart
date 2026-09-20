import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/plan.dart';
import '../core/network/api_client.dart';


final planServiceProvider = Provider<PlanService>((ref) {
  return PlanService();
});

final plansProvider = FutureProvider<List<Plan>>((ref) async {
  final planService = ref.read(planServiceProvider);
  return planService.getPlans();
});

final planDetailProvider = FutureProvider.family<Plan, String>((ref, id) async {
  final planService = ref.read(planServiceProvider);
  return planService.getPlan(id);
});

class PlanService {
  Future<List<Plan>> getPlans() async {
    try {
      final response = await ApiClient.instance.get('/pre-designed-plans');
      if (response.statusCode == 200) {
        final List<dynamic> data = response.data;
        return data.map((json) => Plan.fromJson(json)).toList();
      } else {
        return _getMockPlans();
      }
    } catch (e) {
      return _getMockPlans();
    }
  }

  Future<Plan> getPlan(String id) async {
    try {
      final response = await ApiClient.instance.get('/pre-designed-plans/$id');
      if (response.statusCode == 200) {
        return Plan.fromJson(response.data);
      } else {
        throw Exception('Failed to load plan');
      }
    } catch (e) {
      final mockPlans = _getMockPlans();
      return mockPlans.firstWhere((p) => p.id == id, orElse: () => throw Exception('Plan not found'));
    }
  }

  List<Plan> _getMockPlans() {
    return [
      Plan(
        id: 'HP-2B1B-1F-7',
        name: 'Test Plan HP-2B1B-1F-7',
        description: 'AI Generated Plan',
        style: 'Modern Minimalist',
        estimatedCost: 150000,
        squareFootage: 1200,
        bedrooms: 2,
        bathrooms: 1,
        imageUrls: [],
        designCode: 'HP-2B1B-1F-7',
        category: 'Standard',
        suitableTerrain: 'hillside',
        floorCount: 1,
        totalBuiltUpAreaSqft: 1200,
        createdAt: DateTime.now(),
      ),
      Plan(
        id: 'HP-2B1B-1F-9',
        name: 'Test Plan HP-2B1B-1F-9',
        description: 'AI Generated Plan',
        style: 'Modern Minimalist',
        estimatedCost: 160000,
        squareFootage: 1200,
        bedrooms: 2,
        bathrooms: 1,
        imageUrls: [],
        designCode: 'HP-2B1B-1F-9',
        category: 'Standard',
        suitableTerrain: 'hillside',
        floorCount: 1,
        totalBuiltUpAreaSqft: 1200,
        createdAt: DateTime.now(),
      ),
      Plan(
        id: 'HP-2B1B-2F-25',
        name: 'Test Plan HP-2B1B-2F-25',
        description: 'AI Generated Plan',
        style: 'Modern Minimalist',
        estimatedCost: 250000,
        squareFootage: 1800,
        bedrooms: 2,
        bathrooms: 1,
        imageUrls: [],
        designCode: 'HP-2B1B-2F-25',
        category: 'Standard',
        suitableTerrain: 'hillside',
        floorCount: 2,
        totalBuiltUpAreaSqft: 1800,
        createdAt: DateTime.now(),
      )
    ];
  }
}
