import { useCallback, useEffect, useState } from 'react';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';
import { CheckCircle2, Clock, Check } from 'lucide-react';

export const ConstructorRequestsView = ({ projectId }: { projectId: string }) => {
 const [requests, setRequests] = useState<any[]>([]);
 const [loading, setLoading] = useState(true);
 const [error, setError] = useState('');

 const loadRequests = useCallback(async () => {
  try {
   const data = await constructorWorkflowService.getProjectRequests(projectId);
   setRequests(data);
  } catch {
   setError('Failed to load constructor requests.');
  } finally {
   setLoading(false);
  }
 }, [projectId]);

 useEffect(() => {
  loadRequests();
 }, [loadRequests]);

 const handleApprove = async (requestId: string) => {
  try {
   await constructorWorkflowService.approveRequest(requestId);
   await loadRequests();
  } catch (err: any) {
   alert(err.response?.data || 'Failed to approve request.');
  }
 };

 if (loading) return <div className="text-sm text-text-muted py-2">Loading constructor requests...</div>;
 if (error) return <div className="text-sm text-red-500 py-2">{error}</div>;
 
 if (requests.length === 0) {
  return (
   <div className="mt-4 rounded-xl border border-dashed border-border-strong bg-gray-50 p-6 text-center border-border bg-surface-elevated/50">
    <p className="text-sm text-text-muted text-text-secondary">No constructor requests yet.</p>
    <p className="text-xs text-text-secondary mt-1">Share Project ID <strong>{projectId}</strong> with your constructor.</p>
   </div>
  );
 }

 return (
  <div className="mt-4 rounded-xl border border-border bg-surface shadow-sm dark:border-border-strong bg-surface">
   <div className="border-b border-gray-100 bg-gray-50 px-4 py-3 dark:border-border-strong bg-surface-elevated/50 flex justify-between items-center">
    <h3 className="text-sm font-semibold text-gray-900 dark:text-text-primary">Constructor Requests</h3>
    <span className="text-xs font-mono bg-indigo-100 text-indigo-700 px-2 py-1 rounded dark:bg-indigo-900/30 dark:text-indigo-400">ID: {projectId}</span>
   </div>
   <div className="divide-y divide-gray-100 dark:divide-gray-800">
    {requests.map((req) => (
     <div key={req.id} className="flex items-center justify-between p-4">
      <div>
       <div className="flex items-center gap-2">
        <p className="text-sm font-medium text-gray-900 dark:text-text-primary">
         Constructor: {req.constructorEmail}
        </p>
        {req.status === 'pending' && <span className="flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800 dark:bg-amber-900/30 dark:text-amber-400"><Clock className="h-3 w-3" /> Pending</span>}
        {req.status === 'approved' && <span className="flex items-center gap-1 rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800 dark:bg-green-900/30 dark:text-green-400"><CheckCircle2 className="h-3 w-3" /> Approved</span>}
        {req.status === 'rejected' && <span className="flex items-center gap-1 rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800 dark:bg-red-900/30 dark:text-red-400">Rejected</span>}
       </div>
       <p className="text-xs text-text-muted text-text-secondary mt-1">
        Requested on {new Date(req.requestedAt).toLocaleDateString()}
       </p>
      </div>
      {req.status === 'pending' && (
       <button
        onClick={() => handleApprove(req.id)}
        className="flex items-center gap-1 rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-text-primary shadow-sm hover:bg-indigo-500"
       >
        <Check className="h-3.5 w-3.5" /> Approve
       </button>
      )}
     </div>
    ))}
   </div>
  </div>
 );
};
