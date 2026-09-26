import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { 
 Briefcase,
 Clock,
 CheckCircle2,
 AlertTriangle,
 ChevronRight,
 Activity,
 RefreshCw
} from 'lucide-react';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';
import type { ConstructorWorkflowProject } from '../../../services/constructorWorkflowService';
import type { CostSummaryDto } from '../../../services/workflowService';


interface ConstructionRequest {
 id: string;
 projectId: string;
 houseDesignId: string;
 status: string;
 requestedAt: string;
 customerName: string;
 designVersion: number | null;
 area: number;
 floorCount: number;
 declineReason: string | null;
 title: string;
 bedrooms: number;
 bathrooms: number;
 layoutJson: string | null;
 terrainType: string | null;
 planReference: string | null;
 layoutType: string | null;
 basePreDesignedPlanId: string | null;
 cost: CostSummaryDto | null;
}

export const ConstructorWorkflowDashboard: React.FC = () => {
 const [projects, setProjects] = useState<ConstructorWorkflowProject[]>([]);
 const [loading, setLoading] = useState(true);
 const [requests, setRequests] = useState<ConstructionRequest[]>([]);
 const [requestsError, setRequestsError] = useState('');


 const loadData = async () => {
  setLoading(true);
  setRequestsError('');
  try {
   const [data, incoming] = await Promise.all([
    constructorWorkflowService.getProjects(),
    constructorWorkflowService.getConstructorRequests()
   ]);
   setProjects(data);
   setRequests(incoming);
  } catch (error) {
   console.error('Failed to fetch data', error);
   setRequestsError('Construction data could not be loaded.');
  } finally {
   setLoading(false);
  }
 };

 useEffect(() => { void loadData(); }, []);



 if (loading) {
  return (
   <div className="flex h-64 items-center justify-center">
    <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent" />
   </div>
  );
 }

 const pendingRequests = requests.filter(r => r.status === 'Pending');
 const activeProjects = projects.filter(p => p.status !== 'Completed' && p.status !== 'Cancelled');
 const completedCount = projects.length - activeProjects.length;

 return (
  <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
   <div className="mb-8 flex items-center justify-between">
    <div>
     <h1 className="text-2xl font-bold text-gray-900 dark:text-text-primary">My Workflow</h1>
     <p className="mt-1 text-sm text-text-muted text-text-secondary">
      Manage and track daily logs for your active construction projects.
     </p>
    </div>
    <button onClick={() => void loadData()} aria-label="Refresh dashboard" className="rounded-xl border p-3 hover:bg-gray-50 dark:hover:bg-gray-800">
     <RefreshCw size={18} />
    </button>
   </div>

   {/* Construction Requests Section */}
   <section className="mb-8 rounded-2xl border bg-surface p-6 bg-surface dark:border-border-strong">
    <h2 className="text-lg font-semibold mb-4">Construction Requests</h2>
    {requestsError ? (
     <div className="text-center py-4">
      <p className="text-red-500 mb-3">{requestsError}</p>
      <button onClick={() => void loadData()} className="rounded-lg border px-4 py-2 text-sm hover:bg-gray-50">Try Again</button>
     </div>
    ) : !pendingRequests.length ? (
     <p className="text-sm text-text-muted">No new construction requests from customers.</p>
    ) : (
     <div className="space-y-3">
      {pendingRequests.map(request => (
       <div key={request.id} className="rounded-xl border p-5 flex flex-wrap justify-between items-center gap-4">
        <div className="space-y-1">
         <p className="text-xs font-semibold uppercase tracking-wider text-indigo-600 dark:text-indigo-400">New Construction Request</p>
         <h3 className="text-lg font-bold text-gray-900 dark:text-text-primary">{request.title}</h3>
         <p className="text-sm text-text-secondary dark:text-gray-300">Customer: <strong>{request.customerName}</strong></p>
         <div className="flex flex-wrap gap-3 text-sm text-text-muted text-text-secondary mt-2">
          {request.designVersion != null && <span>Version {request.designVersion}</span>}
          <span>{request.bedrooms} Bedrooms</span>
          <span>{request.bathrooms} Bathrooms</span>
          {request.floorCount > 0 && <span>{request.floorCount} {request.floorCount === 1 ? 'Floor' : 'Floors'}</span>}
          {request.area > 0 && <span>{Number(request.area).toLocaleString()} sq ft</span>}
         </div>
         <p className="text-xs text-text-secondary mt-2">Requested: {new Date(request.requestedAt).toLocaleDateString()}</p>
        </div>
        <div>
         <Link
          to={`/constructor/requests/${request.id}`}
          className="flex items-center gap-2 rounded-lg bg-indigo-600 px-5 py-2.5 text-text-primary text-sm font-semibold shadow-sm hover:bg-indigo-500 transition-colors"
         >
          View Request Details <ChevronRight size={16} />
         </Link>
        </div>
       </div>
      ))}
     </div>
    )}
   </section>

   {/* Stats */}
   <div className="mb-8 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
    <div className="rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface">
     <div className="flex items-center gap-4">
      <div className="rounded-xl bg-indigo-50 p-3 dark:bg-indigo-900/30">
       <Clock className="h-6 w-6 text-indigo-600 dark:text-indigo-400" />
      </div>
      <div>
       <p className="text-sm font-medium text-text-muted text-text-secondary">Pending Requests</p>
       <p className="text-2xl font-bold text-gray-900 dark:text-text-primary">{pendingRequests.length}</p>
      </div>
     </div>
    </div>

    <div className="rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface">
     <div className="flex items-center gap-4">
      <div className="rounded-xl bg-blue-50 p-3 dark:bg-blue-900/30">
       <Briefcase className="h-6 w-6 text-blue-600 dark:text-blue-400" />
      </div>
      <div>
       <p className="text-sm font-medium text-text-muted text-text-secondary">Active Projects</p>
       <p className="text-2xl font-bold text-gray-900 dark:text-text-primary">{activeProjects.length}</p>
      </div>
     </div>
    </div>

    <div className="rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface">
     <div className="flex items-center gap-4">
      <div className="rounded-xl bg-amber-50 p-3 dark:bg-amber-900/30">
       <AlertTriangle className="h-6 w-6 text-amber-600 dark:text-amber-400" />
      </div>
      <div>
       <p className="text-sm font-medium text-text-muted text-text-secondary">Delayed</p>
       <p className="text-2xl font-bold text-gray-900 dark:text-text-primary">0</p>
      </div>
     </div>
    </div>
    
    <div className="rounded-2xl border border-gray-100 bg-surface p-6 shadow-sm dark:border-border-strong bg-surface">
     <div className="flex items-center gap-4">
      <div className="rounded-xl bg-green-50 p-3 dark:bg-green-900/30">
       <CheckCircle2 className="h-6 w-6 text-green-600 dark:text-green-400" />
      </div>
      <div>
       <p className="text-sm font-medium text-text-muted text-text-secondary">Completed</p>
       <p className="text-2xl font-bold text-gray-900 dark:text-text-primary">{completedCount}</p>
      </div>
     </div>
    </div>
   </div>

   {/* Active Projects List */}
   <div className="overflow-hidden rounded-2xl border border-border bg-surface shadow-sm dark:border-border-strong bg-surface">
    <div className="border-b border-border bg-gray-50 px-6 py-4 dark:border-border-strong bg-surface-elevated/50">
     <h2 className="text-lg font-semibold text-gray-900 dark:text-text-primary">Active Projects</h2>
    </div>
    <ul className="divide-y divide-gray-200 dark:divide-gray-800">
     {projects.map((project) => (
      <li key={project.id}>
       <Link
        to={`/constructor/projects/${project.id}`}
        className="block p-6 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
       >
        <div className="flex items-center justify-between">
         <div>
          <h3 className="text-lg font-medium text-gray-900 dark:text-text-primary">
           Project {project.id.substring(0, 8)}
          </h3>
          <div className="mt-2 flex items-center gap-4 text-sm text-text-muted text-text-secondary">
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
         <ChevronRight className="h-5 w-5 text-text-secondary" />
        </div>
       </Link>
      </li>
     ))}
     
     {projects.length === 0 && (
      <li className="p-8 text-center text-text-muted text-text-secondary">
       No active projects. Accept a construction request above to get started.
      </li>
     )}
    </ul>
   </div>
  </div>
 );
};

export default ConstructorWorkflowDashboard;
