import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { constructorWorkflowService, dailyConstructionLogService } from '../../../services/constructorWorkflowService';
import type { ConstructorWorkflowProject, DailyConstructionLogDto, ConstructionPhase } from '../../../services/constructorWorkflowService';
import DailyLogbookForm from './DailyLogbookForm';
import { ArrowLeft, Plus, Calendar, Edit2, Trash2 } from 'lucide-react';
import toast from 'react-hot-toast';

export const ConstructorProjectDetails: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  
  const [project, setProject] = useState<ConstructorWorkflowProject | null>(null);
  const [logs, setLogs] = useState<DailyConstructionLogDto[]>([]);
  const [loading, setLoading] = useState(true);
  
  const [showForm, setShowForm] = useState(false);
  const [editingLog, setEditingLog] = useState<DailyConstructionLogDto | undefined>(undefined);

  const isCompleted = project?.status.toLowerCase() === 'completed';

  const loadData = async () => {
    if (!projectId) return;
    try {
      const p = await constructorWorkflowService.getProjectDetails(projectId);
      setProject(p);
      const l = await dailyConstructionLogService.getLogs(projectId);
      setLogs(l);
    } catch (error) {
      console.error('Failed to load project details', error);
      toast.error('Failed to load project details');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [projectId]);

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
        <div className="px-6 py-5 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-lg font-bold text-gray-900 dark:text-white">Construction Phases</h2>
        </div>
        <ul className="divide-y divide-gray-200 dark:divide-gray-700">
          {project.constructionPhases.map((phase: ConstructionPhase) => (
            <li key={phase.id} className="flex justify-between px-6 py-4">
              <span className="font-medium text-gray-900 dark:text-white">{phase.sequenceOrder}. {phase.phaseName}</span>
              <span className="text-sm text-gray-500 capitalize">{phase.status}</span>
            </li>
          ))}
          {project.constructionPhases.length === 0 && (
            <li className="px-6 py-4 text-gray-500">No phases defined.</li>
          )}
        </ul>
      </div>

      {/* Daily Logbook */}
      <div className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-900/5 dark:bg-gray-800 dark:ring-white/10">
        <div className="flex items-center justify-between px-6 py-5 border-b border-gray-200 dark:border-gray-700">
          <div>
            <h2 className="text-lg font-bold text-gray-900 dark:text-white">Daily Logbook</h2>
            {isCompleted && <p className="text-sm text-amber-600 mt-1">Project completed. Daily logbook is read-only.</p>}
          </div>
          {!isCompleted && !showForm && (
            <button
              onClick={() => { setEditingLog(undefined); setShowForm(true); }}
              className="inline-flex items-center rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500"
            >
              <Plus className="mr-2 h-4 w-4" /> Add Daily Log
            </button>
          )}
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
