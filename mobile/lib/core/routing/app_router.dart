import 'package:flutter/foundation.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:mobile/providers/auth_provider.dart';
import 'package:mobile/views/home_view.dart';
import 'package:mobile/views/login_view.dart';
import 'package:mobile/views/register_view.dart';
import 'package:mobile/views/dashboard_view.dart';
import 'package:mobile/views/plan_library_view.dart';
import 'package:mobile/views/plan_detail_view.dart';
import 'package:mobile/views/intake_view.dart';
import 'package:mobile/views/workflow_status_view.dart';
import 'package:mobile/views/design_preview_view.dart';
import 'package:mobile/views/construction_timeline_view.dart';
import 'package:mobile/views/profile_view.dart';
import 'package:mobile/widgets/main_scaffold.dart';

import 'package:mobile/models/user.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final notifier = ValueNotifier<AsyncValue<User?>>(const AsyncValue.loading());
  
  ref.listen(authProvider, (_, next) {
    notifier.value = next;
  });

  return GoRouter(
    initialLocation: '/',
    refreshListenable: notifier,
    redirect: (context, state) {
      final authState = notifier.value;
      final isLoading = authState.isLoading;
      if (isLoading) return null;

      final isAuth = authState.valueOrNull != null;
      final isLoggingIn = state.matchedLocation == '/login' || state.matchedLocation == '/register';
      final isHome = state.matchedLocation == '/';

      if (!isAuth && !isLoggingIn && !isHome) {
        return '/';
      }

      if (isAuth && (isLoggingIn || isHome)) {
        return '/dashboard';
      }

      return null;
    },
    routes: [
      GoRoute(
        path: '/',
        builder: (context, state) => const HomeView(),
      ),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginView(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterView(),
      ),
      ShellRoute(
        builder: (context, state, child) {
          return MainScaffold(child: child);
        },
        routes: [
          GoRoute(
            path: '/dashboard',
            builder: (context, state) => const DashboardView(),
          ),
          GoRoute(
            path: '/profile',
            builder: (context, state) => const ProfileView(),
          ),
          GoRoute(
            path: '/plans',
            builder: (context, state) => const PlanLibraryView(),
          ),
          GoRoute(
            path: '/plans/:id',
            builder: (context, state) {
              final id = state.pathParameters['id']!;
              return PlanDetailView(planId: id);
            },
          ),
          GoRoute(
            path: '/intake',
            builder: (context, state) {
              final basePlanId = state.uri.queryParameters['basePlanId'];
              final mode = state.uri.queryParameters['mode'];
              return IntakeView(basePlanId: basePlanId, mode: mode);
            },
          ),
          GoRoute(
            path: '/design/:workflowId',
            builder: (context, state) {
              final workflowId = state.pathParameters['workflowId']!;
              return WorkflowStatusView(workflowId: workflowId);
            },
          ),
          GoRoute(
            path: '/design/:workflowId/preview',
            builder: (context, state) {
              final workflowId = state.pathParameters['workflowId']!;
              return DesignPreviewView(workflowId: workflowId);
            },
          ),
          GoRoute(
            path: '/design/:workflowId/timeline',
            builder: (context, state) {
              final workflowId = state.pathParameters['workflowId']!;
              return ConstructionTimelineView(workflowId: workflowId);
            },
          ),
        ],
      ),
    ],
  );
});
