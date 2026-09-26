import React, { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';
import type { ConstructorWorkflowProject, ProjectProgress, ConstructorWorkflowLog } from '../../../services/constructorWorkflowService';
import { ArrowLeft, Clock, CheckCircle2, FileText, AlertTriangle, Plus } from 'lucide-react';
import DailyWorkflowForm from './DailyWorkflowForm';
import WorkflowHistory from './WorkflowHistory';
import CostBreakdownCard from '../../../components/cost/CostBreakdownCard';
import { FloorPlanViewer, type FloorPlanData } from '../../../components/floorplan/FloorPlanViewer';

export const ConstructorProjectWorkflow: React.FC = () => {
 const { projectId } = useParams<{ projectId: string }>();
 const [project, setProject] = useState<ConstructorWorkflowProject | null>(null);
 const [progress, setProgress] = useState<ProjectProgress | null>(null);
 const [logs, setLogs] = useState<ConstructorWorkflowLog[]>([]);
 const [loading, setLoading] = useState(true);
 const [showAddForm, setShowAddForm] = useState(false);

 const loadData = useCallback(async () => {
  if (!projectId) return;
  try {
   const [projData, progData, logsData] = await Promise.all([
    constructorWorkflowService.getProjectDetails(projectId),
    constructorWorkflowService.getProjectProgress(projectId),
    constructorWorkflowService.getWorkflowLogs(projectId)
   ]);
   setProject(projData);
   setProgress(progData);
   setLogs(logsData);
  } catch (error) {
   console.error('Failed to load project workflow data', error);
  } finally {
   setLoading(false);
  }
 }, [projectId]);

 useEffect(() => {
  loadData();
 }, [loadData]);

 if (loading || !project || !progress) {
  return (
   <div className="flex h-64 items-center justify-center">
    <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent" />
   </div>
  );
 }

 const isDelayed = progress.daysCompleted > progress.totalEstimatedDays;
 let floorPlan: FloorPlanData | null = null;
 try {
  floorPlan = project.design?.layoutJson ? JSON.parse(project.design.layoutJson) as FloorPlanData : null;
 } catch {
  floorPlan = null;
 }

 return (
  <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
   <Link
    to="/constructor/dashboard"
    className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-text-muted hover:text-gray-700 text-text-secondary dark:hover:text-gray-300"
   >
    <ArrowLeft className="h-4 w-4" />
    Back to Dashboard
   </Link>

   <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
    <div>
     <h1 className="text-2xl font-bold text-gray-900 dark:text-text-primary">
      Project Workflow: {project.id.substring(0, 8)}
     </h1>
     <p className="mt-1 text-sm text-text-muted text-text-secondary">
      Track daily progress against the construction plan.
     </p>
    </div>
    <button
     onClick={() => setShowAddForm(!showAddForm)}
     className="inline-flex items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-text-primary shadow-sm transition-all hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
    >
     {showAddForm ? 'Cancel' : (
      <>
       <Plus className="h-4 w-4" />
       Add Daily Log
      </>
     )}
    </button>
   </div>

   <div className="mb-8 grid grid-cols-1 gap-6 xl:grid-cols-2">
    <CostBreakdownCard cost={project.cost} />
    <section className="rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface">
     <h2 className="text-lg font-semibold text-gray-900 dark:text-text-primary">Approved Design</h2>
     {project.design ? (
      <>
       <div className="mt-4 grid grid-cols-2 gap-3 text-sm">
        <div className="rounded-xl bg-gray-50 p-3 bg-surface-elevated/50"><span className="text-text-muted">Version</span><p className="font-semibold">{project.design.version}</p></div>
        <div className="rounded-xl bg-gray-50 p-3 bg-surface-elevated/50"><span className="text-text-muted">Floors</span><p className="font-semibold">{project.design.floorCount}</p></div>
        <div className="rounded-xl bg-gray-50 p-3 bg-surface-elevated/50"><span className="text-text-muted">Area</span><p className="font-semibold">{project.design.totalBuiltUpAreaSqft.toLocaleString()} sq ft</p></div>
        <div className="rounded-xl bg-gray-50 p-3 bg-surface-elevated/50"><span className="text-text-muted">Foundation</span><p className="font-semibold capitalize">{project.design.foundationType}</p></div>
       </div>
       {floorPlan && (
        <div className="mt-4 h-80 overflow-hidden rounded-xl border border-border bg-gray-50 dark:border-border-strong bg-background">
         <FloorPlanViewer data={floorPlan} />
        </div>
       )}
      </>
     ) : (
      <p className="mt-4 text-sm text-text-muted">Approved design details are unavailable.</p>
     )}
    </section>
   </div>

   <div className="mb-8 grid grid-cols-1 gap-6 lg:grid-cols-3">
    {/* Progress Overview Card */}
    <div className="col-span-1 rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface lg:col-span-3 xl:col-span-1">
     <h2 className="mb-4 text-lg font-semibold text-gray-900 dark:text-text-primary">Planned vs Actual</h2>
     
     <div className="mb-6">
      <div className="flex items-center justify-between mb-2">
       <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Overall Progress</span>
       <span className="text-sm font-semibold text-indigo-600 dark:text-indigo-400">{progress.overallProgress}%</span>
      </div>
      <div className="h-2.5 w-full rounded-full bg-gray-200 bg-surface-elevated">
       <div
        className="h-2.5 rounded-full bg-indigo-600"
        style={{ width: `${Math.min(100, progress.overallProgress)}%` }}
       ></div>
      </div>
     </div>

     <div className="grid grid-cols-2 gap-4">
      <div className="rounded-xl bg-gray-50 p-4 bg-surface-elevated/50">
       <p className="text-sm text-text-muted text-text-secondary">Planned Days</p>
       <p className="text-xl font-bold text-gray-900 dark:text-text-primary">{progress.totalEstimatedDays}</p>
      </div>
      <div className="rounded-xl bg-gray-50 p-4 bg-surface-elevated/50">
       <p className="text-sm text-text-muted text-text-secondary">Actual Days</p>
       <p className="text-xl font-bold text-gray-900 dark:text-text-primary">{progress.daysCompleted}</p>
      </div>
     </div>

     <div className={`mt-4 flex items-start gap-3 rounded-xl p-4 ${isDelayed ? 'bg-amber-50 text-amber-800 dark:bg-amber-900/30 dark:text-amber-200' : 'bg-green-50 text-green-800 dark:bg-green-900/30 dark:text-green-200'}`}>
      {isDelayed ? <AlertTriangle className="h-5 w-5 shrink-0" /> : <CheckCircle2 className="h-5 w-5 shrink-0" />}
      <p className="text-sm font-medium">{progress.delayStatus}</p>
     </div>
    </div>

    {/* Construction Timeline */}
    <div className="col-span-1 rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface lg:col-span-2 xl:col-span-2">
     <h2 className="mb-4 text-lg font-semibold text-gray-900 dark:text-text-primary">Construction Phases</h2>
     <div className="relative border-l border-border dark:border-border-strong ml-3">
      {project.constructionPhases.map((phase) => (
       <div key={phase.id} className="mb-8 ml-6">
        <span className="absolute -left-3 flex h-6 w-6 items-center justify-center rounded-full bg-indigo-100 ring-8 ring-white dark:bg-indigo-900 dark:ring-gray-900">
         <Clock className="h-3 w-3 text-indigo-600 dark:text-indigo-400" />
        </span>
        <h3 className="mb-1 flex items-center text-base font-semibold text-gray-900 dark:text-text-primary">
         {phase.phaseName}
         {phase.status === 'completed' && <span className="ml-3 rounded bg-green-100 px-2.5 py-0.5 text-sm font-medium text-green-800 dark:bg-green-900 dark:text-green-300">Done</span>}
         {phase.status === 'in_progress' && <span className="ml-3 rounded bg-blue-100 px-2.5 py-0.5 text-sm font-medium text-blue-800 dark:bg-blue-900 dark:text-blue-300">In Progress</span>}
        </h3>
        <time className="mb-2 block text-sm font-normal leading-none text-text-secondary text-text-muted">
         Estimated Duration: {phase.aiEstimatedDurationDays} days
        </time>
        <p className="text-sm font-normal text-text-muted text-text-secondary">
         Phase order: {phase.sequenceOrder}
        </p>
       </div>
      ))}
     </div>
    </div>
   </div>

   {showAddForm && progress.totalEstimatedDays > 0 && (
    <div className="mb-8 rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface">
     <h2 className="mb-6 text-lg font-semibold text-gray-900 dark:text-text-primary flex items-center gap-2">
      <FileText className="h-5 w-5 text-indigo-600" />
      New Daily Log
     </h2>
     <DailyWorkflowForm 
      projectId={project.id} 
      phases={project.constructionPhases}
      onSuccess={() => {
       setShowAddForm(false);
       loadData(); // Refresh data after adding
      }}
     />
    </div>
   )}

   {progress.totalEstimatedDays > 0 ? (
    <div className="rounded-2xl border border-gray-100 bg-surface shadow-sm dark:border-border-strong bg-surface">
     <div className="border-b border-border px-6 py-4 dark:border-border-strong">
      <h2 className="text-lg font-semibold text-gray-900 dark:text-text-primary">Logbook History</h2>
     </div>
     <div className="p-6">
      <WorkflowHistory logs={logs} />
     </div>
    </div>
   ) : (
    <ProjectSetupScreen 
     projectId={project.id} 
     onSuccess={loadData} 
    />
   )}
  </div>
 );
};

const ProjectSetupScreen: React.FC<{ projectId: string, onSuccess: () => void }> = ({ projectId, onSuccess }) => {
 const [estimatedDays, setEstimatedDays] = useState(30);
 const [loading, setLoading] = useState(false);

 const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  setLoading(true);
  try {
   await constructorWorkflowService.setEstimatedDuration(projectId, estimatedDays);
   onSuccess();
  } catch (error) {
   console.error('Failed to set duration', error);
  } finally {
   setLoading(false);
  }
 };

 return (
  <div className="rounded-2xl border border-border bg-surface p-8 shadow-sm dark:border-border-strong bg-surface">
   <div className="mx-auto max-w-lg text-center">
    <h2 className="text-2xl font-bold text-gray-900 dark:text-text-primary">Project Setup Required</h2>
    <p className="mt-4 text-text-muted text-text-secondary">
     Before you can start logging daily progress, please provide an estimate of how many days it will take to complete this project.
    </p>
    
    <form onSubmit={handleSubmit} className="mt-8">
     <div className="mb-6">
      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
       Estimated Total Duration (Days)
      </label>
      <input
       type="number"
       min="1"
       required
       value={estimatedDays}
       onChange={(e) => setEstimatedDays(parseInt(e.target.value) || 0)}
       className="block w-full rounded-xl border border-border-strong bg-surface py-3 px-4 text-center text-xl font-bold text-gray-900 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500 border-border bg-surface-elevated dark:text-text-primary"
      />
     </div>
     <button
      type="submit"
      disabled={loading || estimatedDays < 1}
      className="w-full flex justify-center rounded-xl bg-indigo-600 px-8 py-3 text-sm font-semibold text-text-primary shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50"
     >
      {loading ? 'Saving...' : 'Set Estimate & Continue'}
     </button>
    </form>
   </div>
  </div>
 );
};

export default ConstructorProjectWorkflow;
