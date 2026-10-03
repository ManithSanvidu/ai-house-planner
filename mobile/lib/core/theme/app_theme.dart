import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'app_tokens.dart';

class AppTheme {
  static ThemeData get lightTheme {
    final textTheme = GoogleFonts.interTextTheme(ThemeData.light().textTheme);
    
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: AppTokens.primary,
        brightness: Brightness.light,
        surface: AppTokens.bg,
        primary: AppTokens.primary,
        onPrimary: Colors.white,
      ),
      scaffoldBackgroundColor: AppTokens.bg,
      textTheme: textTheme.copyWith(
        displayLarge: textTheme.displayLarge?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        displayMedium: textTheme.displayMedium?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        displaySmall: textTheme.displaySmall?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        headlineLarge: textTheme.headlineLarge?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        headlineMedium: textTheme.headlineMedium?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        headlineSmall: textTheme.headlineSmall?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        titleLarge: textTheme.titleLarge?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        titleMedium: textTheme.titleMedium?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        titleSmall: textTheme.titleSmall?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w700),
        bodyLarge: textTheme.bodyLarge?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w500),
        bodyMedium: textTheme.bodyMedium?.copyWith(color: AppTokens.textPrimary, fontWeight: FontWeight.w400),
        bodySmall: textTheme.bodySmall?.copyWith(color: AppTokens.textSecondary, fontWeight: FontWeight.w400),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: AppTokens.bg,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        iconTheme: IconThemeData(color: AppTokens.textPrimary),
        titleTextStyle: TextStyle(
          color: AppTokens.textPrimary,
          fontSize: 20,
          fontWeight: FontWeight.w700,
          fontFamily: 'Inter',
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppTokens.primary,
          foregroundColor: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          textStyle: const TextStyle(fontWeight: FontWeight.w700, letterSpacing: 0.8),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppTokens.textPrimary,
          side: const BorderSide(color: AppTokens.line, width: 1.5),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTokens.radiusButton)),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          textStyle: const TextStyle(fontWeight: FontWeight.w700, letterSpacing: 0.8),
        ),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: Colors.white,
        indicatorColor: AppTokens.primarySoft,
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return const TextStyle(color: AppTokens.primary, fontWeight: FontWeight.w700, fontSize: 12);
          }
          return const TextStyle(color: AppTokens.textSecondary, fontWeight: FontWeight.w500, fontSize: 12);
        }),
        iconTheme: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return const IconThemeData(color: AppTokens.primary);
          }
          return const IconThemeData(color: AppTokens.textSecondary);
        }),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: AppTokens.card,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTokens.radiusCard),
          side: const BorderSide(color: AppTokens.line, width: 1),
        ),
        margin: EdgeInsets.zero,
      ),
      dividerTheme: const DividerThemeData(
        color: AppTokens.line,
        thickness: 1,
        space: 32,
      ),
    );
  }

  static ThemeData get darkTheme {
    return lightTheme; // Enforce light theme only for now as requested by exact visual match
  }
}
