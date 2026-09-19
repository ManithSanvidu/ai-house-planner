import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import '../providers/intake_provider.dart';
import '../models/land_submission.dart';
import 'package:go_router/go_router.dart';
import '../core/theme/app_tokens.dart';

class IntakeView extends ConsumerStatefulWidget{
  const IntakeView({super.key});

  @override
  ConsumerState<IntakeView> createState()=>_IntakeViewState();
}

class _IntakeViewState extends ConsumerState<IntakeView>{
  final _formKey=GlobalKey<FormState>();
  final ImagePicker _picker=ImagePicker();

  Future<void> _pickImage() async{
    final XFile? image=await _picker.pickImage(source:ImageSource.gallery);
    if(image!=null){
      ref.read(intakeProvider.notifier).setPhoto(File(image.path));
    }
  }
  
  void _submit() async {
    if (_formKey.currentState!.validate()) {
      _formKey.currentState!.save();
      final workflowId = await ref.read(intakeProvider.notifier).submitIntake();
      if (workflowId != null && mounted) {
        context.go('/design/$workflowId');
      }
    }
  }

  @override
  Widget build(BuildContext context){
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
            Text('New Project Setup', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18, color: AppTokens.ink)),
            Text('Land details & preferences', style: TextStyle(fontSize: 12, color: AppTokens.inkMute)),
          ],
        ),
      ),
      body: intakeState.when(
        loading: () => const Center(child: CircularProgressIndicator(color: AppTokens.ink)),
        error: (err, stack) => Center(child: Text('Error: $err')),
        data: (data) => _buildForm(data),
      ),
    );
  }

  Widget _buildForm(LandSubmission data) {
    return Column(
      children: [
        // Progress Bar
        const Padding(
          padding: EdgeInsets.symmetric(horizontal: 24, vertical: 8),
          child: Row(
            children: [
              Expanded(child: Divider(color: AppTokens.accent, thickness: 3)),
              SizedBox(width: 4),
              Expanded(child: Divider(color: AppTokens.line, thickness: 3)),
              SizedBox(width: 4),
              Expanded(child: Divider(color: AppTokens.line, thickness: 3)),
            ],
          ),
        ),
        Expanded(
          child: Form(
            key: _formKey,
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              physics: const BouncingScrollPhysics(),
              children: [
                _buildCard(
                  title: 'Budget & Land',
                  icon: Icons.attach_money,
                  children: [
                    const Text('Total budget (LKR) · optional', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.inkSoft)),
                    const SizedBox(height: 8),
                    _buildTextField(
                      hint: 'e.g. 15,000,000',
                      keyboardType: TextInputType.number,
                      validator: (val) => null,
                      onSaved: (val) => ref.read(intakeProvider.notifier).updateField(budgetLkr: double.tryParse(val ?? '')),
                    ),
                    const SizedBox(height: 16),
                    Row(
                      children: [
                        Expanded(
                          flex: 2,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Land size', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.inkSoft)),
                              const SizedBox(height: 8),
                              _buildTextField(
                                hint: '10',
                                keyboardType: TextInputType.number,
                                validator: (val) => val == null || val.isEmpty ? 'Required' : null,
                                onSaved: (val) => ref.read(intakeProvider.notifier).updateField(landSizePerches: double.tryParse(val!)),
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
                              const Text('Unit', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.inkSoft)),
                              const SizedBox(height: 8),
                              Container(
                                decoration: BoxDecoration(
                                  color: const Color(0xFFF6F2F4),
                                  borderRadius: BorderRadius.circular(AppTokens.radiusField),
                                  border: Border.all(color: AppTokens.line),
                                ),
                                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                                child: const Text('Perches', style: TextStyle(fontSize: 14.5, color: AppTokens.ink)),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
                const SizedBox(height: 24),
                
                _buildCard(
                  title: 'Terrain',
                  icon: Icons.terrain,
                  children: [
                    data.landPhoto != null
                        ? _buildPhotoPreview(data.landPhoto!)
                        : _buildPhotoUploadButton(),
                    const SizedBox(height: 24),
                    const Text('Terrain fallback type', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.inkSoft)),
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
                          icon: const Icon(Icons.keyboard_arrow_down, color: AppTokens.inkMute),
                          style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
                          items: const [
                            DropdownMenuItem(value: 'hillside', child: Text('Hillside')),
                            DropdownMenuItem(value: 'coastal', child: Text('Coastal')),
                            DropdownMenuItem(value: 'flat', child: Text('Flat / Urban')),
                            DropdownMenuItem(value: 'forested', child: Text('Forested')),
                          ],
                          onChanged: (val) => ref.read(intakeProvider.notifier).updateField(manualTerrainType: val),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 24),
                
                _buildCard(
                  title: 'Design Preferences',
                  icon: Icons.grid_view_rounded,
                  children: [
                    Row(
                      children: [
                        Expanded(child: _buildDropdown('Beds', [1, 2, 3, 4, 5], (val) => ref.read(intakeProvider.notifier).updateField(preferredBedrooms: val))),
                        const SizedBox(width: 12),
                        Expanded(child: _buildDropdown('Baths', [1, 2, 3, 4], (val) {
                          // Note: API hardcodes bathrooms to 1 currently, so we don't update provider
                        })),
                        const SizedBox(width: 12),
                        Expanded(child: _buildDropdown('Floors', [1, 2, 3], (val) => ref.read(intakeProvider.notifier).updateField(preferredFloors: val))),
                      ],
                    ),
                    const SizedBox(height: 16),
                    const Text('Architectural style', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.inkSoft)),
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
                          value: data.stylePreference ?? 'modern',
                          isExpanded: true,
                          icon: const Icon(Icons.keyboard_arrow_down, color: AppTokens.inkMute),
                          style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
                          items: const [
                            DropdownMenuItem(value: 'modern', child: Text('Modern Minimalist')),
                            DropdownMenuItem(value: 'traditional', child: Text('Traditional')),
                            DropdownMenuItem(value: 'contemporary', child: Text('Contemporary')),
                          ],
                          onChanged: (val) => ref.read(intakeProvider.notifier).updateField(stylePreference: val),
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    Wrap(
                      spacing: 8,
                      runSpacing: 12,
                      children: [
                        _buildChip('Open plan'),
                        _buildChip('Master ensuite', isActive: true),
                        _buildChip('Home office'),
                        _buildChip('Balcony', isActive: true),
                        _buildChip('Parking'),
                        _buildChip('Accessible'),
                      ],
                    ),
                  ],
                ),
                const SizedBox(height: 80),
              ],
            ),
          ),
        ),
        
        // Sticky Submit Bar
        Container(
          padding: const EdgeInsets.all(24),
          decoration: const BoxDecoration(
            color: AppTokens.bg,
            border: Border(top: BorderSide(color: AppTokens.line)),
          ),
          child: ElevatedButton(
            onPressed: data.isValid ? _submit : null,
            style: ElevatedButton.styleFrom(
              backgroundColor: AppTokens.ink,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(vertical: 18),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
              minimumSize: const Size(double.infinity, 0),
              elevation: 0,
            ),
            child: const Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text('Generate AI Plan', style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold)),
                SizedBox(width: 8),
                Icon(Icons.auto_awesome, size: 16), // ✦
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildDropdown(String label, List<int> items, void Function(int?) onChanged) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: AppTokens.inkSoft)),
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
              value: items.first, // simple stub for value tracking
              isExpanded: true,
              icon: const Icon(Icons.keyboard_arrow_down, color: AppTokens.inkMute, size: 20),
              style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
              items: items.map((e) => DropdownMenuItem(value: e, child: Text('$e'))).toList(),
              onChanged: onChanged,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildChip(String label, {bool isActive = false}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: BoxDecoration(
        color: isActive ? AppTokens.accentSoft : Colors.white,
        borderRadius: BorderRadius.circular(AppTokens.radiusPill),
        border: Border.all(color: isActive ? AppTokens.accent : AppTokens.line),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: isActive ? AppTokens.accent : AppTokens.inkSoft,
          fontSize: 12.5,
          fontWeight: isActive ? FontWeight.w600 : FontWeight.w500,
        ),
      ),
    );
  }

  Widget _buildCard({required String title, required IconData icon, required List<Widget> children}) {
    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: AppTokens.card,
        borderRadius: BorderRadius.circular(AppTokens.radiusCardSolid),
        border: Border.all(color: AppTokens.line),
        boxShadow: const [
          BoxShadow(
            color: Color(0x1F0B0B14),
            blurRadius: 26,
            offset: Offset(0, 10),
            spreadRadius: -14,
          )
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: AppTokens.accentSoft,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: AppTokens.accent, size: 16),
              ),
              const SizedBox(width: 12),
              Text(title, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800, color: AppTokens.ink)),
            ],
          ),
          const SizedBox(height: 24),
          ...children,
        ],
      ),
    );
  }

  Widget _buildTextField({
    required String hint,
    required FormFieldSetter<String> onSaved,
    required FormFieldValidator<String> validator,
    TextInputType? keyboardType,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFFF6F2F4),
        borderRadius: BorderRadius.circular(AppTokens.radiusField),
        border: Border.all(color: AppTokens.line),
      ),
      child: TextFormField(
        keyboardType: keyboardType,
        style: const TextStyle(fontSize: 14.5, color: AppTokens.ink),
        decoration: InputDecoration(
          hintText: hint,
          hintStyle: const TextStyle(color: AppTokens.inkMute, fontSize: 14.5),
          border: InputBorder.none,
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        ),
        validator: validator,
        onSaved: onSaved,
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
        // Faking a dashed border with a solid light border for now
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
              Text('Upload a land photo, or choose terrain manually', 
                style: TextStyle(color: AppTokens.inkMute, fontSize: 11.5, fontWeight: FontWeight.w500),
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
              ? Image.network(photo.path, height: 160, width: double.infinity, fit: BoxFit.cover)
              : Image.file(photo, height: 160, width: double.infinity, fit: BoxFit.cover),
        ),
        Positioned(
          top: 8,
          right: 8,
          child: InkWell(
            onTap: () => ref.read(intakeProvider.notifier).clearPhoto(),
            child: CircleAvatar(
              radius: 16,
              backgroundColor: Colors.black.withValues(alpha: 0.6),
              child: const Icon(Icons.close, color: Colors.white, size: 18),
            ),
          ),
        ),
      ],
    );
  }
}