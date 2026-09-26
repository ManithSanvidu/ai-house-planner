import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import { FolderKanban, Trash2 } from 'lucide-react';
import { adminWorkflowService, type AdminWorkflowSummary } from '../services/adminWorkflowService';
import { toast } from 'react-hot-toast';

const AdminWorkflowsPage: React.FC = () => {
 const [workflows, setWorkflows] = useState<AdminWorkflowSummary[]>([]);
 const [loading, setLoading] = useState(true);

 useEffect(() => {
  fetchWorkflows();
 }, []);

 const fetchWorkflows = async () => {
  try {
   setLoading(true);
   const data = await adminWorkflowService.getAllWorkflows();
   setWorkflows(data);
  } catch (error) {
   console.error(error);
   toast.error('Failed to load customer workflows.');
  } finally {
   setLoading(false);
  }
 };

 const handleDelete = async (id: string) => {
  if (!window.confirm('Are you sure you want to delete this workflow? This cannot be undone.')) {
   return;
  }
  
  try {
   await adminWorkflowService.deleteWorkflow(id);
   toast.success('Workflow deleted successfully.');
   setWorkflows(prev => prev.filter(w => w.workflowId !== id));
  } catch (error) {
   console.error(error);
   toast.error('Failed to delete workflow.');
  }
 };

 return (
  <div className="p-8 max-w-7xl mx-auto min-h-screen">
   <div className="flex justify-between items-center mb-8">
    <div>
     <h1 className="text-3xl font-bold text-slate-800 dark:text-text-primary flex items-center gap-3">
      <FolderKanban className="text-indigo-600" />
      Customer Workflows
     </h1>
     <p className="text-text-muted mt-2">Manage and review all customer projects.</p>
    </div>
   </div>

   <div className="bg-surface rounded-2xl shadow-sm border border-slate-200 dark:border-border-strong overflow-hidden">
    {loading ? (
     <div className="p-8 text-center text-text-muted">Loading workflows...</div>
    ) : workflows.length === 0 ? (
     <div className="p-8 text-center text-text-muted">No workflows found.</div>
    ) : (
     <div className="overflow-x-auto">
      <table className="w-full text-left border-collapse">
       <thead>
        <tr className="bg-slate-50 bg-surface-elevated/50 border-b border-slate-200 dark:border-border-strong">
         <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Client</th>
         <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Status</th>
         <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Approval</th>
         <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Date</th>
         <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider text-right">Actions</th>
        </tr>
       </thead>
       <tbody className="divide-y divide-slate-200 dark:divide-gray-800">
        {workflows.map((workflow) => (
         <motion.tr 
          key={workflow.workflowId}
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className="hover:bg-slate-50/50 dark:hover:bg-gray-800/30 transition-colors"
         >
          <td className="px-6 py-4">
           <div className="font-semibold text-slate-800 dark:text-text-primary">{workflow.clientName}</div>
           <div className="text-sm text-text-muted">{workflow.clientEmail}</div>
          </td>
          <td className="px-6 py-4">
           <span className="px-3 py-1 rounded-full text-xs font-bold bg-indigo-100 text-indigo-800">
            {workflow.status.replace(/_/g, ' ')}
           </span>
          </td>
          <td className="px-6 py-4">
           <span className="px-3 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800">
            {workflow.approvalStatus.replace(/_/g, ' ')}
           </span>
          </td>
          <td className="px-6 py-4 text-sm text-text-muted">
           {new Date(workflow.createdAt).toLocaleDateString()}
          </td>
          <td className="px-6 py-4 text-right">
           <div className="flex items-center justify-end gap-3">
            <Link 
             to={`/dashboard/workflows/${workflow.workflowId}`}
             className="px-4 py-2 bg-slate-900 dark:bg-surface text-text-primary dark:text-slate-900 text-sm font-semibold rounded-lg hover:bg-slate-800 transition-colors"
            >
             View Plan
            </Link>
            <button
             onClick={() => handleDelete(workflow.workflowId)}
             className="p-2 text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 rounded-lg transition-colors"
             title="Delete Workflow"
            >
             <Trash2 size={20} />
            </button>
           </div>
          </td>
         </motion.tr>
        ))}
       </tbody>
      </table>
     </div>
    )}
   </div>
  </div>
 );
};

export default AdminWorkflowsPage;
