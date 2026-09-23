import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class HomeView extends StatelessWidget {
  const HomeView({super.key});

  // Main image displayed in the hero section.
  static const String _heroImageUrl =
      'https://images.unsplash.com/photo-1671621556393-72aae2f654e5'
      '?q=85&w=1400&auto=format&fit=crop';

  // Shared colors used throughout the home screen.
  static const Color _background = Color(0xFFF7F7F5);
  static const Color _surface = Color(0xFFFFFFFF);
  static const Color _primary = Color(0xFF171717);
  static const Color _secondary = Color(0xFF6F6F6B);
  static const Color _border = Color(0xFFE8E8E4);
  static const Color _accent = Color(0xFFE9E7DF);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _background,
      body: SafeArea(
        // Makes the home page scrollable on smaller screens.
        child: CustomScrollView(
          physics: const BouncingScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(22, 18, 22, 32),
              sliver: SliverList(
                delegate: SliverChildListDelegate(
                  [
                    _buildTopBar(),
                    const SizedBox(height: 28),

                    // Main architectural image.
                    _buildHeroImage(),

                    const SizedBox(height: 26),
                    _buildAiBadge(),
                    const SizedBox(height: 14),

                    const Text(
                      'Design your\nfuture home.',
                      style: TextStyle(
                        color: _primary,
                        fontSize: 42,
                        height: 1.02,
                        fontWeight: FontWeight.w700,
                        letterSpacing: -1.8,
                      ),
                    ),

                    const SizedBox(height: 16),

                    const Text(
                      'Turn your land and ideas into a personalized '
                      'architectural plan with AI.',
                      style: TextStyle(
                        color: _secondary,
                        fontSize: 16,
                        height: 1.55,
                        fontWeight: FontWeight.w400,
                      ),
                    ),

                    const SizedBox(height: 24),

                    // Highlights of the main app features.
                    _buildFeatureRow(),

                    const SizedBox(height: 28),

                    // Main call-to-action button.
                    _buildGetStartedButton(context),

                    const SizedBox(height: 14),

                    const Center(
                      child: Text(
                        'Create your first design in minutes',
                        style: TextStyle(
                          color: Color(0xFF8A8A85),
                          fontSize: 12.5,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),

                    const SizedBox(height: 28),

                    // Short description displayed at the bottom.
                    _buildBottomInfo(),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTopBar() {
    return Row(
      children: [
        Container(
          width: 42,
          height: 42,
          decoration: BoxDecoration(
            color: _primary,
            borderRadius: BorderRadius.circular(13),
          ),
          child: const Icon(
            Icons.architecture_rounded,
            color: Colors.white,
            size: 22,
          ),
        ),

        const SizedBox(width: 12),

        // Expanded keeps the menu button aligned to the right.
        const Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'HOMEPLANNER',
                style: TextStyle(
                  color: _primary,
                  fontSize: 14,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 1.2,
                ),
              ),
              SizedBox(height: 2),
              Text(
                'AI ARCHITECTURE',
                style: TextStyle(
                  color: Color(0xFF999994),
                  fontSize: 8.5,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.5,
                ),
              ),
            ],
          ),
        ),

        // More options button.
        Container(
          width: 42,
          height: 42,
          decoration: BoxDecoration(
            color: _surface,
            borderRadius: BorderRadius.circular(13),
            border: Border.all(
              color: _border,
              width: 1,
            ),
          ),
          child: const Icon(
            Icons.more_horiz_rounded,
            color: _primary,
            size: 22,
          ),
        ),
      ],
    );
  }

  Widget _buildHeroImage() {
    return Container(
      height: 285,
      width: double.infinity,
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        color: const Color(0xFFE4E4DF),
      ),
      child: Stack(
        fit: StackFit.expand,
        children: [
          Image.network(
            _heroImageUrl,
            fit: BoxFit.cover,

            // Shows a loader while the image is being downloaded.
            loadingBuilder: (context, child, loadingProgress) {
              if (loadingProgress == null) {
                return child;
              }

              return Container(
                color: const Color(0xFFE5E5E0),
                child: const Center(
                  child: SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: _primary,
                    ),
                  ),
                ),
              );
            },

            // Shows a fallback icon if the image fails to load.
            errorBuilder: (context, error, stackTrace) {
              return Container(
                color: const Color(0xFFE3E3DE),
                child: const Center(
                  child: Icon(
                    Icons.home_work_outlined,
                    size: 52,
                    color: Color(0xFF9B9B94),
                  ),
                ),
              );
            },
          ),

          // Adds a subtle dark gradient over the image.
          Positioned.fill(
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Colors.transparent,
                    Colors.black.withValues(alpha: 0.04),
                  ],
                ),
              ),
            ),
          ),

          // AI badge shown on top of the image.
          Positioned(
            left: 16,
            top: 16,
            child: Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 8,
              ),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.90),
                borderRadius: BorderRadius.circular(20),
              ),
              child: const Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    Icons.auto_awesome_rounded,
                    size: 14,
                    color: _primary,
                  ),
                  SizedBox(width: 6),
                  Text(
                    'AI POWERED',
                    style: TextStyle(
                      color: _primary,
                      fontSize: 9.5,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 0.8,
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Short tagline positioned at the bottom of the image.
          Positioned(
            left: 16,
            bottom: 16,
            child: Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 8,
              ),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.35),
                borderRadius: BorderRadius.circular(20),
              ),
              child: const Text(
                'Your ideas. Your space.',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 11,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAiBadge() {
    return Row(
      children: [
        // Small status indicator.
        Container(
          width: 8,
          height: 8,
          decoration: const BoxDecoration(
            color: Color(0xFF7C8C63),
            shape: BoxShape.circle,
          ),
        ),

        const SizedBox(width: 8),

        const Text(
          'SMART HOME DESIGN',
          style: TextStyle(
            color: Color(0xFF777771),
            fontSize: 10.5,
            fontWeight: FontWeight.w800,
            letterSpacing: 1.2,
          ),
        ),
      ],
    );
  }

  Widget _buildFeatureRow() {
    return Row(
      children: [
        Expanded(
          child: _buildFeatureItem(
            icon: Icons.architecture_rounded,
            title: 'Plans',
            subtitle: 'AI generated',
          ),
        ),

        const SizedBox(width: 10),

        Expanded(
          child: _buildFeatureItem(
            icon: Icons.calendar_month_rounded,
            title: 'Timeline',
            subtitle: 'Construction',
          ),
        ),
      ],
    );
  }

  Widget _buildFeatureItem({
    required IconData icon,
    required String title,
    required String subtitle,
  }) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: _border,
        ),
      ),
      child: Row(
        children: [
          // Icon container for the feature.
          Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              color: _accent,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(
              icon,
              size: 19,
              color: _primary,
            ),
          ),

          const SizedBox(width: 10),

          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: _primary,
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                  ),
                ),

                const SizedBox(height: 2),

                Text(
                  subtitle,
                  style: const TextStyle(
                    color: _secondary,
                    fontSize: 10.5,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGetStartedButton(BuildContext context) {
    return Column(
      children: [
        // Start designing button (login)
        SizedBox(
          width: double.infinity,
          height: 58,
          child: ElevatedButton(
            // Opens the login screen when the user starts designing.
            onPressed: () => context.go('/login'),
            style: ElevatedButton.styleFrom(
              backgroundColor: _primary,
              foregroundColor: Colors.white,
              elevation: 0,
              shadowColor: Colors.transparent,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(18),
              ),
            ),
            child: const Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  'Start designing',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    letterSpacing: -0.1,
                  ),
                ),
                SizedBox(width: 10),
                Icon(
                  Icons.arrow_forward_rounded,
                  size: 19,
                ),
              ],
            ),
          ),
        ),

        const SizedBox(height: 12),

        // Create account button (register)
        TextButton(
          onPressed: () => context.go('/register'),
          style: TextButton.styleFrom(
            foregroundColor: AppTokens.red,
          ),
          child: const Text(
            'Create an account',
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
          ),
        ),

        const SizedBox(height: 24),
      ],
    );
  }

  Widget _buildBottomInfo() {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: 16,
        vertical: 14,
      ),
      decoration: BoxDecoration(
        color: const Color(0xFFF0F0EC),
        borderRadius: BorderRadius.circular(16),
      ),
      child: const Row(
        children: [
          Icon(
            Icons.auto_awesome_rounded,
            size: 17,
            color: Color(0xFF77776F),
          ),

          SizedBox(width: 10),

          Expanded(
            child: Text(
              'Personalized designs based on your land, budget and lifestyle.',
              style: TextStyle(
                color: Color(0xFF666660),
                fontSize: 11.5,
                height: 1.4,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}