import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import '../providers/intake_provider.dart';
import '../models/land_submission.dart';
import 'package:go_router/go_router.dart';
import '../core/theme/app_tokens.dart';

class IntakeView extends ConsumerStatefulWidget {
  const IntakeView({super.key});

  @override
  ConsumerState<IntakeView> createState() => _IntakeViewState();
}

class _IntakeViewState extends ConsumerState<IntakeView> {
  final _formKey = GlobalKey<FormState>();
  final ImagePicker _picker = ImagePicker();
  int _currentStep = 1;

  Future<void> _pickImage() async {
    final XFile? image = await _picker.pickImage(source: ImageSource.gallery);
    if (image != null) {
      ref.read(intakeProvider.notifier).setPhoto(File(image.path));
    }
  }

  void _submit() async {
    if (_formKey.currentState!.validate()) {
      _formKey.currentState!.save();
      try {
        final workflowId = await ref
            .read(intakeProvider.notifier)
            .submitIntake();
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
              backgroundColor: AppTokens.red,
            ),
          );
        }
      }
    }
  }

  void _nextStep() {
    if (_formKey.currentState!.validate()) {
      _formKey.currentState!.save();
      if (_currentStep < 5) {
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

    return Scaffold(
      backgroundColor: AppTokens.bg,
      appBar: AppBar(
        backgroundColor: AppTokens.bg,
        elevation: 0,
        leading: Padding(
          padding: const EdgeInsets.only(left: 16.0),
          child: IconButton(
            icon: const Icon(Icons.arrow_back, color: AppTokens.ink),
            onPressed: () => context.go('/dashboard'),
            style: IconButton.styleFrom(
              backgroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: const BorderSide(color: AppTokens.line),
              ),
            ),
          ),
        ),
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'New Project Setup',
              style: TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 18,
                color: AppTokens.ink,
              ),
            ),
            Text(
              'Provide your land details and requirements',
              style: TextStyle(fontSize: 12, color: AppTokens.inkMute),
            ),
          ],
        ),
      ),
      body: intakeState.when(
        loading: () => const Center(
          child: CircularProgressIndicator(color: AppTokens.ink),
        ),
        error: (err, stack) => Center(child: Text('Error: $err')),
        data: (data) => Column(
          children: [
            _buildStepIndicator(),
            Expanded(
              child: Form(
                key: _formKey,
                child: ListView(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 16,
                  ),
                  physics: const BouncingScrollPhysics(),
                  children: [
                    if (_currentStep == 1) _buildStep1(data),
                    if (_currentStep == 2) _buildStep2(data),
                    if (_currentStep == 3) _buildStep3(data),
                    if (_currentStep == 4) _buildStep4(data),
                    if (_currentStep == 5) _buildStep5(data),
                    const SizedBox(height: 12),
                    _buildFooter(intakeState.isLoading),
                    const SizedBox(height: 80),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStepIndicator() {
    final steps = ['LAND', 'PLOT & SITE', 'HOUSE', 'FEATURES', 'REVIEW'];
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: List.generate(steps.length, (index) {
          final stepNum = index + 1;
          final isActive = _currentStep == stepNum;
          final isPast = _currentStep > stepNum;

          return Expanded(
            child: Column(
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: isActive || isPast
                        ? const Color(0xFF2563EB)
                        : Colors.white,
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: isActive || isPast
                          ? const Color(0xFF2563EB)
                          : const Color(0xFFE2E8F0),
                      width: 1,
                    ),
                  ),
                  child: Center(
                    child: isPast
                        ? const Icon(Icons.check, size: 20, color: Colors.white)
                        : Text(
                            '$stepNum',
                            style: TextStyle(
                              color: isActive
                                  ? Colors.white
                                  : const Color(0xFF64748B),
                              fontWeight: FontWeight.bold,
                              fontSize: 14,
                            ),
                          ),
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  steps[index],
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w800,
                    color: isActive || isPast
                        ? AppTokens.ink
                        : const Color(0xFF64748B),
                  ),
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          );
        }),
      ),
    );
  }

  Widget _buildFooter(bool isSubmitting) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          if (_currentStep > 1) ...[
            Expanded(
              flex: 1,
              child: OutlinedButton.icon(
                onPressed: isSubmitting ? null : _prevStep,
                icon: const Icon(Icons.chevron_left, size: 18),
                label: const Text('Back', style: TextStyle(fontWeight: FontWeight.bold)),
                style: OutlinedButton.styleFrom(
                  foregroundColor: const Color(0xFF475569),
                  side: const BorderSide(color: AppTokens.line),
                  padding: const EdgeInsets.symmetric(vertical: 18),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                  backgroundColor: Colors.white,
                ),
              ),
            ),
            const SizedBox(width: 16),
          ],
          Expanded(
            flex: _currentStep > 1 ? 2 : 1,
            child: Container(
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(16),
                boxShadow: [
                  BoxShadow(
                    color: (_currentStep == 5 ? const Color(0xFF2563EB) : const Color(0xFF0F172A)).withOpacity(0.25),
                    blurRadius: 12,
                    offset: const Offset(0, 4),
                  ),
                ],
                gradient: _currentStep == 5
                    ? const LinearGradient(
                        colors: [Color(0xFF3B82F6), Color(0xFF1D4ED8)],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      )
                    : null,
              ),
              child: ElevatedButton(
                onPressed: isSubmitting ? null : _nextStep,
                style: ElevatedButton.styleFrom(
                  backgroundColor: _currentStep == 5 ? Colors.transparent : const Color(0xFF0F172A),
                  foregroundColor: Colors.white,
                  shadowColor: Colors.transparent,
                  padding: const EdgeInsets.symmetric(vertical: 18),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                mainAxisAlignment: MainAxisAlignment.center,
                children: isSubmitting
                    ? const [
                        SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(
                            color: Colors.white,
                            strokeWidth: 2,
                          ),
                        ),
                        SizedBox(width: 8),
                        Flexible(
                          child: Text(
                            'Generating your plan...',
                            style: TextStyle(fontWeight: FontWeight.bold),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ]
                    : [
                        Flexible(
                          child: Text(
                            _currentStep == 5 ? 'Generate AI Plan' : 'Next',
                            style: const TextStyle(fontWeight: FontWeight.bold),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Icon(
                          _currentStep == 5
                              ? Icons.check_circle_outline
                              : Icons.chevron_right,
                          size: 18,
                        ),
                      ],
              ), // end Row
            ), // end ElevatedButton
          ), // end Container
        ), // end Expanded
        ],
      ),
    );
  }

  Widget _buildStep1(LandSubmission data) {
    return _buildCard(
      title: 'Tell us about your land',
      subtitle: 'Basic details about your property size and terrain.',
      icon: Icons.map,
      children: [
        Row(
          children: [
            Expanded(
              flex: 2,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Land Size *',
                    style: TextStyle(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                      color: AppTokens.inkSoft,
                    ),
                  ),
                  const SizedBox(height: 8),
                  _buildTextField(
                    hint: 'e.g. 25',
                    initialValue: data.landSizePerches?.toString() ?? '',
                    keyboardType: TextInputType.number,
                    validator: (val) {
                      if (val == null || val.isEmpty) return 'Required';
                      final num = double.tryParse(val);
                      if (num != null && num <= 0) return 'Must be positive';
                      return null;
                    },
                    onSaved: (val) => ref
                        .read(intakeProvider.notifier)
                        .updateField(landSizePerches: double.tryParse(val!)),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              flex: 1,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Unit',
                    style: TextStyle(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                      color: AppTokens.inkSoft,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Container(
                    decoration: BoxDecoration(
                      color: const Color(0xFFF6F2F4),
                      borderRadius: BorderRadius.circular(
                        AppTokens.radiusField,
                      ),
                      border: Border.all(color: AppTokens.line),
                    ),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 16,
                    ),
                    child: const Text(
                      'Perches',
                      style: TextStyle(fontSize: 14.5, color: AppTokens.ink),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 24),
        const Text(
          'Terrain Type',
          style: TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: AppTokens.inkSoft,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          decoration: BoxDecoration(
            color: const Color(0xFFF6F2F4),
            borderRadius: BorderRadius.circular(AppTokens.radiusField),
            border: Border.all(color: AppTokens.line),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              value: data.manualTerrainType ?? 'flat',
              isExpanded: true,
              icon: const Icon(
                Icons.keyboard_arrow_down,
                color: AppTokens.inkMute,
              ),
              style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
              items: const [
                DropdownMenuItem(
                  value: 'flat',
                  child: Text('Flat / Urban'),
                ),
                DropdownMenuItem(value: 'hillside', child: Text('Sloped')),
                DropdownMenuItem(
                  value: 'coastal',
                  child: Text('Coastal / Beachfront'),
                ),
                DropdownMenuItem(
                  value: 'forested',
                  child: Text('Wooded / Forest'),
                ),
                DropdownMenuItem(
                  value: 'rural',
                  child: Text('Rural / Farmland'),
                ),
              ],
              onChanged: (val) => ref
                  .read(intakeProvider.notifier)
                  .updateField(manualTerrainType: val),
            ),
          ),
        ),
        const SizedBox(height: 24),
        const Text(
          'Upload Land Photo (Optional)',
          style: TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: AppTokens.inkSoft,
          ),
        ),
        const SizedBox(height: 8),
        data.landPhoto != null
            ? _buildPhotoPreview(data.landPhoto!)
            : _buildPhotoUploadButton(),
      ],
    );
  }

  Widget _buildStep2(LandSubmission data) {
    return _buildCard(
      title: 'Define the plot',
      subtitle:
          'If dimensions are unavailable, the system can estimate conceptual proportions.',
      icon: Icons.straighten,
      children: [
        Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Plot Width (ft) (Optional)',
                    style: TextStyle(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                      color: AppTokens.inkSoft,
                    ),
                  ),
                  const SizedBox(height: 8),
                  _buildTextField(
                    hint: 'e.g. 50',
                    initialValue: data.plotWidth?.toString(),
                    keyboardType: TextInputType.number,
                    validator: (val) {
                      if (val != null && val.isNotEmpty) {
                        final num = double.tryParse(val);
                        if (num != null && num <= 0) return 'Must be positive';
                      }
                      return null;
                    },
                    onSaved: (val) => ref
                        .read(intakeProvider.notifier)
                        .updateField(plotWidth: double.tryParse(val ?? '')),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Plot Length (ft) (Optional)',
                    style: TextStyle(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                      color: AppTokens.inkSoft,
                    ),
                  ),
                  const SizedBox(height: 8),
                  _buildTextField(
                    hint: 'e.g. 100',
                    initialValue: data.plotLength?.toString(),
                    keyboardType: TextInputType.number,
                    validator: (val) {
                      if (val != null && val.isNotEmpty) {
                        final num = double.tryParse(val);
                        if (num != null && num <= 0) return 'Must be positive';
                      }
                      return null;
                    },
                    onSaved: (val) => ref
                        .read(intakeProvider.notifier)
                        .updateField(plotLength: double.tryParse(val ?? '')),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _buildStringDropdown(
                'Road Side',
                ['North', 'South', 'East', 'West'],
                data.roadSide ?? 'south',
                (val) => ref
                    .read(intakeProvider.notifier)
                    .updateField(roadSide: val),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: _buildStringDropdown(
                'North Direction',
                ['North', 'South', 'East', 'West'],
                data.northOrientation ?? 'north',
                (val) => ref
                    .read(intakeProvider.notifier)
                    .updateField(northOrientation: val),
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        _buildStringDropdown(
          'Main Access / Entrance',
          ['Road Side', 'North', 'South', 'East', 'West'],
          data.entranceSide ?? 'road_side',
          (val) =>
              ref.read(intakeProvider.notifier).updateField(entranceSide: val),
        ),
        const SizedBox(height: 16),
        const Text(
          'Plot Setbacks (Optional)',
          style: TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: AppTokens.inkSoft,
          ),
        ),
        const SizedBox(height: 8),
        _buildTextField(
          hint: 'e.g. Front 10ft, Rear 5ft',
          initialValue: data.plotSetbacks,
          validator: (val) => null,
          onSaved: (val) =>
              ref.read(intakeProvider.notifier).updateField(plotSetbacks: val),
        ),
        const SizedBox(height: 16),
        const Text(
          'Target Completion Date (Optional)',
          style: TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: AppTokens.inkSoft,
          ),
        ),
        const SizedBox(height: 8),
        _buildDatePicker(context, data.targetCompletionDate),
      ],
    );
  }

  Widget _buildStep3(LandSubmission data) {
    return _buildCard(
      title: 'Describe the home you want',
      subtitle: 'Basic requirements for the internal spaces.',
      icon: Icons.home,
      children: [
        Row(
          children: [
            Expanded(
              child: _buildDropdown(
                'Beds *',
                [1, 2, 3, 4, 5],
                data.preferredBedrooms ?? 3,
                (val) => ref
                    .read(intakeProvider.notifier)
                    .updateField(preferredBedrooms: val),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildDropdown(
                'Baths *',
                [1, 2, 3, 4],
                data.preferredBathrooms ?? 1,
                (val) => ref
                    .read(intakeProvider.notifier)
                    .updateField(preferredBathrooms: val),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildDropdown(
                'Floors *',
                [1, 2, 3],
                data.preferredFloors ?? 1,
                (val) => ref
                    .read(intakeProvider.notifier)
                    .updateField(preferredFloors: val),
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        const Text(
          'Architectural Style',
          style: TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: AppTokens.inkSoft,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          decoration: BoxDecoration(
            color: const Color(0xFFF6F2F4),
            borderRadius: BorderRadius.circular(AppTokens.radiusField),
            border: Border.all(color: AppTokens.line),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              value: data.stylePreference ?? 'modern minimalist',
              isExpanded: true,
              icon: const Icon(
                Icons.keyboard_arrow_down,
                color: AppTokens.inkMute,
              ),
              style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
              items: const [
                DropdownMenuItem(
                  value: 'modern minimalist',
                  child: Text('Modern Minimalist'),
                ),
                DropdownMenuItem(
                  value: 'tropical modern',
                  child: Text('Tropical Modern'),
                ),
                DropdownMenuItem(
                  value: 'traditional',
                  child: Text('Traditional'),
                ),
                DropdownMenuItem(
                  value: 'contemporary',
                  child: Text('Contemporary'),
                ),
                DropdownMenuItem(
                  value: 'industrial',
                  child: Text('Industrial'),
                ),
                DropdownMenuItem(value: 'modern', child: Text('Modern')),
              ],
              onChanged: (val) => ref
                  .read(intakeProvider.notifier)
                  .updateField(stylePreference: val),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildStep4(LandSubmission data) {
    return _buildCard(
      title: 'Choose optional features',
      subtitle: 'Select any extra requirements for your home.',
      icon: Icons.check_circle_outline,
      children: [
        Column(
          children: [
            Row(
              children: [
                Expanded(
                  child: _buildFeatureCard(
                    title: 'Separate Dining Area',
                    subtitle: 'Enclosed or distinct dining room.',
                    isActive: data.separateDining,
                    onTap: () => ref
                        .read(intakeProvider.notifier)
                        .togglePreference('separateDining'),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: _buildFeatureCard(
                    title: 'Home Office',
                    subtitle: 'Dedicated workspace room.',
                    isActive: data.homeOffice,
                    onTap: () => ref
                        .read(intakeProvider.notifier)
                        .togglePreference('homeOffice'),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _buildFeatureCard(
                    title: 'Accessible Layout',
                    subtitle: 'Reduced-step and wheelchair friendly.',
                    isActive: data.accessibility,
                    onTap: () => ref
                        .read(intakeProvider.notifier)
                        .togglePreference('accessibility'),
                  ),
                ),
                const SizedBox(width: 16),
                const Expanded(child: SizedBox()),
              ],
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildFeatureCard({
    required String title,
    required String subtitle,
    required bool isActive,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isActive ? const Color(0xFFEFF6FF) : Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: isActive ? const Color(0xFF3B82F6) : const Color(0xFFE2E8F0),
            width: 1,
          ),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              isActive ? Icons.check_box : Icons.check_box_outline_blank,
              color: isActive ? const Color(0xFF2563EB) : const Color(0xFF94A3B8),
              size: 20,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: isActive ? const Color(0xFF1E3A8A) : AppTokens.ink,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    subtitle,
                    style: TextStyle(
                      fontSize: 12,
                      color: isActive ? const Color(0xFF1D4ED8) : AppTokens.inkMute,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStep5(LandSubmission data) {
    return _buildCard(
      title: 'Review your project',
      subtitle: 'Ensure all details are correct before generating your plan.',
      icon: Icons.rate_review_outlined,
      children: [
        _buildReviewSection('Land', [
          '${data.landSizePerches ?? '--'} Perches',
          '${data.manualTerrainType ?? 'Flat / Urban'}',
        ], onEdit: () => setState(() => _currentStep = 1)),
        _buildReviewSection('Plot & Site', [
          data.plotWidth != null && data.plotLength != null
              ? '${data.plotWidth} ft x ${data.plotLength} ft'
              : 'Dimensions estimated',
          'Road: ${data.roadSide ?? 'South'}',
          'Entrance: ${data.entranceSide ?? 'Road Side'}',
        ], onEdit: () => setState(() => _currentStep = 2)),
        _buildReviewSection('House Requirements', [
          '${data.preferredBedrooms ?? 3} Bedrooms, ${data.preferredBathrooms ?? 1} Bathrooms, ${data.preferredFloors ?? 1} Floors',
          'Style: ${data.stylePreference ?? 'Modern Minimalist'}',
        ], onEdit: () => setState(() => _currentStep = 3)),
        _buildReviewSection('Features', [
          if (!data.separateDining && !data.homeOffice && !data.accessibility)
            'No extra features selected'
          else ...[
            if (data.separateDining) 'Separate Dining Area',
            if (data.homeOffice) 'Home Office',
            if (data.accessibility) 'Accessible Layout',
          ]
        ], onEdit: () => setState(() => _currentStep = 4)),
      ],
    );
  }

  Widget _buildReviewSection(String title, List<String> details, {required VoidCallback onEdit}) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: const Color(0xFFF9FAFB),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(
                    title == 'Land' ? Icons.map_outlined : 
                    title == 'Plot & Site' ? Icons.explore_outlined : 
                    title == 'House Requirements' ? Icons.home_outlined : Icons.check_circle_outline,
                    size: 18,
                    color: const Color(0xFF475569),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    title,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      color: AppTokens.ink,
                    ),
                  ),
                ],
              ),
              InkWell(
                onTap: onEdit,
                child: const Text(
                  'Edit',
                  style: TextStyle(
                    color: Color(0xFF2563EB),
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...details
              .map(
                (d) => Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(
                    '• $d',
                    style: const TextStyle(
                      color: Color(0xFF475569),
                      fontSize: 13,
                    ),
                  ),
                ),
              )
              .toList(),
        ],
      ),
    );
  }

  Widget _buildDropdown(
    String label,
    List<int> items,
    int value,
    void Function(int?) onChanged,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: AppTokens.inkSoft,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          decoration: BoxDecoration(
            color: const Color(0xFFF6F2F4),
            borderRadius: BorderRadius.circular(AppTokens.radiusField),
            border: Border.all(color: AppTokens.line),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<int>(
              value: value,
              isExpanded: true,
              icon: const Icon(
                Icons.keyboard_arrow_down,
                color: AppTokens.inkMute,
                size: 20,
              ),
              style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
              items: items
                  .map((e) => DropdownMenuItem(value: e, child: Text('$e')))
                  .toList(),
              onChanged: onChanged,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildStringDropdown(
    String label,
    List<String> items,
    String value,
    void Function(String?) onChanged,
  ) {
    final lowercaseItems = items.map((e) => e.toLowerCase()).toList();
    final safeValue =
        lowercaseItems.contains(value.toLowerCase().replaceAll('_', ' '))
        ? value.toLowerCase()
        : lowercaseItems.first;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: AppTokens.inkSoft,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          decoration: BoxDecoration(
            color: const Color(0xFFF6F2F4),
            borderRadius: BorderRadius.circular(AppTokens.radiusField),
            border: Border.all(color: AppTokens.line),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              value: safeValue,
              isExpanded: true,
              icon: const Icon(
                Icons.keyboard_arrow_down,
                color: AppTokens.inkMute,
                size: 20,
              ),
              style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
              items: items
                  .map(
                    (e) => DropdownMenuItem(
                      value: e.toLowerCase(),
                      child: Text(e),
                    ),
                  )
                  .toList(),
              onChanged: onChanged,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildChip(
    String label, {
    bool isActive = false,
    VoidCallback? onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppTokens.radiusPill),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        decoration: BoxDecoration(
          color: isActive ? AppTokens.accentSoft : Colors.white,
          borderRadius: BorderRadius.circular(AppTokens.radiusPill),
          border: Border.all(
            color: isActive ? AppTokens.accent : AppTokens.line,
          ),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: isActive ? AppTokens.accent : AppTokens.inkSoft,
            fontSize: 12.5,
            fontWeight: isActive ? FontWeight.w600 : FontWeight.w500,
          ),
        ),
      ),
    );
  }

  Widget _buildCard({
    required String title,
    String? subtitle,
    required IconData icon,
    required List<Widget> children,
  }) {
    return Container(
      padding: const EdgeInsets.only(top: 8, bottom: 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.w800,
              color: AppTokens.ink,
              letterSpacing: -0.5,
            ),
          ),
          if (subtitle != null) ...[
            const SizedBox(height: 8),
            Text(
              subtitle,
              style: const TextStyle(
                fontSize: 15,
                color: Color(0xFF475569), // Slate-600
              ),
            ),
          ],
          const SizedBox(height: 32),
          ...children,
        ],
      ),
    );
  }

  Widget _buildTextField({
    required String hint,
    required FormFieldSetter<String> onSaved,
    required FormFieldValidator<String> validator,
    String? initialValue,
    TextInputType? keyboardType,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFFF6F2F4),
        borderRadius: BorderRadius.circular(AppTokens.radiusField),
        border: Border.all(color: AppTokens.line),
      ),
      child: TextFormField(
        key: ValueKey(hint),
        initialValue: initialValue,
        keyboardType: keyboardType,
        style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
        decoration: InputDecoration(
          hintText: hint,
          hintStyle: const TextStyle(color: AppTokens.inkMute, fontSize: 14.5),
          border: InputBorder.none,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 16,
          ),
        ),
        validator: validator,
        onSaved: onSaved,
      ),
    );
  }

  Widget _buildDatePicker(BuildContext context, String? currentDate) {
    return InkWell(
      onTap: () async {
        final DateTime? picked = await showDatePicker(
          context: context,
          initialDate: DateTime.now().add(const Duration(days: 1)),
          firstDate: DateTime.now(),
          lastDate: DateTime.now().add(const Duration(days: 3650)),
        );
        if (picked != null) {
          ref
              .read(intakeProvider.notifier)
              .updateField(
                targetCompletionDate:
                    "${picked.year}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}",
              );
        }
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        decoration: BoxDecoration(
          color: const Color(0xFFF6F2F4),
          borderRadius: BorderRadius.circular(AppTokens.radiusField),
          border: Border.all(color: AppTokens.line),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              currentDate ?? 'Select a future date',
              style: TextStyle(
                fontSize: 14.5,
                color: currentDate != null ? AppTokens.ink : AppTokens.inkMute,
              ),
            ),
            const Icon(
              Icons.calendar_today,
              color: AppTokens.inkMute,
              size: 20,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPhotoUploadButton() {
    return InkWell(
      onTap: _pickImage,
      borderRadius: BorderRadius.circular(AppTokens.radiusSection),
      child: Container(
        height: 120,
        width: double.infinity,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(AppTokens.radiusSection),
        ),
        child: Container(
          decoration: BoxDecoration(
            border: Border.all(color: AppTokens.line, width: 2),
            borderRadius: BorderRadius.circular(AppTokens.radiusSection),
          ),
          child: const Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.upload_rounded, color: AppTokens.inkMute, size: 32),
              SizedBox(height: 12),
              Text(
                'Click to upload or drag and drop',
                style: TextStyle(
                  color: AppTokens.inkMute,
                  fontSize: 11.5,
                  fontWeight: FontWeight.w500,
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPhotoPreview(File photo) {
    return Stack(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(AppTokens.radiusSection),
          child: kIsWeb
              ? Image.network(
                  photo.path,
                  height: 160,
                  width: double.infinity,
                  fit: BoxFit.cover,
                )
              : Image.file(
                  photo,
                  height: 160,
                  width: double.infinity,
                  fit: BoxFit.cover,
                ),
        ),
        Positioned(
          top: 8,
          right: 8,
          child: InkWell(
            onTap: () => ref.read(intakeProvider.notifier).clearPhoto(),
            child: CircleAvatar(
              radius: 16,
              backgroundColor: Colors.black.withOpacity(0.6),
              child: const Icon(Icons.close, color: Colors.white, size: 18),
            ),
          ),
        ),
      ],
    );
  }
}
