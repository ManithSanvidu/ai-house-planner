import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from '../pages/LoginPage';
import DashboardPage from '../pages/DashboardPage';
import ProtectedRoute from './ProtectedRoute';
import PageContainer from '../components/layout/PageContainer';
import IntakeForm from '../pages/IntakeForm';
import HomePage from '../pages/HomePage';
import { WorkflowReviewPage } from '../pages/WorkflowReviewPage';
import PlanLibraryPage from '../pages/PlanLibraryPage';
import PlanDetailPage from '../pages/PlanDetailPage';
import AdminPlansPage from '../pages/AdminPlansPage';
import AdminPlanFormPage from '../pages/AdminPlanFormPage';
import ArchitectDashboard from '../pages/architect/ArchitectDashboard';
import ValidationRequestsPage from '../pages/architect/ValidationRequestsPage';
import ApprovedRequestsPage from '../pages/architect/ApprovedRequestsPage';
import ValidationRequestDetails from '../pages/architect/ValidationRequestDetails';
import MyDesignsPage from '../pages/MyDesignsPage';
import ConstructorWorkflowDashboard from '../pages/constructor/workflow/ConstructorWorkflowDashboard';
import ConstructorProjectWorkflow from '../pages/constructor/workflow/ConstructorProjectWorkflow';

export const AppRoutes: React.FC = () => {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />

      {/* Protected Routes */}
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <PageContainer>
              <DashboardPage />
            </PageContainer>
          </ProtectedRoute>
        }
      />

      <Route
        path="/dashboard/new-project"
        element={
          <ProtectedRoute>
            <PageContainer>
              <IntakeForm/>
            </PageContainer>
          </ProtectedRoute>
        }
      />

      <Route
        path="/dashboard/plans"
        element={<ProtectedRoute><PageContainer><PlanLibraryPage /></PageContainer></ProtectedRoute>}
      />
      <Route path="/dashboard/designs" element={<ProtectedRoute><PageContainer><MyDesignsPage /></PageContainer></ProtectedRoute>} />
      <Route
        path="/dashboard/plans/:id"
        element={<ProtectedRoute><PageContainer><PlanDetailPage /></PageContainer></ProtectedRoute>}
      />
      <Route
        path="/dashboard/admin/plans"
        element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminPlansPage /></PageContainer></ProtectedRoute>}
      />
      <Route
        path="/dashboard/admin/plans/new"
        element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminPlanFormPage /></PageContainer></ProtectedRoute>}
      />
      <Route
        path="/dashboard/admin/plans/:id/edit"
        element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminPlanFormPage /></PageContainer></ProtectedRoute>}
      />

      <Route
        path="/dashboard/workflows/:id"
        element={
          <ProtectedRoute>
            <PageContainer>
              <WorkflowReviewPage />
            </PageContainer>
          </ProtectedRoute>
        }
      />

            <Route
        path="/architect/dashboard"
        element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ArchitectDashboard /></PageContainer></ProtectedRoute>}
      />
      <Route
        path="/architect/requests"
        element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ValidationRequestsPage /></PageContainer></ProtectedRoute>}
      />
      <Route
        path="/architect/approved"
        element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ApprovedRequestsPage /></PageContainer></ProtectedRoute>}
      />
      <Route
        path="/architect/requests/:id"
        element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ValidationRequestDetails /></PageContainer></ProtectedRoute>}
      />

      {/* Constructor Routes */}
      <Route
        path="/constructor/dashboard"
        element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorWorkflowDashboard /></PageContainer></ProtectedRoute>}
      />
      <Route
        path="/constructor/workflows/:id"
        element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorProjectWorkflow /></PageContainer></ProtectedRoute>}
      />

      {/* Fallback routing */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};

export default AppRoutes;

