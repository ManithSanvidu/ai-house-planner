import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { 
  Briefcase,
  Clock,
  CheckCircle2,
  AlertTriangle,
  ChevronRight,
  Activity
} from 'lucide-react';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';
import type { ConstructorWorkflowProject } from '../../../services/constructorWorkflowService';

export const ConstructorWorkflowDashboard: React.FC = () => {
  const [projects, setProjects] = useState<ConstructorWorkflowProject[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchProjects = async () => {
      try {
        const data = await constructorWorkflowService.getProjects();
        setProjects(data);
      } catch (error) {
        console.error('Failed to fetch projects', error);
      } finally {
        setLoading(false);
      }
    };
    
    fetchProjects();
  }, []);

  if (loading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent" />
      </div>
    );
  }

  const activeProjects = projects.filter(p => p.status !== 'Completed');
  const delayedProjects = activeProjects.length > 0 ? 0 : 0; // TODO: Calculate dynamically based on progress

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">My Workflow</h1>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          Manage and track daily logs for your active construction projects.
        </p>
      </div>

      <div className="mb-8 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="flex items-center gap-4">
            <div className="rounded-xl bg-indigo-50 p-3 dark:bg-indigo-900/30">
              <Briefcase className="h-6 w-6 text-indigo-600 dark:text-indigo-400" />
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Active Projects</p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">{activeProjects.length}</p>
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="flex items-center gap-4">
            <div className="rounded-xl bg-amber-50 p-3 dark:bg-amber-900/30">
              <AlertTriangle className="h-6 w-6 text-amber-600 dark:text-amber-400" />
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Delayed</p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">{delayedProjects}</p>
            </div>
          </div>
        </div>
        
        <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="flex items-center gap-4">
            <div className="rounded-xl bg-green-50 p-3 dark:bg-green-900/30">
              <CheckCircle2 className="h-6 w-6 text-green-600 dark:text-green-400" />
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Completed</p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">{projects.length - activeProjects.length}</p>
            </div>
          </div>
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <div className="border-b border-gray-200 bg-gray-50 px-6 py-4 dark:border-gray-800 dark:bg-gray-800/50">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Active Projects</h2>
        </div>
        <ul className="divide-y divide-gray-200 dark:divide-gray-800">
          {projects.map((project) => (
            <li key={project.id}>
              <Link
                to={`/constructor/workflow/${project.id}`}
                className="block p-6 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-medium text-gray-900 dark:text-white">
                      Project {project.id.substring(0, 8)}
                    </h3>
                    <div className="mt-2 flex items-center gap-4 text-sm text-gray-500 dark:text-gray-400">
                      <span className="flex items-center gap-1">
                        <Activity className="h-4 w-4" />
                        {project.constructionPhases.length} Phases
                      </span>
                      <span className="flex items-center gap-1">
                        <Clock className="h-4 w-4" />
                        Started {new Date(project.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                  </div>
                  <ChevronRight className="h-5 w-5 text-gray-400" />
                </div>
              </Link>
            </li>
          ))}
          
          {projects.length === 0 && (
            <li className="p-8 text-center text-gray-500 dark:text-gray-400">
              No projects found. Validate some requests first.
            </li>
          )}
        </ul>
      </div>
    </div>
  );
};

export default ConstructorWorkflowDashboard;
