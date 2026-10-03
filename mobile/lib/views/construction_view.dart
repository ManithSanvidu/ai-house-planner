import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../core/network/api_client.dart';

class ConstructionView extends ConsumerStatefulWidget {
  const ConstructionView({super.key});

  @override
  ConsumerState<ConstructionView> createState() => _ConstructionViewState();
}

class _ConstructionViewState extends ConsumerState<ConstructionView> {
  bool _isLoading = true;
  String _error = '';
  Map<String, dynamic>? _overview;
  List<dynamic> _designs = [];
  List<dynamic> _constructors = [];
  String? _selectedDesign;
  String? _message;
  bool _isRequesting = false;
  String? _requestingConstructor;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final overviewRes = await ApiClient.instance.get('/customer/construction');
      final designsRes = await ApiClient.instance.get('/customer/construction/approved-designs');
      
      final overview = overviewRes.data;
      final designs = designsRes.data as List<dynamic>;
      
      setState(() {
        _overview = overview;
        _designs = designs;
        if (_selectedDesign == null && designs.isNotEmpty) {
          _selectedDesign = designs[0]['designId'];
        }
      });

      // Load constructors in background
      ApiClient.instance.get('/customer/construction/constructors').then((res) {
        if (mounted) {
          setState(() {
            _constructors = res.data as List<dynamic>;
          });
        }
      }).catchError((_) {});
    } catch (e) {
      if (mounted) setState(() => _error = 'Failed to load construction data.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _requestConstructor(String constructorId) async {
    if (_selectedDesign == null || _requestingConstructor != null) return;
    setState(() => _requestingConstructor = constructorId);
    try {
      await ApiClient.instance.post('/customer/construction/requests', data: {
        'houseDesignId': _selectedDesign,
        'constructorId': constructorId,
      });
      setState(() => _message = 'Construction request sent successfully.');
      await _loadData();
    } catch (e) {
      setState(() => _message = 'Could not send construction request.');
    } finally {
      if (mounted) setState(() => _requestingConstructor = null);
    }
  }

  Future<void> _cancelProject(String projectId) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Cancel Construction'),
        content: const Text('Cancel this construction project?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('No')),
          TextButton(onPressed: () => Navigator.pop(c, true), child: const Text('Yes')),
        ],
      )
    );
    if (confirm != true) return;

    try {
      await ApiClient.instance.patch('/customer/construction/projects/$projectId/cancel');
      setState(() => _message = 'Construction project cancelled.');
      await _loadData();
    } catch (e) {
      setState(() => _message = 'Could not cancel project.');
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_error.isNotEmpty) {
      return Scaffold(body: Center(child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text(_error, style: const TextStyle(color: Colors.red)),
          ElevatedButton(onPressed: _loadData, child: const Text('Retry'))
        ],
      )));
    }

    final activeProjects = _overview?['activeProjects'] as List<dynamic>? ?? [];
    final pendingRequests = _overview?['pendingRequests'] as List<dynamic>? ?? [];
    final selectedDesignData = _designs.firstWhere((d) => d['designId'] == _selectedDesign, orElse: () => null);

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Construction', style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Color(0xFF0F172A))),
                      SizedBox(height: 4),
                      Text('Choose a constructor for an approved house design.', style: TextStyle(fontSize: 14, color: Color(0xFF64748B))),
                    ],
                  ),
                ),
                IconButton(icon: const Icon(Icons.refresh), onPressed: _loadData),
              ],
            ),
            const SizedBox(height: 16),
            if (_message != null)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                color: Colors.green.shade100,
                child: Text(_message!, style: TextStyle(color: Colors.green.shade800)),
              ),

            // Active Construction
            if (activeProjects.isNotEmpty) ...[
              const Text('Active Construction', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              const SizedBox(height: 12),
              ...activeProjects.map((p) => _buildActiveProject(p)),
              const SizedBox(height: 24),
            ],

            // Pending Requests
            if (pendingRequests.isNotEmpty) ...[
              const Text('Pending Requests', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              const SizedBox(height: 12),
              ...pendingRequests.map((r) => ListTile(
                title: Text('Waiting for ${r['constructorName']}'),
                subtitle: Text('Requested on ${r['requestedAt'].toString().substring(0,10)}'),
                leading: const Icon(Icons.access_time, color: Colors.orange),
              )),
              const SizedBox(height: 24),
            ],

            // Start Construction section
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(16)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Start Construction', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 16),
                  
                  if (_designs.isEmpty)
                    const Text('No approved designs are available yet.')
                  else ...[
                    // Design selection
                    if (_selectedDesign != null && selectedDesignData != null)
                      Container(
                        padding: const EdgeInsets.all(16),
                        color: Colors.grey.shade50,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Selected Design', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.grey)),
                            Text(selectedDesignData['title'] ?? 'Design', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                            Text('${selectedDesignData['bedrooms']} Bedrooms • ${selectedDesignData['bathrooms']} Bathrooms'),
                            const SizedBox(height: 8),
                            ElevatedButton(
                              onPressed: () => setState(() => _selectedDesign = null),
                              child: const Text('Change design')
                            ),
                          ],
                        ),
                      )
                    else
                      Column(
                        children: _designs.map((d) => ListTile(
                          title: Text(d['title']),
                          subtitle: Text('${d['bedrooms']} Beds • ${d['bathrooms']} Baths'),
                          onTap: () => setState(() => _selectedDesign = d['designId']),
                          trailing: const Text('Select'),
                        )).toList(),
                      ),

                    if (_selectedDesign != null && selectedDesignData != null) ...[
                      const SizedBox(height: 24),
                      const Text('2. Review estimate', style: TextStyle(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      Text('Estimated construction cost: ${selectedDesignData['cost'] != null ? 'LKR ${selectedDesignData['cost']['totalCostLkr']}' : 'Not available'}'),
                      
                      const SizedBox(height: 24),
                      const Text('3. Choose a constructor', style: TextStyle(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      ..._constructors.map((c) => ListTile(
                        title: Text(c['name']),
                        trailing: ElevatedButton(
                          onPressed: _requestingConstructor != null ? null : () => _requestConstructor(c['id']),
                          child: Text(_requestingConstructor == c['id'] ? 'Sending...' : 'Send request'),
                        ),
                      )),
                    ],
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActiveProject(dynamic p) {
    final design = _designs.firstWhere((d) => d['designId'] == p['houseDesignId'], orElse: () => null);
    final title = design != null ? design['title'] : 'Design v${p['designVersion']}';

    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(16), border: Border.all(color: Colors.grey.shade300)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              Container(padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4), decoration: BoxDecoration(color: Colors.indigo.shade50, borderRadius: BorderRadius.circular(8)), child: const Text('In Progress', style: TextStyle(color: Colors.indigo))),
            ],
          ),
          Text(p['constructorName'], style: const TextStyle(color: Colors.grey)),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(12),
            color: Colors.grey.shade50,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Current Phase', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.grey)),
                Text(p['currentPhase'] ?? 'Awaiting update', style: const TextStyle(fontWeight: FontWeight.bold)),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(child: ElevatedButton(onPressed: () => context.go('/dashboard/construction/${p['id']}'), child: const Text('View Progress'))),
              const SizedBox(width: 8),
              TextButton(onPressed: () => _cancelProject(p['id']), child: const Text('Cancel', style: TextStyle(color: Colors.red))),
            ],
          )
        ],
      ),
    );
  }
}
