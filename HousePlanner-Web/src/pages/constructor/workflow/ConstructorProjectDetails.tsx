import React, { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { constructorWorkflowService, dailyConstructionLogService } from '../../../services/constructorWorkflowService';
import type { ConstructorWorkflowProject, DailyConstructionLogDto, ConstructionPhase } from '../../../services/constructorWorkflowService';
import DailyLogbookForm from './DailyLogbookForm';
import { ArrowLeft, Plus, Calendar, Edit2, Trash2, CheckCircle, PlayCircle } from 'lucide-react';
import toast from 'react-hot-toast';

import ConstructorProjectCalendar from './ConstructorProjectCalendar';
import type { CalendarEventDto } from '../../../services/constructorWorkflowService';

export const ConstructorProjectDetails: React.FC = () => {
 const { projectId } = useParams<{ projectId: string }>();

 const [project, setProject] = useState<ConstructorWorkflowProject | null>(null);
 const [logs, setLogs] = useState<DailyConstructionLogDto[]>([]);
 const [calendarEvents, setCalendarEvents] = useState<CalendarEventDto[]>([]);
 const [loading, setLoading] = useState(true);

 const [showForm, setShowForm] = useState(false);
 const [editingLog, setEditingLog] = useState<DailyConstructionLogDto | undefined>(undefined);
 const [activeTab, setActiveTab] = useState<'list' | 'calendar'>('list');

 const isCompleted = project?.status.toLowerCase() === 'completed';
 const isCancelled = project?.status.toLowerCase() === 'cancelled';
 const isReadOnly = isCompleted || isCancelled;

 const [editingPhaseId, setEditingPhaseId] = useState<string | null>(null);
 const [editPhaseDuration, setEditPhaseDuration] = useState<number>(0);
 const [savingPhase, setSavingPhase] = useState(false);

 const loadData = useCallback(async () => {
  if (!projectId) return;
  try {
   const p = await constructorWorkflowService.getProjectDetails(projectId);
   setProject(p);
   const l = await dailyConstructionLogService.getLogs(projectId);
   setLogs(l);
   const c = await dailyConstructionLogService.getProjectCalendar(projectId);
   setCalendarEvents(c);
  } catch (error) {
   console.error('Failed to load project details', error);
   toast.error('Failed to load project details');
  } finally {
   setLoading(false);
  }
 }, [projectId]);

 useEffect(() => {
  loadData();
 }, [loadData]);

 const handleDelete = async (logId: string) => {
  if (!projectId) return;
  if (window.confirm('Are you sure you want to delete this log?')) {
   try {
    await dailyConstructionLogService.deleteLog(projectId, logId);
    toast.success('Log deleted successfully');
    loadData();
   } catch (e: any) {
    if (e.response?.status === 409) {
      toast.error('Project completed. Daily logbook is read-only.');
    } else {
      toast.error('Failed to delete log');
    }
   }
  }
 };

 const handleCalendarEventClick = (logId: string) => {
  const log = logs.find(l => l.id === logId);
  if (log) {
   setEditingLog(log);
   setShowForm(true);
   setActiveTab('list');
  }
 };

 const handleCalendarDateClick = (_dateStr: string) => {
  if (isReadOnly) return;
  setEditingLog(undefined);
  setShowForm(true);
  setActiveTab('list');
 };

 const handleStatusChange = async (phaseId: string, newStatus: string) => {
  if (!projectId) return;
  try {
   await constructorWorkflowService.updatePhaseStatus(projectId, phaseId, newStatus);
   toast.success('Phase status updated');
   loadData();
  } catch (e: any) {
   toast.error(e.response?.data?.message || 'Failed to update phase status');
  }
 };

 if (loading) {
  return <div className="flex h-64 items-center justify-center text-text-secondary">Loading...</div>;
 }

 if (!project) {
  return <div className="p-8 text-center text-text-muted">Project not found or unauthorized.</div>;
 }

 // Calculate Progress and Current Phase
 const totalDuration = project.constructionPhases.reduce((acc, p) => acc + p.plannedDurationDays, 0);
 const completedDuration = project.constructionPhases
  .filter(p => p.status === 'Completed')
  .reduce((acc, p) => acc + p.plannedDurationDays, 0);
 const progressPercentage = totalDuration > 0 ? Math.round((completedDuration / totalDuration) * 100) : 0;

 const currentPhaseObj = project.constructionPhases.find(p => p.status === 'InProgress') 
  || project.constructionPhases.find(p => p.status === 'Pending');
 const currentPhaseName = currentPhaseObj ? currentPhaseObj.phaseName : 'Completed';

 return (
  <div className="mx-auto max-w-5xl space-y-8 p-4 md:p-8 min-h-[calc(100vh-64px)] bg-background text-text-secondary">
   <Link
    to="/constructor/dashboard"
    className="inline-flex items-center text-sm font-medium text-purple-400 hover:text-purple-300"
   >
    <ArrowLeft className="mr-2 h-4 w-4" />
    Back to Dashboard
   </Link>

   {/* Project Overview */}
   <div className="overflow-hidden rounded-2xl bg-surface border border-border-strong shadow-lg p-6">
    <h2 className="text-xl font-bold text-text-primary mb-6">Project Overview</h2>
    <div className="grid grid-cols-2 gap-6 sm:grid-cols-4">
     <div className="bg-surface-elevated/50 rounded-xl p-4 border border-border/50">
      <p className="text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">Status</p>
      <p className="font-bold text-text-primary capitalize">{project.status}</p>
     </div>
     <div className="bg-surface-elevated/50 rounded-xl p-4 border border-border/50">
      <p className="text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">Current Phase</p>
      <p className="font-bold text-text-primary">{currentPhaseName}</p>
     </div>
     <div className="bg-surface-elevated/50 rounded-xl p-4 border border-border/50">
      <p className="text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">Overall Progress</p>
      <div className="flex items-center gap-2">
       <span className="font-bold text-text-primary">{progressPercentage}%</span>
       <div className="h-1.5 w-full bg-slate-700 rounded-full overflow-hidden">
        <div className="h-full bg-purple-500 rounded-full" style={{ width: `${progressPercentage}%` }}></div>
       </div>
      </div>
     </div>
     <div className="bg-surface-elevated/50 rounded-xl p-4 border border-border/50">
      <p className="text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">Planned Completion</p>
      <p className="font-bold text-text-primary">
       {project.constructionPhases.length > 0 && project.constructionPhases[project.constructionPhases.length - 1].plannedEndDate 
        ? new Date(project.constructionPhases[project.constructionPhases.length - 1].plannedEndDate!).toLocaleDateString()
        : 'N/A'}
      </p>
     </div>
    </div>
   </div>

   {isCancelled && (
    <div className="bg-rose-900/30 border border-rose-800 rounded-xl p-4 text-rose-400 text-sm font-medium text-center">
     Construction cancelled. Project history is read-only.
    </div>
   )}

   {/* Construction Phases */}
   <div className="overflow-hidden rounded-2xl bg-surface border border-border-strong shadow-lg">
    <div className="px-6 py-5 border-b border-border-strong flex justify-between items-center bg-surface-elevated/30">
     <h2 className="text-lg font-bold text-text-primary">Construction Phases Schedule</h2>
     <div className="text-xs font-medium text-text-secondary flex gap-4">
       <span>Total Planned: {project.plannedTotalDurationDays}d</span>
       {project.aiEstimatedTotalDurationDays !== project.plannedTotalDurationDays && (
        <span className="text-purple-400">AI Est: {project.aiEstimatedTotalDurationDays}d</span>
       )}
     </div>
    </div>
    
    <div className="divide-y divide-slate-800/50">
     {project.constructionPhases.map((phase: ConstructionPhase) => (
      <div key={phase.id} className="flex flex-col sm:flex-row sm:items-center sm:justify-between px-6 py-5 gap-4 hover:bg-surface-elevated/30 transition-colors">
       <div className="flex items-start gap-4">
        <div className="flex-shrink-0 w-8 h-8 rounded-full bg-slate-800 flex items-center justify-center text-xs font-bold text-text-secondary">
         {phase.sequenceOrder.toString().padStart(2, '0')}
        </div>
        <div>
         <div className="flex items-center gap-2">
          <span className="font-bold text-text-primary">{phase.phaseName}</span>
          {phase.status === 'Completed' && <CheckCircle className="w-4 h-4 text-emerald-400" />}
          {phase.status === 'InProgress' && <PlayCircle className="w-4 h-4 text-purple-400" />}
         </div>
         <div className="text-xs text-text-secondary mt-1.5 flex items-center gap-2">
          <Calendar className="w-3.5 h-3.5" />
          <span>
           {phase.plannedStartDate ? new Date(phase.plannedStartDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' }) : 'N/A'} 
           {' – '} 
           {phase.plannedEndDate ? new Date(phase.plannedEndDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' }) : 'N/A'}
          </span>
         </div>
        </div>
       </div>

       <div className="flex items-center gap-6 sm:ml-auto">
        {/* Duration Edit Block */}
        {editingPhaseId === phase.id ? (
         <div className="flex items-center gap-2">
          <input
           type="number"
           min="1"
           className="w-16 rounded-lg bg-background border border-border focus:border-purple-500 focus:ring-purple-500 text-sm text-text-primary px-2 py-1"
           value={editPhaseDuration}
           onChange={(e) => setEditPhaseDuration(parseInt(e.target.value) || 0)}
           disabled={savingPhase}
          />
          <button
           onClick={async () => {
            if (editPhaseDuration < 1) return toast.error('Duration must be at least 1 day');
            setSavingPhase(true);
            try {
             await constructorWorkflowService.updatePhaseSchedule(projectId!, phase.id, editPhaseDuration);
             toast.success('Phase schedule updated');
             setEditingPhaseId(null);
             loadData();
            } catch {
             toast.error('Failed to update phase schedule');
            } finally {
             setSavingPhase(false);
            }
           }}
           className="text-xs bg-purple-600 text-text-primary px-2.5 py-1.5 rounded-lg hover:bg-purple-500 disabled:opacity-50 font-semibold"
           disabled={savingPhase}
          >
           Save
          </button>
          <button
           onClick={() => setEditingPhaseId(null)}
           className="text-xs bg-slate-700 text-text-secondary px-2.5 py-1.5 rounded-lg hover:bg-slate-600 font-semibold"
           disabled={savingPhase}
          >
           Cancel
          </button>
         </div>
        ) : (
         <div className="flex flex-col items-end">
          <div className="flex items-center gap-2">
           <span className="text-sm font-medium text-text-secondary">Planned: {phase.plannedDurationDays}d</span>
           {!isReadOnly && (
            <button
             onClick={() => {
              setEditingPhaseId(phase.id);
              setEditPhaseDuration(phase.plannedDurationDays);
             }}
             className="text-text-muted hover:text-purple-400 text-xs font-semibold uppercase tracking-wider"
            >
             Edit
            </button>
           )}
          </div>
          {phase.aiEstimatedDurationDays !== phase.plannedDurationDays && (
           <span className="text-xs font-medium text-text-muted mt-0.5">
            AI Est: {phase.aiEstimatedDurationDays}d
           </span>
          )}
         </div>
        )}
        
        {/* Status Dropdown */}
        <div className="min-w-[130px]">
         {isReadOnly || phase.status === 'Completed' ? (
          <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold ${
           phase.status === 'Completed' ? 'bg-emerald-900/30 text-emerald-400 ring-1 ring-emerald-800' :
           phase.status === 'InProgress' ? 'bg-purple-900/30 text-purple-400 ring-1 ring-purple-800' :
           'bg-slate-800 text-text-secondary ring-1 ring-border'
          }`}>
            {phase.status === 'InProgress' ? 'In Progress' : phase.status}
          </span>
         ) : (
          <select
           value={phase.status}
           onChange={(e) => handleStatusChange(phase.id, e.target.value)}
           className={`text-xs font-bold rounded-lg border-0 ring-1 ring-inset focus:ring-2 focus:ring-inset py-1.5 pl-3 pr-8 ${
            phase.status === 'InProgress' ? 'bg-purple-900/20 text-purple-400 ring-purple-800 focus:ring-purple-500' :
            'bg-background text-text-secondary ring-border focus:ring-purple-500'
           }`}
          >
           <option value="Pending" disabled={phase.status !== 'Pending'}>Pending</option>
           <option value="InProgress">In Progress</option>
           <option value="Completed" disabled={phase.status === 'Pending'}>Completed</option>
          </select>
         )}
        </div>
       </div>
      </div>
     ))}
     {project.constructionPhases.length === 0 && (
      <div className="px-6 py-8 text-center text-text-muted">No phases defined.</div>
     )}
    </div>
   </div>

   {/* Daily Logbook */}
   <div className="overflow-hidden rounded-2xl bg-surface border border-border-strong shadow-lg">
    <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between px-6 py-5 border-b border-border-strong gap-4 bg-surface-elevated/30">
     <div>
      <h2 className="text-lg font-bold text-text-primary">Daily Site Progress</h2>
      <p className="text-xs text-text-secondary mt-1">Record daily work, workforce, materials, challenges, and progress.</p>
     </div>

     <div className="flex items-center gap-4 w-full sm:w-auto">
      {!showForm && (
       <div className="flex rounded-lg shadow-sm ring-1 ring-border">
        <button
         onClick={() => setActiveTab('list')}
         className={`px-4 py-1.5 text-xs font-bold uppercase tracking-wider rounded-l-lg transition-colors ${
          activeTab === 'list'
           ? 'bg-surface-muted text-text-primary'
           : 'bg-surface-elevated text-text-secondary hover:text-text-primary'
         }`}
        >
         List
        </button>
        <button
         onClick={() => setActiveTab('calendar')}
         className={`px-4 py-1.5 text-xs font-bold uppercase tracking-wider rounded-r-lg transition-colors border-l border-border ${
          activeTab === 'calendar'
           ? 'bg-surface-muted text-text-primary'
           : 'bg-surface-elevated text-text-secondary hover:text-text-primary'
         }`}
        >
         Calendar
        </button>
       </div>
      )}

      {!isReadOnly && !showForm && (
       <button
        onClick={() => { setEditingLog(undefined); setShowForm(true); setActiveTab('list'); }}
        className="inline-flex items-center rounded-lg bg-purple-600 px-3 py-1.5 text-xs font-bold text-text-primary shadow-sm hover:bg-purple-500 transition-colors uppercase tracking-wider"
       >
        <Plus className="mr-1.5 h-3.5 w-3.5" /> Add Log
       </button>
      )}
     </div>
    </div>

    <div className="p-6">
     {showForm ? (
      <DailyLogbookForm
       projectId={projectId!}
       phases={project.constructionPhases}
       existingLog={editingLog}
       isReadOnly={isReadOnly}
       onSuccess={() => {
        setShowForm(false);
        setEditingLog(undefined);
        loadData();
       }}
       onCancel={() => {
        setShowForm(false);
        setEditingLog(undefined);
       }}
      />
     ) : activeTab === 'calendar' ? (
      <ConstructorProjectCalendar
       events={calendarEvents}
       onEventClick={handleCalendarEventClick}
       onDateClick={handleCalendarDateClick}
       isReadOnly={isReadOnly}
      />
     ) : (
      <div className="space-y-4">
       {logs.length === 0 ? (
        <div className="text-center py-12 text-text-muted bg-surface-elevated/30 rounded-xl border border-border-strong border-dashed">
         No daily logs have been recorded yet.
        </div>
       ) : (
        logs.map(log => {
         const phaseObj = project.constructionPhases.find(p => p.id === log.constructionPhaseId);
         const isPhaseInProgress = phaseObj?.status === 'InProgress';
         
         return (
         <div key={log.id} className="rounded-xl border border-border-strong p-5 bg-surface-elevated/40 hover:bg-surface-elevated/60 transition-colors">
          <div className="flex justify-between items-start mb-4 pb-4 border-b border-border-strong/50">
           <div className="flex items-center text-sm font-bold text-text-primary">
            <Calendar className="mr-2 h-4 w-4 text-purple-400" />
            {new Date(log.logDate).toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' })}
            {log.phaseName && (
             <span className={`ml-4 px-2.5 py-1 rounded-md text-[10px] uppercase tracking-wider font-bold ${isPhaseInProgress ? 'bg-purple-900/30 text-purple-400 ring-1 ring-purple-800' : 'bg-slate-800 text-text-secondary ring-1 ring-border'}`}>
              {log.phaseName}
             </span>
            )}
           </div>
           {!isReadOnly && (
            <div className="flex gap-1">
             <button onClick={() => { setEditingLog(log); setShowForm(true); }} className="text-text-muted hover:text-purple-400 p-1.5 rounded-lg hover:bg-slate-800 transition-colors">
              <Edit2 className="h-3.5 w-3.5" />
             </button>
             <button onClick={() => handleDelete(log.id)} className="text-text-muted hover:text-rose-400 p-1.5 rounded-lg hover:bg-slate-800 transition-colors">
              <Trash2 className="h-3.5 w-3.5" />
             </button>
            </div>
           )}
          </div>

          <div className="space-y-4 text-sm">
           <div>
            <span className="font-semibold text-text-secondary block mb-1">Work Completed</span>
            <p className="text-text-secondary">{log.workCompleted}</p>
           </div>
           
           <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {log.challenges && (
             <div>
              <span className="font-semibold text-text-secondary block mb-1 flex items-center gap-1.5">
               <span className="w-1.5 h-1.5 rounded-full bg-rose-500"></span> Challenges
              </span>
              <p className="text-text-secondary">{log.challenges}</p>
             </div>
            )}
            {log.materialsUsed && (
             <div>
              <span className="font-semibold text-text-secondary block mb-1 flex items-center gap-1.5">
               <span className="w-1.5 h-1.5 rounded-full bg-amber-500"></span> Materials Used
              </span>
              <p className="text-text-secondary">{log.materialsUsed}</p>
             </div>
            )}
           </div>

           <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-4 p-4 bg-background rounded-xl border border-border-strong/80">
             <div>
              <p className="text-[10px] uppercase font-bold tracking-widest text-text-muted mb-1">Progress</p>
              <p className="font-bold text-text-primary text-base">{log.progressPercentage}%</p>
             </div>
             <div>
              <p className="text-[10px] uppercase font-bold tracking-widest text-text-muted mb-1">Workers</p>
              <p className="font-bold text-text-primary text-base">{log.workforceCount || '-'}</p>
             </div>
             <div className="col-span-2">
              {log.tomorrowPlan && (
               <>
                <p className="text-[10px] uppercase font-bold tracking-widest text-text-muted mb-1">Tomorrow's Plan</p>
                <p className="text-text-secondary text-xs mt-0.5">{log.tomorrowPlan}</p>
               </>
              )}
             </div>
           </div>
          </div>
         </div>
         );
        })
       )}
      </div>
     )}
    </div>
   </div>
  </div>
 );
};

export default ConstructorProjectDetails;
