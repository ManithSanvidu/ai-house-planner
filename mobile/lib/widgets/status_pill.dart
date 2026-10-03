import 'package:flutter/material.dart';
import '../core/theme/app_tokens.dart';

class StatusPill extends StatelessWidget {
  final String label;
  final Color backgroundColor;
  final Color textColor;

  const StatusPill({
    super.key,
    required this.label,
    required this.backgroundColor,
    required this.textColor,
  });

  factory StatusPill.approved(String label) => StatusPill(label: label, backgroundColor: AppTokens.statusApprovedBg, textColor: AppTokens.statusApprovedText);
  factory StatusPill.active(String label) => StatusPill(label: label, backgroundColor: AppTokens.statusActiveBg, textColor: AppTokens.statusActiveText);
  factory StatusPill.pending(String label) => StatusPill(label: label, backgroundColor: AppTokens.statusPendingBg, textColor: AppTokens.statusPendingText);
  factory StatusPill.primary(String label) => StatusPill(label: label, backgroundColor: AppTokens.statusPillBg, textColor: AppTokens.statusPillText);
  factory StatusPill.gray(String label) => StatusPill(label: label, backgroundColor: const Color(0xFFF1F5F9), textColor: const Color(0xFF475569));

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(AppTokens.radiusPill),
      ),
      child: Text(
        label.toUpperCase(),
        style: TextStyle(
          color: textColor,
          fontSize: 10,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.5,
        ),
      ),
    );
  }
}
