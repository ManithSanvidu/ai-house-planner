import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/plan_provider.dart';

class PlanDetailView extends ConsumerWidget {
  final String planId;

  const PlanDetailView({super.key, required this.planId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final planDetailAsyncValue = ref.watch(planDetailProvider(planId));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Plan Details'),
      ),
      body: planDetailAsyncValue.when(
        data: (plan) {
          return SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (plan.imageUrls.isNotEmpty)
                  Image.network(plan.imageUrls.first, width: double.infinity, height: 250, fit: BoxFit.cover),
                Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(plan.name, style: Theme.of(context).textTheme.headlineMedium),
                      const SizedBox(height: 8),
                      Text(plan.style, style: const TextStyle(color: Colors.grey, fontSize: 16)),
                      const SizedBox(height: 16),
                      Text(plan.description),
                      const Divider(height: 32),
                      _buildSpecRow('Estimated Cost', '\$${plan.estimatedCost.toStringAsFixed(0)}'),
                      _buildSpecRow('Square Footage', '${plan.squareFootage} sq ft'),
                      _buildSpecRow('Bedrooms', '${plan.bedrooms}'),
                      _buildSpecRow('Bathrooms', '${plan.bathrooms}'),
                    ],
                  ),
                ),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, stack) => Center(child: Text('Error: $err')),
      ),
    );
  }

  Widget _buildSpecRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontWeight: FontWeight.bold)),
          Text(value),
        ],
      ),
    );
  }
}
