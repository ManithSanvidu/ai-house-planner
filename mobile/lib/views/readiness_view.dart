import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/network/api_client.dart';

class ReadinessView extends ConsumerStatefulWidget {
  const ReadinessView({super.key});

  @override
  ConsumerState<ReadinessView> createState() => _ReadinessViewState();
}

class _ReadinessViewState extends ConsumerState<ReadinessView> {
  bool _isLoading = true;
  String _error = '';
  String? _projectId;
  List<dynamic> _availableProjects = [];
  Map<String, dynamic>? _readinessPlan;
  List<dynamic> _materials = [];
  bool _isGenerating = false;
  String _activeTab = 'overview'; // overview, materials, procurement, optimizer

  @override
  void initState() {
    super.initState();
    _initProject();
  }

  Future<void> _initProject() async {
    try {
      final res = await ApiClient.instance.get('/customer/construction');
      final activeProjects = res.data['activeProjects'] as List<dynamic>? ?? [];
      
      final projects = activeProjects.map((p) => {'id': p['id'], 'name': 'Project ${p['id'].toString().substring(0,8)}'}).toList();
      
      setState(() {
        _availableProjects = projects;
        if (projects.isNotEmpty) {
          _projectId = projects[0]['id'];
        }
      });
      if (_projectId != null) {
        await _fetchMaterials();
      }
    } catch (e) {
      if (mounted) setState(() => _error = 'Failed to load projects.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _fetchMaterials() async {
    if (_projectId == null) return;
    try {
      final res = await ApiClient.instance.get('/readiness/$_projectId/materials');
      setState(() => _materials = res.data as List<dynamic>);
    } catch (e) {
      // Ignore
    }
  }

  Future<void> _generatePlan() async {
    if (_projectId == null || _isGenerating) return;
    setState(() => _isGenerating = true);
    try {
      final res = await ApiClient.instance.post('/readiness/$_projectId/plan');
      setState(() {
        _readinessPlan = res.data;
        _activeTab = 'optimizer';
      });
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to generate plan.')));
    } finally {
      if (mounted) setState(() => _isGenerating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    if (_error.isNotEmpty) return Scaffold(body: Center(child: Text(_error)));

    final readinessPercent = _readinessPlan?['final_recommendation']?['status'] == 'Ready' ? 100 : (_readinessPlan != null ? 68 : 0);

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Construction Readiness', style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Color(0xFF0F172A))),
              const SizedBox(height: 8),
              const Text('AI-powered material & task planning', style: TextStyle(fontSize: 14, color: Color(0xFF64748B))),
              const SizedBox(height: 24),

              if (_availableProjects.isNotEmpty)
                DropdownButtonFormField<String>(
                  value: _projectId,
                  decoration: const InputDecoration(labelText: 'Select Project/Design', border: OutlineInputBorder()),
                  items: _availableProjects.map((p) => DropdownMenuItem<String>(value: p['id'], child: Text(p['name']))).toList(),
                  onChanged: (val) {
                    setState(() {
                      _projectId = val;
                      _readinessPlan = null;
                    });
                    _fetchMaterials();
                  },
                )
              else
                const Text('No active projects available.'),

              const SizedBox(height: 24),
              
              // TABS
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Row(
                  children: [
                    _buildTab('Overview', 'overview', Icons.bar_chart),
                    _buildTab('Materials', 'materials', Icons.inventory),
                    _buildTab('Procurement', 'procurement', Icons.local_shipping),
                    _buildTab('Optimizer', 'optimizer', Icons.bolt),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              if (_activeTab == 'overview') ...[
                Container(
                  padding: const EdgeInsets.all(24),
                  decoration: BoxDecoration(color: Colors.blue.shade50, borderRadius: BorderRadius.circular(16)),
                  child: Column(
                    children: [
                      const Text('Project Readiness', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.blue)),
                      const SizedBox(height: 12),
                      Text('$readinessPercent%', style: const TextStyle(fontSize: 48, fontWeight: FontWeight.bold, color: Colors.blue)),
                      LinearProgressIndicator(value: readinessPercent / 100, minHeight: 12, backgroundColor: Colors.blue.shade100, color: Colors.blue),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
                ElevatedButton.icon(
                  onPressed: _isGenerating ? null : _generatePlan,
                  icon: const Icon(Icons.bolt),
                  label: Text(_isGenerating ? 'Analyzing...' : 'Generate Acceleration Plan'),
                ),
              ]
              else if (_activeTab == 'materials') ...[
                const Text('Materials Inventory', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                const SizedBox(height: 12),
                if (_materials.isEmpty) const Text('No materials found.')
                else ..._materials.map((m) => ListTile(
                  title: Text(m['materialName']),
                  subtitle: Text('Req: ${m['requiredQuantity']} | Avail: ${m['availableQuantity']}'),
                  trailing: Text((m['requiredQuantity'] - m['availableQuantity'] - (m['orderedQuantity'] ?? 0)) > 0 ? 'Shortage' : 'Ready', style: TextStyle(color: (m['requiredQuantity'] - m['availableQuantity'] - (m['orderedQuantity'] ?? 0)) > 0 ? Colors.red : Colors.green)),
                )),
              ]
              else if (_activeTab == 'procurement') ...[
                const Text('Procurement Priority', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                const SizedBox(height: 12),
                if (_readinessPlan?['procurement_priorities'] != null)
                  ...(_readinessPlan!['procurement_priorities'] as List).map((p) => ListTile(
                    title: Text(p['item']),
                    subtitle: Text(p['priority']),
                    trailing: Text(p['status']),
                  ))
                else
                  const Text('Run the AI planner to generate procurement priorities.'),
              ]
              else if (_activeTab == 'optimizer') ...[
                const Text('AI Acceleration Plan', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                const SizedBox(height: 12),
                if (_readinessPlan?['acceleration_opportunities'] != null)
                  if (_readinessPlan!['acceleration_opportunities'] is Map)
                    ...(_readinessPlan!['acceleration_opportunities']['actions'] as List).map((a) => ListTile(
                      leading: const Icon(Icons.check_circle, color: Colors.purple),
                      title: Text(a),
                    ))
                  else
                    ...(_readinessPlan!['acceleration_opportunities'] as List).map((a) => ListTile(
                      leading: const Icon(Icons.check_circle, color: Colors.purple),
                      title: Text(a),
                    ))
                else
                  ElevatedButton(onPressed: _generatePlan, child: const Text('Generate Acceleration Plan')),
              ]
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTab(String label, String id, IconData icon) {
    final isActive = _activeTab == id;
    return GestureDetector(
      onTap: () => setState(() => _activeTab = id),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        margin: const EdgeInsets.only(right: 8),
        decoration: BoxDecoration(
          color: isActive ? Colors.white : Colors.transparent,
          border: Border.all(color: isActive ? Colors.blue : Colors.transparent),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          children: [
            Icon(icon, color: isActive ? Colors.blue : Colors.grey, size: 18),
            const SizedBox(width: 8),
            Text(label, style: TextStyle(color: isActive ? Colors.blue : Colors.grey, fontWeight: FontWeight.bold)),
          ],
        ),
      ),
    );
  }
}
