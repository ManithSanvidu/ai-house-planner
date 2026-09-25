import React, { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { constructorWorkflowService, dailyConstructionLogService } from '../../../services/constructorWorkflowService';
import type { ConstructorWorkflowProject, DailyConstructionLogDto, ConstructionPhase } from '../../../services/constructorWorkflowService';
import DailyLogbookForm from './DailyLogbookForm';
import { ArrowLeft, Plus, Calendar, Edit2, Trash2 } from 'lucide-react';
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
    if (isCompleted) return;
    setEditingLog(undefined);
    setShowForm(true);
    setActiveTab('list');
    // Pre-select the date when we mount the form... we can pass initialDate down if we want, but for now we'll rely on the default which is today.
    // The user can manually pick the date for now.
  };

  if (loading) {
    return <div className="flex h-64 items-center justify-center">Loading...</div>;
  }

  if (!project) {
    return <div className="p-8 text-center text-gray-500">Project not found or unauthorized.</div>;
  }

  return (
    <div className="mx-auto max-w-5xl space-y-8 p-4 md:p-8">
      <Link
        to="/constructor/dashboard"
        className="inline-flex items-center text-sm font-medium text-indigo-600 hover:text-indigo-500"
      >
        <ArrowLeft className="mr-2 h-4 w-4" />
        Back to Dashboard
      </Link>

      {/* Project Overview */}
      <div className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-900/5 dark:bg-gray-800 dark:ring-white/10">
        <div className="px-6 py-5">
          <h2 className="text-xl font-bold text-gray-900 dark:text-white">Project Overview</h2>
          <div className="mt-4 grid grid-cols-2 gap-4 text-sm sm:grid-cols-4">
            <div>
              <p className="text-gray-500 dark:text-gray-400">Status</p>
              <p className="font-medium text-gray-900 dark:text-white capitalize">{project.status}</p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Created</p>
              <p className="font-medium text-gray-900 dark:text-white">
                {new Date(project.createdAt).toLocaleDateString()}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Construction Phases */}
      <div className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-900/5 dark:bg-gray-800 dark:ring-white/10">
        <div className="px-6 py-5 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
          <h2 className="text-lg font-bold text-gray-900 dark:text-white">Construction Phases Schedule</h2>
          {project && (
            <div className="text-sm text-gray-500">
              Total Duration: {project.plannedTotalDurationDays} days
              {project.aiEstimatedTotalDurationDays !== project.plannedTotalDurationDays &&
                ` (AI est: ${project.aiEstimatedTotalDurationDays} days)`}
            </div>
          )}
        </div>
        <ul className="divide-y divide-gray-200 dark:divide-gray-700">
          {project.constructionPhases.map((phase: ConstructionPhase) => (
            <li key={phase.id} className="flex flex-col sm:flex-row sm:justify-between px-6 py-4 gap-4">
              <div className="flex-1">
                <span className="font-medium text-gray-900 dark:text-white">{phase.sequenceOrder}. {phase.phaseName}</span>
                <div className="text-sm text-gray-500 mt-1 flex gap-4">
                   <span>Start: {phase.plannedStartDate ? new Date(phase.plannedStartDate).toLocaleDateString() : 'N/A'}</span>
                   <span>End: {phase.plannedEndDate ? new Date(phase.plannedEndDate).toLocaleDateString() : 'N/A'}</span>
                </div>
              </div>

              <div className="flex items-center gap-4">
                {editingPhaseId === phase.id ? (
                  <div className="flex items-center gap-2">
                    <input
                      type="number"
                      min="1"
                      className="w-20 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-white"
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
                      className="text-sm bg-indigo-600 text-white px-3 py-1 rounded hover:bg-indigo-500 disabled:opacity-50"
                      disabled={savingPhase}
                    >
                      Save
                    </button>
                    <button
                      onClick={() => setEditingPhaseId(null)}
                      className="text-sm bg-gray-200 text-gray-700 px-3 py-1 rounded hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
                      disabled={savingPhase}
                    >
                      Cancel
                    </button>
                  </div>
                ) : (
                  <div className="flex flex-col items-end">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium">{phase.plannedDurationDays} days</span>
                      {!isCompleted && (
                        <button
                          onClick={() => {
                            setEditingPhaseId(phase.id);
                            setEditPhaseDuration(phase.plannedDurationDays);
                          }}
                          className="text-gray-400 hover:text-indigo-600"
                        >
                          <Edit2 className="h-4 w-4" />
                        </button>
                      )}
                    </div>
                    {phase.aiEstimatedDurationDays !== phase.plannedDurationDays && (
                      <span className="text-xs text-amber-600 dark:text-amber-500">
                        (AI est: {phase.aiEstimatedDurationDays} days)
                      </span>
                    )}
                  </div>
                )}
                <span className="text-sm text-gray-500 capitalize min-w-[80px] text-right">{phase.status}</span>
              </div>
            </li>
          ))}
          {project.constructionPhases.length === 0 && (
            <li className="px-6 py-4 text-gray-500">No phases defined.</li>
          )}
        </ul>
      </div>

      {/* Daily Logbook & Calendar Tabs */}
      <div className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-900/5 dark:bg-gray-800 dark:ring-white/10">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between px-6 py-5 border-b border-gray-200 dark:border-gray-700 gap-4">
          <div>
            <h2 className="text-lg font-bold text-gray-900 dark:text-white">Daily Workflow</h2>
            {isCompleted && <p className="text-sm text-amber-600 mt-1">Project completed. Logbook is read-only.</p>}
          </div>

          <div className="flex items-center gap-4 w-full sm:w-auto">
            {!showForm && (
              <div className="flex rounded-lg shadow-sm">
                <button
                  onClick={() => setActiveTab('list')}
                  className={`px-4 py-2 text-sm font-medium border border-gray-200 rounded-l-lg dark:border-gray-700 ${
                    activeTab === 'list'
                      ? 'bg-gray-100 text-gray-900 dark:bg-gray-700 dark:text-white'
                      : 'bg-white text-gray-500 hover:bg-gray-50 dark:bg-gray-800 dark:text-gray-400 dark:hover:bg-gray-700'
                  }`}
                >
                  List View
                </button>
                <button
                  onClick={() => setActiveTab('calendar')}
                  className={`px-4 py-2 text-sm font-medium border border-l-0 border-gray-200 rounded-r-lg dark:border-gray-700 ${
                    activeTab === 'calendar'
                      ? 'bg-gray-100 text-gray-900 dark:bg-gray-700 dark:text-white'
                      : 'bg-white text-gray-500 hover:bg-gray-50 dark:bg-gray-800 dark:text-gray-400 dark:hover:bg-gray-700'
                  }`}
                >
                  Calendar View
                </button>
              </div>
            )}

            {!isCompleted && !showForm && (
              <button
                onClick={() => { setEditingLog(undefined); setShowForm(true); setActiveTab('list'); }}
                className="inline-flex items-center rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500"
              >
                <Plus className="mr-2 h-4 w-4" /> Add Log
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
              isReadOnly={isCompleted}
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
              isReadOnly={isCompleted}
            />
          ) : (
            <div className="space-y-4">
              {logs.length === 0 ? (
                <div className="text-center py-8 text-gray-500">No daily logs have been recorded yet.</div>
              ) : (
                logs.map(log => (
                  <div key={log.id} className="rounded-xl border border-gray-200 p-5 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
                    <div className="flex justify-between items-start mb-4">
                      <div className="flex items-center text-sm font-medium text-gray-900 dark:text-white">
                        <Calendar className="mr-2 h-4 w-4 text-gray-500" />
                        {new Date(log.logDate).toLocaleDateString()}
                        {log.phaseName && <span className="ml-3 px-2 py-1 bg-indigo-100 text-indigo-700 rounded-md text-xs dark:bg-indigo-900/30 dark:text-indigo-300">{log.phaseName}</span>}
                      </div>
                      {!isCompleted && (
                        <div className="flex gap-2">
                          <button onClick={() => { setEditingLog(log); setShowForm(true); }} className="text-gray-400 hover:text-indigo-600">
                            <Edit2 className="h-4 w-4" />
                          </button>
                          <button onClick={() => handleDelete(log.id)} className="text-gray-400 hover:text-red-600">
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </div>
                      )}
                    </div>

                    <div className="space-y-3 text-sm">
                      <div>
                        <span className="font-semibold text-gray-900 dark:text-gray-200">Work Completed: </span>
                        <span className="text-gray-700 dark:text-gray-400">{log.workCompleted}</span>
                      </div>
                      {log.challenges && (
                        <div>
                          <span className="font-semibold text-gray-900 dark:text-gray-200">Challenges: </span>
                          <span className="text-gray-700 dark:text-gray-400">{log.challenges}</span>
                        </div>
                      )}
                      {log.materialsUsed && (
                        <div>
                          <span className="font-semibold text-gray-900 dark:text-gray-200">Materials: </span>
                          <span className="text-gray-700 dark:text-gray-400">{log.materialsUsed}</span>
                        </div>
                      )}
                      <div className="grid grid-cols-2 gap-4 mt-2 p-3 bg-white dark:bg-gray-800 rounded-lg">
                         <div><span className="text-gray-500">Progress:</span> {log.progressPercentage}%</div>
                         <div><span className="text-gray-500">Workers:</span> {log.workforceCount || 'N/A'}</div>
                      </div>
                      {log.tomorrowPlan && (
                        <div className="mt-3">
                          <span className="font-semibold text-gray-900 dark:text-gray-200">Plan for Tomorrow: </span>
                          <span className="text-gray-700 dark:text-gray-400">{log.tomorrowPlan}</span>
                        </div>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ConstructorProjectDetails;
