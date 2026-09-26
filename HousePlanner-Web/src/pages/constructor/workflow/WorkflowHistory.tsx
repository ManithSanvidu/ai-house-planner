import React from 'react';
import type { ConstructorWorkflowLog } from '../../../services/constructorWorkflowService';
import { Calendar, FileText, AlertCircle, CheckCircle2 } from 'lucide-react';

interface WorkflowHistoryProps {
 logs: ConstructorWorkflowLog[];
}

export const WorkflowHistory: React.FC<WorkflowHistoryProps> = ({ logs }) => {
 if (logs.length === 0) {
  return (
   <div className="text-center py-8">
    <p className="text-text-muted text-text-secondary">No logs recorded for this project yet.</p>
   </div>
  );
 }

 return (
  <div className="space-y-8 relative before:absolute before:inset-0 before:ml-5 before:-translate-x-px md:before:mx-auto md:before:translate-x-0 before:h-full before:w-0.5 before:bg-gradient-to-b before:from-transparent before:via-gray-200 dark:before:via-gray-800 before:to-transparent">
   {logs.map((log) => (
    <div key={log.id} className="relative flex items-center justify-between md:justify-normal md:odd:flex-row-reverse group is-active">
     {/* Icon */}
     <div className="flex items-center justify-center w-10 h-10 rounded-full border border-white dark:border-gray-900 bg-indigo-100 dark:bg-indigo-900/50 text-indigo-500 dark:text-indigo-400 shadow shrink-0 md:order-1 md:group-odd:-translate-x-1/2 md:group-even:translate-x-1/2">
      <FileText className="w-5 h-5" />
     </div>
     
     {/* Card */}
     <div className="w-[calc(100%-4rem)] md:w-[calc(50%-2.5rem)] p-4 rounded-2xl border border-gray-100 dark:border-border-strong bg-surface shadow-sm">
      <div className="flex items-center justify-between mb-2">
       <span className="font-semibold text-gray-900 dark:text-text-primary flex items-center gap-2">
        <Calendar className="w-4 h-4 text-text-secondary" />
        {new Date(log.date).toLocaleDateString()}
       </span>
       <span className={`px-2 py-1 rounded text-xs font-medium ${
        log.status === 'Completed' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300' :
        log.status === 'In Progress' ? 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300' :
        'bg-gray-100 text-gray-800 bg-surface-elevated dark:text-gray-300'
       }`}>
        {log.status}
       </span>
      </div>
      
      <p className="text-sm font-medium text-indigo-600 dark:text-indigo-400 mb-2">
       Day {log.dayNumber} - {log.progressPercentage}% Progress
      </p>

      <div className="space-y-3 mt-4 text-sm">
       <div>
        <h4 className="font-medium text-gray-900 dark:text-text-primary mb-1 flex items-center gap-1">
         <CheckCircle2 className="w-4 h-4 text-green-500" />
         Work Completed
        </h4>
        <p className="text-text-secondary whitespace-pre-wrap">{log.completedWork}</p>
       </div>

       {log.challenges && (
        <div className="bg-amber-50 dark:bg-amber-900/10 p-3 rounded-lg">
         <h4 className="font-medium text-amber-900 dark:text-amber-300 mb-1 flex items-center gap-1">
          <AlertCircle className="w-4 h-4" />
          Challenges
         </h4>
         <p className="text-amber-800 dark:text-amber-400/80">{log.challenges}</p>
         {log.resolution && (
          <p className="mt-2 text-green-700 dark:text-green-400/80"><span className="font-medium">Resolution:</span> {log.resolution}</p>
         )}
        </div>
       )}

       {log.tomorrowPlan && (
        <div className="bg-gray-50 bg-surface-elevated/50 p-3 rounded-lg">
         <h4 className="font-medium text-gray-900 dark:text-text-primary mb-1">Plan for Tomorrow</h4>
         <p className="text-text-secondary">{log.tomorrowPlan}</p>
        </div>
       )}
      </div>
     </div>
    </div>
   ))}
  </div>
 );
};

export default WorkflowHistory;
