import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../core/theme/app_tokens.dart';
import '../core/network/api_client.dart';
import '../widgets/app_card.dart';
import '../widgets/app_buttons.dart';
import '../widgets/status_pill.dart';

class MyDesignsView extends ConsumerStatefulWidget {
  const MyDesignsView({super.key});

  @override
  ConsumerState<MyDesignsView> createState() => _MyDesignsViewState();
}

class _MyDesignsViewState extends ConsumerState<MyDesignsView> {
  bool _isLoading = true;
  String _error = '';
  List<dynamic> _workflows = [];

  @override
  void initState() {
    super.initState();
    _loadDesigns();
  }

  Future<void> _loadDesigns() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final res = await ApiClient.instance.get('/workflows/designs');
      setState(() {
        _workflows = res.data as List<dynamic>;
      });
    } catch (e) {
      setState(() => _error = 'Failed to load designs.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _sendToArchitect(String workflowId, String designId) async {
    try {
      await ApiClient.instance.post('/workflows/$workflowId/submit-architect-review/$designId');
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Design sent to architect for review.')));
      _loadDesigns();
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to send for review.')));
    }
  }

  Future<void> _deleteDesign(String workflowId, String designId) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Delete Design'),
        content: const Text('Are you sure you want to delete this design?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.pop(c, true), child: const Text('Delete', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirm != true) return;

    try {
      await ApiClient.instance.delete('/workflows/$workflowId/designs/$designId');
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Design deleted.')));
      _loadDesigns();
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to delete design.')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTokens.bg,
      body: SafeArea(
        child: CustomScrollView(
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 24.0),
              sliver: SliverList(
                delegate: SliverChildListDelegate([
                  // Header
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('My Designs', style: Theme.of(context).textTheme.headlineSmall),
                            const SizedBox(height: 4),
                            const Text('Review and manage your saved house designs.', style: TextStyle(fontSize: 14, color: AppTokens.textSecondary, fontWeight: FontWeight.w500)),
                          ],
                        ),
                      ),
                      const SizedBox(width: 16),
                      Container(
                        decoration: BoxDecoration(color: Colors.white, border: Border.all(color: AppTokens.line), borderRadius: BorderRadius.circular(12)),
                        child: IconButton(icon: const Icon(Icons.refresh, size: 20, color: AppTokens.textPrimary), onPressed: _loadDesigns),
                      )
                    ],
                  ),
                  const SizedBox(height: 24),

                  if (_isLoading)
                    const Center(child: Padding(padding: EdgeInsets.all(32.0), child: CircularProgressIndicator()))
                  else if (_error.isNotEmpty)
                    Center(child: Column(children: [Text(_error, style: const TextStyle(color: Colors.red)), PrimaryButton(label: 'Retry', onPressed: _loadDesigns)]))
                  else if (_workflows.isEmpty)
                    const Center(child: Padding(padding: EdgeInsets.all(32.0), child: Text('No saved designs yet.', style: TextStyle(color: AppTokens.textSecondary))))
                  else
                    ..._workflows.map((wf) {
                      final workflowId = wf['workflowId'];
                      final designs = wf['designs'] as List<dynamic>? ?? [];
                      if (designs.isEmpty) return const SizedBox();

                      // Take the first or current design to display in this list
                      final d = designs.firstWhere((x) => x['isCurrent'] == true, orElse: () => designs.first);
                      
                      final designId = d['designId'];
                      final version = d['version'].toString();
                      final isArchitectApproved = d['isArchitectApproved'] == true;
                      
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 24.0),
                        child: _buildDesignCard(
                          context: context,
                          workflowId: workflowId,
                          designId: designId,
                          version: version,
                          title: d['generationMode'] == 'template' ? 'Template Based' : 'Custom AI Generated',
                          bedrooms: d['bedrooms'].toString(),
                          bathrooms: d['bathrooms'].toString(),
                          floors: d['floorCount'].toString(),
                          area: '${d['totalBuiltUpAreaSqft']} sq ft',
                          isReady: true,
                          isArchitectApproved: isArchitectApproved,
                          onOpen: () => context.push('/design/$workflowId/preview?designId=$designId'),
                          onSend: () => _sendToArchitect(workflowId, designId),
                          onDelete: () => _deleteDesign(workflowId, designId),
                          onReadiness: () => context.push('/readiness'), // Navigates to readiness planner
                        ),
                      );
                    }),
                ]),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDesignCard({
    required BuildContext context,
    required String workflowId,
    required String designId,
    required String version,
    required String title,
    required String bedrooms,
    required String bathrooms,
    required String floors,
    required String area,
    required bool isReady,
    required bool isArchitectApproved,
    required VoidCallback onOpen,
    required VoidCallback onSend,
    required VoidCallback onDelete,
    required VoidCallback onReadiness,
  }) {
    return AppCard(
      padding: const EdgeInsets.all(0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.all(20.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('House Design Project', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTokens.textPrimary)),
                const SizedBox(height: 8),
                Row(
                  children: [
                    const Text('1 Design', style: TextStyle(color: AppTokens.textSecondary, fontSize: 13, fontWeight: FontWeight.w500)),
                    const SizedBox(width: 8),
                    if (isArchitectApproved)
                      StatusPill.approved('Architect Approved')
                    else if (isReady)
                      StatusPill.gray('Ready for Your Review')
                    else
                      StatusPill.pending('Draft'),
                  ],
                ),
              ],
            ),
          ),
          
          Padding(
            padding: const EdgeInsets.only(left: 20.0, right: 20.0, bottom: 20.0),
            child: Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppTokens.line, width: 0.5),
                boxShadow: const [BoxShadow(color: Color(0x05000000), blurRadius: 10, offset: Offset(0, 4))],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('VERSION $version', style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: AppTokens.textSecondary, letterSpacing: 0.5)),
                  const SizedBox(height: 2),
                  Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTokens.textPrimary)),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(child: Text('$bedrooms Bedroom', style: const TextStyle(color: AppTokens.textPrimary, fontWeight: FontWeight.w500, fontSize: 13))),
                      Expanded(child: Text('$bathrooms Bathroom', style: const TextStyle(color: AppTokens.textPrimary, fontWeight: FontWeight.w500, fontSize: 13))),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(child: Text('$floors Floor', style: const TextStyle(color: AppTokens.textPrimary, fontWeight: FontWeight.w500, fontSize: 13))),
                      Expanded(child: Text(area, style: const TextStyle(color: AppTokens.textPrimary, fontWeight: FontWeight.w500, fontSize: 13))),
                    ],
                  ),
                  const SizedBox(height: 16),
                  const Text('Generated design', style: TextStyle(color: AppTokens.textSecondary, fontSize: 13)),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(child: PrimaryButton(label: 'Open Design', onPressed: onOpen)),
                      const SizedBox(width: 8),
                      Expanded(
                        child: isArchitectApproved 
                          ? SecondaryButton(label: 'Approved', onPressed: null)
                          : PrimaryButton(label: 'Send to Architect', onPressed: onSend),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      ElevatedButton(
                        onPressed: onReadiness,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTokens.statusPillBg,
                          foregroundColor: AppTokens.statusPillText,
                          elevation: 0,
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                          minimumSize: Size.zero,
                        ),
                        child: const Text('Readiness Planner', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 12)),
                      ),
                      TextButton(
                        onPressed: onDelete,
                        style: TextButton.styleFrom(
                          foregroundColor: Colors.red,
                          minimumSize: Size.zero,
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        ),
                        child: const Text('Delete', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                      ),
                    ],
                  )
                ],
              ),
            ),
          )
        ],
      ),
    );
  }
}
