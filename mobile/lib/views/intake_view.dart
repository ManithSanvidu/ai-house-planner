import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/intake_provider.dart';
import '../models/land_submission.dart';

class IntakeView extends ConsumerStatefulWidget {
  const IntakeView({super.key});

  @override
  ConsumerState<IntakeView> createState() => _IntakeViewState();
}

class _IntakeViewState extends ConsumerState<IntakeView> {
  final _formKey = GlobalKey<FormState>();
  int _currentStep = 1;
  final TextEditingController _durationController = TextEditingController();

  @override
  void dispose() {
    _durationController.dispose();
    super.dispose();
  }

  void _submit() async {
    final data = ref.read(intakeProvider).value;
    if (data == null || !data.isValid) return;

    try {
      final workflowId = await ref.read(intakeProvider.notifier).submitIntake();
      if (workflowId != null && mounted) {
        context.go('/design/$workflowId');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Failed to generate plan: $e',
              style: const TextStyle(color: Colors.white),
            ),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  void _nextStep() {
    final data = ref.read(intakeProvider).value;
    if (data == null) return;
    bool canProceed = false;

    if (_currentStep == 1) {
      canProceed = data.landSizeCategory != null;
    } else if (_currentStep == 2) {
      canProceed = data.preferredBedrooms != null && data.preferredBathrooms != null;
    } else if (_currentStep == 3) {
      canProceed = data.stylePreference != null;
    }

    if (canProceed) {
      if (_currentStep < 3) {
        setState(() => _currentStep++);
      } else {
        _submit();
      }
    }
  }

  void _prevStep() {
    if (_currentStep > 1) {
      setState(() => _currentStep--);
    }
  }

  @override
  Widget build(BuildContext context) {
    final intakeState = ref.watch(intakeProvider);
    final data = intakeState.value ?? LandSubmission();

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        backgroundColor: const Color(0xFFF8FAFC),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => context.go('/dashboard'),
        ),
      ),
      body: intakeState.when(
        loading: () => const Center(
          child: CircularProgressIndicator(color: Colors.blue),
        ),
        error: (err, stack) => Center(child: Text('Error: $err')),
        data: (data) => SafeArea(
          child: Container(
            margin: const EdgeInsets.only(left: 16, right: 16, top: 8, bottom: 16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(24),
              border: Border.all(color: const Color(0xFFE2E8F0)),
              boxShadow: const [
                BoxShadow(color: Color(0x0A000000), blurRadius: 10, offset: Offset(0, 4)),
              ],
            ),
            child: Column(
              children: [
                _buildStepIndicator(),
                Expanded(
                  child: Form(
                    key: _formKey,
                    child: ListView(
                      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
                      physics: const BouncingScrollPhysics(),
                      children: [
                        if (_currentStep == 1) _buildStep1(data),
                        if (_currentStep == 2) _buildStep2(data),
                        if (_currentStep == 3) _buildStep3(data),
                        const SizedBox(height: 24),
                      ],
                    ),
                  ),
                ),
                _buildFooter(intakeState.isLoading, data),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildStepIndicator() {
    return Padding(
      padding: const EdgeInsets.only(top: 32, bottom: 24, left: 24, right: 24),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
        children: [
          _buildStepCircle(1),
          _buildStepCircle(2),
          _buildStepCircle(3),
        ],
      ),
    );
  }

  Widget _buildStepCircle(int stepNum) {
    final isActive = _currentStep == stepNum;
    final isPast = _currentStep > stepNum;

    return Container(
      width: 40,
      height: 40,
      decoration: BoxDecoration(
        color: isActive || isPast ? const Color(0xFF2563EB) : Colors.white,
        shape: BoxShape.circle,
        border: Border.all(
          color: isActive || isPast ? const Color(0xFF2563EB) : const Color(0xFFE2E8F0),
          width: 1,
        ),
      ),
      child: Center(
        child: isPast
            ? const Icon(Icons.check, size: 20, color: Colors.white)
            : Text(
                '$stepNum',
                style: TextStyle(
                  color: isActive ? Colors.white : const Color(0xFF64748B),
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                ),
              ),
      ),
    );
  }

  Widget _buildFooter(bool isSubmitting, LandSubmission data) {
    bool canProceed = false;
    if (_currentStep == 1) canProceed = data.landSizeCategory != null;
    if (_currentStep == 2) canProceed = data.preferredBedrooms != null && data.preferredBathrooms != null;
    if (_currentStep == 3) canProceed = data.stylePreference != null;

    return Container(
      padding: const EdgeInsets.all(24),
      decoration: const BoxDecoration(
        border: Border(top: BorderSide(color: Color(0xFFE2E8F0))),
        borderRadius: BorderRadius.only(
          bottomLeft: Radius.circular(24),
          bottomRight: Radius.circular(24),
        ),
      ),
      child: Row(
        children: [
          if (_currentStep > 1)
            Expanded(
              flex: 1,
              child: OutlinedButton(
                onPressed: isSubmitting ? null : _prevStep,
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  side: const BorderSide(color: Color(0xFFE2E8F0), width: 2),
                ),
                child: const Text('Back', style: TextStyle(color: Color(0xFF64748B), fontWeight: FontWeight.bold, fontSize: 16)),
              ),
            ),
          if (_currentStep > 1) const SizedBox(width: 16),
          Expanded(
            flex: 2,
            child: ElevatedButton(
              onPressed: isSubmitting || !canProceed ? null : _nextStep,
              style: ElevatedButton.styleFrom(
                backgroundColor: _currentStep == 3 ? const Color(0xFF16A34A) : const Color(0xFF0F172A),
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    _currentStep == 3 ? (isSubmitting ? 'Generating...' : 'Generate AI Plan') : 'Next Step',
                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                  ),
                  if (_currentStep < 3) ...[
                    const SizedBox(width: 4),
                    const Icon(Icons.chevron_right, size: 20),
                  ]
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStep1(LandSubmission data) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'How big is your land?',
          style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: Color(0xFF0F172A)),
        ),
        const SizedBox(height: 8),
        const Text(
          'Choose a supported single-floor residential plot size.',
          style: TextStyle(fontSize: 16, color: Color(0xFF475569)),
        ),
        const SizedBox(height: 32),
        _buildSelectionCard(
          title: 'Small Plot',
          subtitle: '10 - 20 perches',
          description: 'Compact family home',
          isSelected: data.landSizeCategory == 'small',
          onTap: () => ref.read(intakeProvider.notifier).updateField(landSizeCategory: 'small'),
        ),
        const SizedBox(height: 16),
        _buildSelectionCard(
          title: 'Medium Plot',
          subtitle: '20 - 35 perches',
          description: 'Comfortable family home',
          isSelected: data.landSizeCategory == 'medium',
          onTap: () => ref.read(intakeProvider.notifier).updateField(landSizeCategory: 'medium'),
        ),
      ],
    );
  }

  Widget _buildStep2(LandSubmission data) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'How many rooms does your family need?',
          style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: Color(0xFF0F172A)),
        ),
        const SizedBox(height: 8),
        const Text(
          'Choose the bedroom and bathroom counts for your single-floor home.',
          style: TextStyle(fontSize: 16, color: Color(0xFF475569)),
        ),
        const SizedBox(height: 32),
        Row(
          children: [
            Expanded(
              child: _buildOptionButton(
                label: '1 Bedroom',
                isSelected: data.preferredBedrooms == 1,
                onTap: () {
                  ref.read(intakeProvider.notifier).updateField(preferredBedrooms: 1);
                  if (data.preferredBathrooms == null) {
                    ref.read(intakeProvider.notifier).updateField(preferredBathrooms: 1);
                  }
                },
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: _buildOptionButton(
                label: '2 Bedrooms',
                isSelected: data.preferredBedrooms == 2,
                onTap: () => ref.read(intakeProvider.notifier).updateField(preferredBedrooms: 2),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: _buildOptionButton(
                label: '3 Bedrooms',
                isSelected: data.preferredBedrooms == 3,
                onTap: () => ref.read(intakeProvider.notifier).updateField(preferredBedrooms: 3),
              ),
            ),
          ],
        ),
        const SizedBox(height: 32),
        const Text(
          'How many bathrooms do you need?',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Color(0xFF0F172A)),
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _buildOptionButton(
                label: data.preferredBedrooms == 1 ? '1 Bathroom\n(Recommended)' : '1 Bathroom',
                isSelected: data.preferredBathrooms == 1,
                onTap: () => ref.read(intakeProvider.notifier).updateField(preferredBathrooms: 1),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: _buildOptionButton(
                label: '2 Bathrooms',
                isSelected: data.preferredBathrooms == 2,
                onTap: () => ref.read(intakeProvider.notifier).updateField(preferredBathrooms: 2),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildStep3(LandSubmission data) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Choose your home style',
          style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: Color(0xFF0F172A)),
        ),
        const SizedBox(height: 8),
        const Text(
          'Choose one of the supported family-home styles.',
          style: TextStyle(fontSize: 16, color: Color(0xFF475569)),
        ),
        const SizedBox(height: 32),
        _buildStyleCard(
          title: 'Simple Family Home',
          description: 'Practical single-floor home with essential spaces: Living room, kitchen, dining area, bedrooms and bathrooms.',
          isSelected: data.stylePreference == 'simple',
          onTap: () => ref.read(intakeProvider.notifier).updateField(stylePreference: 'simple'),
        ),
        const SizedBox(height: 16),
        _buildStyleCard(
          title: 'Modern Family Home',
          description: 'Comfortable modern single-floor home with an open living area, modern kitchen, better room spacing and natural lighting.',
          isSelected: data.stylePreference == 'modern',
          onTap: () => ref.read(intakeProvider.notifier).updateField(stylePreference: 'modern'),
        ),
        const SizedBox(height: 32),
        const Text(
          'Target Construction Duration (Optional)',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Color(0xFF0F172A)),
        ),
        const SizedBox(height: 8),
        const Text(
          'Let us know if you have a specific timeline in days.',
          style: TextStyle(fontSize: 14, color: Color(0xFF475569)),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _durationController,
          keyboardType: TextInputType.number,
          decoration: InputDecoration(
            hintText: 'e.g. 120',
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: Color(0xFF3B82F6), width: 2),
            ),
            filled: true,
            fillColor: const Color(0xFFF8FAFC),
          ),
          onChanged: (val) {
            final days = int.tryParse(val);
            ref.read(intakeProvider.notifier).updateField(targetDurationDays: days);
          },
        ),
      ],
    );
  }

  Widget _buildSelectionCard({
    required String title,
    required String subtitle,
    required String description,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: isSelected ? const Color(0xFFEFF6FF) : Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isSelected ? const Color(0xFF3B82F6) : const Color(0xFFE2E8F0),
            width: isSelected ? 2 : 1,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF0F172A))),
            const SizedBox(height: 4),
            Text(subtitle, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: Color(0xFF2563EB))),
            const SizedBox(height: 8),
            Text(description, style: const TextStyle(fontSize: 14, color: Color(0xFF64748B))),
          ],
        ),
      ),
    );
  }

  Widget _buildStyleCard({
    required String title,
    required String description,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: isSelected ? const Color(0xFFEFF6FF) : Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isSelected ? const Color(0xFF3B82F6) : const Color(0xFFE2E8F0),
            width: isSelected ? 2 : 1,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF0F172A))),
            const SizedBox(height: 8),
            Text(description, style: const TextStyle(fontSize: 14, color: Color(0xFF64748B))),
          ],
        ),
      ),
    );
  }

  Widget _buildOptionButton({
    required String label,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 8),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: isSelected ? const Color(0xFFEFF6FF) : Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: isSelected ? const Color(0xFF3B82F6) : const Color(0xFFE2E8F0),
            width: isSelected ? 2 : 1,
          ),
        ),
        child: Text(
          label,
          textAlign: TextAlign.center,
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
            color: isSelected ? const Color(0xFF1D4ED8) : const Color(0xFF0F172A),
          ),
        ),
      ),
    );
  }
}
