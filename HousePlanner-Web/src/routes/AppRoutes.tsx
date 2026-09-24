import React from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import PageContainer from '../components/layout/PageContainer';
import AdminPlanFormPage from '../pages/AdminPlanFormPage';
import AdminPlansPage from '../pages/AdminPlansPage';
import AdminStaffPage from '../pages/AdminStaffPage';
import AdminWorkflowsPage from '../pages/AdminWorkflowsPage';
import DashboardPage from '../pages/DashboardPage';
import HomePage from '../pages/HomePage';
import IntakeForm from '../pages/IntakeForm';
import LoginPage from '../pages/LoginPage';
import RegisterPage from '../pages/RegisterPage';
import MyDesignsPage from '../pages/MyDesignsPage';
import CustomerConstructionPage from '../pages/CustomerConstructionPage';
import CustomerConstructionProgressPage from '../pages/CustomerConstructionProgressPage';
import PlanDetailPage from '../pages/PlanDetailPage';
import PlanLibraryPage from '../pages/PlanLibraryPage';
import PricingManagementPage from '../pages/PricingManagementPage';
import { WorkflowReviewPage } from '../pages/WorkflowReviewPage';
import ApprovedRequestsPage from '../pages/architect/ApprovedRequestsPage';
import ArchitectDashboard from '../pages/architect/ArchitectDashboard';
import ValidationRequestDetails from '../pages/architect/ValidationRequestDetails';
import ValidationRequestsPage from '../pages/architect/ValidationRequestsPage';
import ConstructorProjectWorkflow from '../pages/constructor/workflow/ConstructorProjectWorkflow';
import ConstructorProjectDetails from '../pages/constructor/workflow/ConstructorProjectDetails';
import ConstructorWorkflowDashboard from '../pages/constructor/workflow/ConstructorWorkflowDashboard';
import ProtectedRoute from './ProtectedRoute';

export const AppRoutes: React.FC = () => (
  <Routes>
    <Route path="/" element={<HomePage />} />
    <Route path="/login" element={<LoginPage />} />
    <Route path="/register" element={<RegisterPage />} />
    <Route path="/dashboard" element={<ProtectedRoute><PageContainer><DashboardPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/new-project" element={<ProtectedRoute allowedRoles={['Customer']}><PageContainer><IntakeForm /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/cost-estimator" element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><PricingManagementPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/plans" element={<ProtectedRoute allowedRoles={['Customer']}><PageContainer><PlanLibraryPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/designs" element={<ProtectedRoute allowedRoles={['Customer']}><PageContainer><MyDesignsPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/construction" element={<ProtectedRoute allowedRoles={['Customer']}><PageContainer><CustomerConstructionPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/construction/:projectId" element={<ProtectedRoute allowedRoles={['Customer']}><PageContainer><CustomerConstructionProgressPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/plans/:id" element={<ProtectedRoute allowedRoles={['Customer']}><PageContainer><PlanDetailPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/admin/plans" element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminPlansPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/admin/plans/new" element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminPlanFormPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/admin/plans/:id/edit" element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminPlanFormPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/admin/staff" element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminStaffPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/admin/workflows" element={<ProtectedRoute allowedRoles={['Admin']}><PageContainer><AdminWorkflowsPage /></PageContainer></ProtectedRoute>} />
    <Route path="/dashboard/workflows/:id" element={<ProtectedRoute allowedRoles={['Customer', 'Admin', 'Constructor']}><PageContainer><WorkflowReviewPage /></PageContainer></ProtectedRoute>} />
    <Route path="/architect/dashboard" element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ArchitectDashboard /></PageContainer></ProtectedRoute>} />
    <Route path="/architect/requests" element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ValidationRequestsPage /></PageContainer></ProtectedRoute>} />
    <Route path="/architect/approved" element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ApprovedRequestsPage /></PageContainer></ProtectedRoute>} />
    <Route path="/architect/requests/:id" element={<ProtectedRoute allowedRoles={['Architect']}><PageContainer><ValidationRequestDetails /></PageContainer></ProtectedRoute>} />
    <Route path="/constructor/dashboard" element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorWorkflowDashboard /></PageContainer></ProtectedRoute>} />
    <Route path="/constructor/workflows/:id" element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorProjectWorkflow /></PageContainer></ProtectedRoute>} />
    <Route path="/constructor/projects/:projectId" element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorProjectDetails /></PageContainer></ProtectedRoute>} />
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);

export default AppRoutes;
