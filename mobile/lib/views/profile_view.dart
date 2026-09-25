import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/auth_provider.dart';
import '../providers/workflow_provider.dart';
import '../core/theme/app_tokens.dart';

class ProfileView extends ConsumerWidget {
  const ProfileView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final email = authState.value?.email ?? 'User';
    final fallbackName = email.split('@').first;
    final displayName = authState.value?.fullName ?? (fallbackName[0].toUpperCase() + fallbackName.substring(1));
    final myDesignsState = ref.watch(myDesignsProvider);

    return Scaffold(
      backgroundColor: AppTokens.bg,
      appBar: AppBar(
        backgroundColor: AppTokens.bg,
        elevation: 0,
        title: const Text('Profile', style: TextStyle(fontWeight: FontWeight.w800, color: AppTokens.ink)),
        centerTitle: false,
        actions: [
          IconButton(
            icon: const Icon(Icons.logout, color: AppTokens.ink),
            onPressed: () => ref.read(authProvider.notifier).logout(),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
        child: Column(
          children: [
            // User Card
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                border: Border.all(color: AppTokens.line),
                boxShadow: const [BoxShadow(color: Color(0x140B0B14), blurRadius: 16, offset: Offset(0, 8), spreadRadius: -8)],
              ),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 32,
                    backgroundColor: AppTokens.accentSoft,
                    child: Text(
                      displayName[0],
                      style: const TextStyle(color: AppTokens.accent, fontWeight: FontWeight.bold, fontSize: 28),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(displayName, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 20, color: AppTokens.ink)),
                      Text(email, style: const TextStyle(color: AppTokens.inkSoft, fontSize: 14)),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 32),
            
            // My Designs Section
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('My Designs', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16, color: AppTokens.ink)),
                IconButton(icon: const Icon(Icons.refresh, size: 20), onPressed: () => ref.read(myDesignsProvider.notifier).fetch()),
              ],
            ),
            const Align(
              alignment: Alignment.centerLeft,
              child: Text('Review, compare, and manage every saved version.', style: TextStyle(color: AppTokens.inkMute, fontSize: 13)),
            ),
            const SizedBox(height: 16),
            
            myDesignsState.when(
              loading: () => const Center(child: Padding(padding: EdgeInsets.all(24), child: CircularProgressIndicator())),
              error: (err, stack) => Center(child: Text('Error: $err')),
              data: (projects) {
                if (projects.isEmpty) {
                  return Container(
                    padding: const EdgeInsets.all(32),
                    alignment: Alignment.center,
                    decoration: BoxDecoration(border: Border.all(color: AppTokens.line, style: BorderStyle.solid), borderRadius: BorderRadius.circular(16)),
                    child: const Text('No designs yet', style: TextStyle(fontWeight: FontWeight.bold, color: AppTokens.inkSoft)),
                  );
                }
                return ListView.builder(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: projects.length,
                  itemBuilder: (context, index) {
                    final project = projects[index];
                    final String workflowId = project['workflowId'] ?? '';
                    final String status = project['status'] ?? '';
                    final List<dynamic> designs = project['designs'] ?? [];
                    final String? preferredId = project['preferredHouseDesignId'];
                    final bool isApproved = status == 'approved';
                    
                    return Container(
                      margin: const EdgeInsets.only(bottom: 24),
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
                        border: Border.all(color: AppTokens.line),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            crossAxisAlignment: CrossAxisAlignment.center,
                            children: [
                              Expanded(
                                child: Text('Project ${workflowId.substring(0, 8)}', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16), overflow: TextOverflow.ellipsis),
                              ),
                              if (preferredId != null && status != 'awaiting_architect_review' && !isApproved) 
                                const SizedBox(width: 8),
                              if (preferredId != null && status != 'awaiting_architect_review' && !isApproved) 
                                Flexible(
                                  child: ElevatedButton(
                                    onPressed: () => ref.read(myDesignsProvider.notifier).submitToArchitect(workflowId, preferredId),
                                    style: ElevatedButton.styleFrom(backgroundColor: AppTokens.emerald, foregroundColor: Colors.white, minimumSize: const Size(0, 36), padding: const EdgeInsets.symmetric(horizontal: 8)),
                                    child: const Text('Send Selected', style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold), maxLines: 1, overflow: TextOverflow.ellipsis),
                                  ),
                                ),
                              if (isApproved)
                                const SizedBox(width: 8),
                              if (isApproved)
                                Flexible(
                                  child: Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                    decoration: BoxDecoration(color: AppTokens.emerald.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(12)),
                                    child: const Text('✓ Approved', style: TextStyle(color: AppTokens.emerald, fontSize: 11, fontWeight: FontWeight.bold), maxLines: 1, overflow: TextOverflow.ellipsis),
                                  ),
                                )
                            ],
                          ),
                          const SizedBox(height: 4),
                          Row(
                            children: [
                              Text('${designs.length} saved design(s)', style: const TextStyle(fontSize: 13, color: AppTokens.inkSoft)),
                              const SizedBox(width: 12),
                              Text(preferredId != null ? 'Selected: Version ${designs.firstWhere((d) => d['designId'] == preferredId, orElse: () => {'version': '?'})['version']}' : 'No design selected', style: const TextStyle(fontSize: 13, color: AppTokens.inkSoft)),
                            ],
                          ),
                          const SizedBox(height: 16),
                          
                          SizedBox(
                            height: 280,
                            child: ListView.builder(
                              scrollDirection: Axis.horizontal,
                              itemCount: designs.length,
                              itemBuilder: (context, dIndex) {
                                final design = designs[dIndex];
                                final bool isPreferred = design['designId'] == preferredId;
                                
                                return Container(
                                  width: 240,
                                  margin: const EdgeInsets.only(right: 12),
                                  padding: const EdgeInsets.all(16),
                                  decoration: BoxDecoration(
                                    border: Border.all(color: isPreferred ? AppTokens.emerald : AppTokens.line, width: isPreferred ? 2 : 1),
                                    borderRadius: BorderRadius.circular(16),
                                    color: Colors.white,
                                  ),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Text('VERSION ${design['version']}', style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w800, color: AppTokens.inkMute)),
                                          if (isPreferred)
                                            const Row(children: [
                                              Icon(Icons.check_circle, size: 14, color: AppTokens.emerald),
                                              SizedBox(width: 4),
                                              Text('Selected', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: AppTokens.emerald)),
                                            ]),
                                        ],
                                      ),
                                      const SizedBox(height: 4),
                                      Text(design['topology'] ?? 'Design', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16), maxLines: 1, overflow: TextOverflow.ellipsis),
                                      const SizedBox(height: 12),
                                      Container(
                                        height: 80,
                                        width: double.infinity,
                                        decoration: BoxDecoration(color: const Color(0xFFF1F5F9), borderRadius: BorderRadius.circular(8), border: Border.all(color: const Color(0xFFCBD5E1))),
                                        child: const Center(child: Icon(Icons.architecture, color: AppTokens.inkMute, size: 32)), // Placeholder for MiniPlan
                                      ),
                                      const SizedBox(height: 12),
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                            const Text('Rooms', style: TextStyle(fontSize: 11, color: AppTokens.inkMute)),
                                            Text('${design['bedrooms']} Beds', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
                                          ]),
                                          Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                            const Text('Area', style: TextStyle(fontSize: 11, color: AppTokens.inkMute)),
                                            Text('${design['totalBuiltUpAreaSqft']} sq ft', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
                                          ]),
                                        ],
                                      ),
                                      const Spacer(),
                                      Row(
                                        children: [
                                          Expanded(
                                            child: ElevatedButton(
                                              onPressed: isPreferred 
                                                  ? () => ref.read(myDesignsProvider.notifier).clearSelection(workflowId)
                                                  : () => ref.read(myDesignsProvider.notifier).selectDesign(workflowId, design['designId']),
                                              style: ElevatedButton.styleFrom(
                                                backgroundColor: isPreferred ? AppTokens.inkSoft : AppTokens.accent, 
                                                foregroundColor: Colors.white, 
                                                padding: EdgeInsets.zero, 
                                                minimumSize: const Size(0, 36),
                                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                              ),
                                              child: Text(isPreferred ? 'Unselect' : 'Select', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                );
                              },
                            ),
                          ),
                        ],
                      ),
                    );
                  },
                );
              },
            ),
            const SizedBox(height: 100),
          ],
        ),
      ),
    );
  }
}
