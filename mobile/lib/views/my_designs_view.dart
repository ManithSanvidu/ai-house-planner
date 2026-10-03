import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class MyDesignsView extends ConsumerWidget {
  const MyDesignsView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('My Designs', style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Color(0xFF0F172A))),
              const SizedBox(height: 8),
              const Text('View and manage your saved architectural designs.', style: TextStyle(fontSize: 14, color: Color(0xFF64748B))),
              const SizedBox(height: 32),
              
              _buildDesignCard('Approved Design v1', 'Approved', '3 Beds • 1 Baths • 1 Flrs', 'assets/placeholder_design.png'),
              const SizedBox(height: 16),
              _buildDesignCard('Modern Minimalist Idea', 'Draft', '2 Beds • 2 Baths • 2 Flrs', 'assets/placeholder_design.png'),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDesignCard(String title, String status, String details, String imagePath) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        children: [
          Container(
            height: 160,
            decoration: const BoxDecoration(
              color: Color(0xFFE2E8F0),
              borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
            ),
            child: const Center(child: Icon(Icons.architecture, size: 48, color: Color(0xFF94A3B8))),
          ),
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Color(0xFF0F172A))),
                    const SizedBox(height: 4),
                    Text(details, style: const TextStyle(fontSize: 14, color: Color(0xFF64748B))),
                  ],
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: status == 'Approved' ? const Color(0xFFECFDF5) : const Color(0xFFF1F5F9),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    status,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: status == 'Approved' ? const Color(0xFF059669) : const Color(0xFF475569),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
