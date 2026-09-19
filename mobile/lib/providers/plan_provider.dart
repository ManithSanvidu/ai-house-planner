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
        id: '1',
        name: 'Modern Oasis',
        description: 'A beautiful modern home with open spaces.',
        style: 'Modern',
        estimatedCost: 350000,
        squareFootage: 2500,
        bedrooms: 4,
        bathrooms: 3,
        imageUrls: ['https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800'],
        createdAt: DateTime.now(),
      ),
      Plan(
        id: '2',
        name: 'Cozy Cottage',
        description: 'A warm and inviting cottage perfect for small families.',
        style: 'Cottage',
        estimatedCost: 200000,
        squareFootage: 1500,
        bedrooms: 3,
        bathrooms: 2,
        imageUrls: ['https://images.unsplash.com/photo-1518780664697-55e3ad937233?w=800'],
        createdAt: DateTime.now(),
      )
    ];
  }
}
