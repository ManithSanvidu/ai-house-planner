import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/plan_provider.dart';
import '../core/theme/app_tokens.dart';
import '../models/plan.dart';

class PlanLibraryView extends ConsumerStatefulWidget {
  const PlanLibraryView({super.key});

  @override
  ConsumerState<PlanLibraryView> createState() => _PlanLibraryViewState();
}

class _PlanLibraryViewState extends ConsumerState<PlanLibraryView> {
  final _searchController = TextEditingController();

  // Filters State
  String _searchQuery = '';
  String? _bedrooms;
  String? _floors;
  String? _bathrooms;
  String? _style;
  String? _terrain;
  String? _category;
  String _landArea = '';
  String _maxArea = '';
  bool _parking = false;
  bool _office = false;
  bool _balcony = false;
  bool _accessible = false;

  @override
  void initState() {
    super.initState();
    _searchController.addListener(() {
      setState(() {
        _searchQuery = _searchController.text.toLowerCase();
      });
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _showFilterBottomSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (context) {
        return StatefulBuilder(
          builder: (BuildContext context, StateSetter setModalState) {
            return DraggableScrollableSheet(
              initialChildSize: 0.85,
              minChildSize: 0.5,
              maxChildSize: 0.95,
              expand: false,
              builder: (_, controller) {
                return Column(
                  children: [
                    Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('Filters', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTokens.ink)),
                          IconButton(
                            icon: const Icon(Icons.close),
                            onPressed: () => Navigator.pop(context),
                          ),
                        ],
                      ),
                    ),
                    const Divider(height: 1),
                    Expanded(
                      child: ListView(
                        controller: controller,
                        padding: const EdgeInsets.all(24),
                        children: [
                          _buildDropdown('Any bedrooms', ['Any', '1', '2', '3', '4+'], _bedrooms, (v) => setModalState(() => _bedrooms = v == 'Any' ? null : v)),
                          const SizedBox(height: 16),
                          _buildDropdown('Any floors', ['Any', '1', '2', '3+'], _floors, (v) => setModalState(() => _floors = v == 'Any' ? null : v)),
                          const SizedBox(height: 16),
                          _buildDropdown('Any bathrooms', ['Any', '1', '2', '3+'], _bathrooms, (v) => setModalState(() => _bathrooms = v == 'Any' ? null : v)),
                          const SizedBox(height: 16),
                          _buildDropdown('Style', ['Any', 'Modern Minimalist', 'Contemporary', 'Traditional'], _style, (v) => setModalState(() => _style = v == 'Any' ? null : v)),
                          const SizedBox(height: 16),
                          _buildDropdown('Any terrain', ['Any', 'Flat', 'Urban', 'Hillside', 'Coastal'], _terrain, (v) => setModalState(() => _terrain = v == 'Any' ? null : v)),
                          const SizedBox(height: 16),
                          _buildDropdown('Category', ['Any', 'Compact Rectangle', 'L-Shaped', 'Split Zone', 'Standard'], _category, (v) => setModalState(() => _category = v == 'Any' ? null : v)),
                          const SizedBox(height: 16),
                          TextField(
                            decoration: const InputDecoration(labelText: 'Available land (perches)', border: OutlineInputBorder()),
                            keyboardType: TextInputType.number,
                            onChanged: (v) => setModalState(() => _landArea = v),
                          ),
                          const SizedBox(height: 16),
                          TextField(
                            decoration: const InputDecoration(labelText: 'Max built-up area (ft²)', border: OutlineInputBorder()),
                            keyboardType: TextInputType.number,
                            onChanged: (v) => setModalState(() => _maxArea = v),
                          ),
                          const SizedBox(height: 24),
                          CheckboxListTile(title: const Text('Parking'), value: _parking, onChanged: (v) => setModalState(() => _parking = v ?? false)),
                          CheckboxListTile(title: const Text('Office'), value: _office, onChanged: (v) => setModalState(() => _office = v ?? false)),
                          CheckboxListTile(title: const Text('Balcony'), value: _balcony, onChanged: (v) => setModalState(() => _balcony = v ?? false)),
                          CheckboxListTile(title: const Text('Accessible'), value: _accessible, onChanged: (v) => setModalState(() => _accessible = v ?? false)),
                        ],
                      ),
                    ),
                    Padding(
                      padding: const EdgeInsets.all(24),
                      child: SizedBox(
                        width: double.infinity,
                        height: 50,
                        child: ElevatedButton(
                          onPressed: () {
                            setState(() {}); // Apply filters to main view
                            Navigator.pop(context);
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppTokens.ink,
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                          ),
                          child: const Text('Apply Filters', style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold)),
                        ),
                      ),
                    )
                  ],
                );
              },
            );
          }
        );
      },
    );
  }

  Widget _buildDropdown(String label, List<String> items, String? value, ValueChanged<String?> onChanged) {
    return DropdownButtonFormField<String>(
      decoration: InputDecoration(
        labelText: label,
        border: const OutlineInputBorder(),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      ),
      initialValue: value ?? 'Any',
      items: items.map((i) => DropdownMenuItem(value: i, child: Text(i))).toList(),
      onChanged: onChanged,
    );
  }

  List<Plan> _filterPlans(List<Plan> plans) {
    return plans.where((p) {
      if (_searchQuery.isNotEmpty) {
        if (!p.name.toLowerCase().contains(_searchQuery) &&
            !p.style.toLowerCase().contains(_searchQuery) &&
            !p.tags.any((t) => t.toLowerCase().contains(_searchQuery))) {
          return false;
        }
      }
      if (_bedrooms != null && !p.bedrooms.toString().contains(_bedrooms!.replaceAll('+', ''))) return false;
      if (_floors != null && !p.floorCount.toString().contains(_floors!.replaceAll('+', ''))) return false;
      if (_bathrooms != null && !p.bathrooms.toString().contains(_bathrooms!.replaceAll('+', ''))) return false;
      if (_style != null && p.style != _style) return false;
      if (_terrain != null && p.suitableTerrain != _terrain) return false;
      if (_category != null && p.category != _category) return false;
      
      if (_landArea.isNotEmpty) {
        final area = int.tryParse(_landArea);
        if (area != null && p.minimumLandSizePerches > area) return false;
      }
      if (_maxArea.isNotEmpty) {
        final area = int.tryParse(_maxArea);
        if (area != null && p.totalBuiltUpAreaSqft > area) return false;
      }
      
      if (_parking && p.parkingSpaces == 0) return false;
      if (_office && !p.hasOffice) return false;
      if (_balcony && !p.hasBalcony) return false;
      if (_accessible && !p.isAccessibleFriendly) return false;
      
      return true;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final plansAsyncValue = ref.watch(plansProvider);

    return Scaffold(
      backgroundColor: AppTokens.bg,
      appBar: AppBar(
        backgroundColor: AppTokens.bg,
        elevation: 0,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('CONCEPTUAL PLAN LIBRARY', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, letterSpacing: 1.0, color: AppTokens.accent)),
            Text('Pre-designed plans', style: TextStyle(fontSize: 23, fontWeight: FontWeight.w800, color: AppTokens.ink)),
          ],
        ),
      ),
      body: Column(
        children: [
          // Search & Filter Bar
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 8, 24, 16),
            child: Row(
              children: [
                Expanded(
                  child: Container(
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                      border: Border.all(color: AppTokens.line),
                    ),
                    child: TextField(
                      controller: _searchController,
                      style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
                      decoration: const InputDecoration(
                        hintText: 'Search name, style or tag',
                        hintStyle: TextStyle(color: AppTokens.inkMute, fontSize: 14.5),
                        border: InputBorder.none,
                        prefixIcon: Icon(Icons.search, color: AppTokens.inkMute, size: 20),
                        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                InkWell(
                  onTap: _showFilterBottomSheet,
                  borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                  child: Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(AppTokens.radiusPill),
                      border: Border.all(color: AppTokens.line),
                    ),
                    child: const Icon(Icons.tune, color: AppTokens.ink, size: 20),
                  ),
                )
              ],
            ),
          ),
          
          // Grid View
          Expanded(
            child: plansAsyncValue.when(
              loading: () => const Center(child: CircularProgressIndicator(color: AppTokens.ink)),
              error: (err, stack) => Center(child: Text('Error: $err')),
              data: (allPlans) {
                final plans = _filterPlans(allPlans);
                
                if (plans.isEmpty) {
                  return const Center(child: Text('No plans match your filters.', style: TextStyle(color: AppTokens.inkMute)));
                }
                
                return ListView.separated(
                  padding: const EdgeInsets.fromLTRB(24, 0, 24, 100),
                  itemCount: plans.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 16),
                  itemBuilder: (context, index) {
                    final plan = plans[index];
                    return InkWell(
                      onTap: () => context.go('/plans/${plan.id}'),
                      borderRadius: BorderRadius.circular(16),
                      child: Container(
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppTokens.line),
                          boxShadow: const [
                            BoxShadow(
                              color: Color(0x0A000000),
                              blurRadius: 10,
                              offset: Offset(0, 4),
                            ),
                          ],
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            // Top Banner Section
                            Container(
                              padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
                              decoration: const BoxDecoration(
                                borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
                                gradient: LinearGradient(
                                  colors: [Color(0xFFF5F3FF), Color(0xFFF0FDF4)], // Very light purple to green gradient
                                  begin: Alignment.topLeft,
                                  end: Alignment.bottomRight,
                                ),
                              ),
                              child: Column(
                                children: [
                                  const Icon(Icons.home_outlined, color: Color(0xFF4F46E5), size: 36),
                                  const SizedBox(height: 8),
                                  Text(
                                    '${plan.bedrooms} Bedrooms',
                                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: AppTokens.ink),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    '${plan.floorCount} Floor',
                                    style: const TextStyle(color: AppTokens.inkSoft, fontSize: 14),
                                  ),
                                  const SizedBox(height: 8),
                                  Text(
                                    plan.category,
                                    style: const TextStyle(color: Color(0xFF4F46E5), fontSize: 13, fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ),
                            ),
                            // Details Section
                            Padding(
                              padding: const EdgeInsets.all(16.0),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    plan.name,
                                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 20, color: AppTokens.ink),
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    plan.style,
                                    style: const TextStyle(color: AppTokens.inkSoft, fontSize: 14),
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  const SizedBox(height: 16),
                                  Row(
                                    children: [
                                      Expanded(child: _buildGridItem('🛏', '${plan.bedrooms} Bedrooms')),
                                      Expanded(child: _buildGridItem('🛁', '${plan.bathrooms} Bathroom')),
                                    ],
                                  ),
                                  const SizedBox(height: 12),
                                  Row(
                                    children: [
                                      Expanded(child: _buildGridItem('▤', '${plan.floorCount} Floor')),
                                      Expanded(child: _buildGridItem('📏', '${plan.totalBuiltUpAreaSqft.round()} sq ft')),
                                    ],
                                  ),
                                  const SizedBox(height: 20),
                                  const Divider(color: AppTokens.line, height: 1),
                                  const SizedBox(height: 16),
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: [
                                      Text(
                                        plan.category,
                                        style: const TextStyle(color: AppTokens.inkSoft, fontSize: 14),
                                      ),
                                      const Text(
                                        'View Plan',
                                        style: TextStyle(
                                          color: Color(0xFF4F46E5),
                                          fontWeight: FontWeight.bold,
                                          fontSize: 14,
                                          decoration: TextDecoration.underline,
                                          decorationColor: Color(0xFF4F46E5),
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGridItem(String icon, String text) {
    return Row(
      children: [
        Text(icon, style: const TextStyle(fontSize: 16)),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            text,
            style: const TextStyle(color: AppTokens.ink, fontSize: 14, fontWeight: FontWeight.w500),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}
