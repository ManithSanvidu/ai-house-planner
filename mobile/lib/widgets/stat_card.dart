import 'package:flutter/material.dart';
import '../core/theme/app_tokens.dart';

class StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color backgroundColor;
  final Color iconColor;

  const StatCard({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    required this.backgroundColor,
    required this.iconColor,
  });

  factory StatCard.approved(String label, String value, IconData icon) => StatCard(label: label, value: value, icon: icon, backgroundColor: AppTokens.statusApprovedBg, iconColor: AppTokens.statusApprovedText);
  factory StatCard.active(String label, String value, IconData icon) => StatCard(label: label, value: value, icon: icon, backgroundColor: AppTokens.statusActiveBg, iconColor: AppTokens.statusActiveText);
  factory StatCard.pending(String label, String value, IconData icon) => StatCard(label: label, value: value, icon: icon, backgroundColor: AppTokens.statusPendingBg, iconColor: AppTokens.statusPendingText);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppTokens.card,
        borderRadius: BorderRadius.circular(AppTokens.radiusContainer),
        border: Border.all(color: AppTokens.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: backgroundColor,
              borderRadius: BorderRadius.circular(AppTokens.radiusIconTile),
            ),
            child: Icon(icon, color: iconColor, size: 20),
          ),
          const SizedBox(height: 16),
          Text(value, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w700, color: AppTokens.textPrimary)),
          const SizedBox(height: 4),
          Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppTokens.textSecondary)),
        ],
      ),
    );
  }
}
