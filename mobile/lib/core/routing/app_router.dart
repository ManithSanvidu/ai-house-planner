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
import 'package:mobile/widgets/main_scaffold.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authProvider);

  return GoRouter(
    initialLocation: '/',
    redirect: (context, state) {
      final isLoading = authState.isLoading;
      if (isLoading) return null;

      final isAuth = authState.valueOrNull != null;
      final isLoggingIn = state.matchedLocation == '/login' || state.matchedLocation == '/register';
      final isHome = state.matchedLocation == '/';

      if (!isAuth && !isLoggingIn && !isHome) {
        return '/';
      }

      if (isAuth && isLoggingIn) {
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
            builder: (context, state) => const IntakeView(),
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
