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
        throw Exception('Failed to load plans');
      }
    } catch (e) {
      throw Exception('Failed to load plans: $e');
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
      throw Exception('Failed to load plan: $e');
    }
  }
}
