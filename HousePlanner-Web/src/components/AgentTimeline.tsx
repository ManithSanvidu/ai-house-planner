import React from 'react';
import type { AgentExecutionLogEntry } from '../services/workflowService';

interface AgentTimelineProps {
  agentExecutionLog?: AgentExecutionLogEntry[] | null;
}

export const AgentTimeline: React.FC<AgentTimelineProps> = ({ agentExecutionLog }) => {
  if (!agentExecutionLog || agentExecutionLog.length === 0) {
    return null;
  }

  const formatAgentName = (agent: string) => {
    return agent
      .split('_')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  };

  return (
    <div className="text-left mt-6 border-t border-border/50 pt-6">
      <h3 className="text-lg font-bold text-zinc-900 mb-4 tracking-tight">AI Design Process</h3>
      <div className="space-y-0">
        {agentExecutionLog.map((log, index) => {
          const isLast = index === agentExecutionLog.length - 1;
          
          return (
            <div key={index} className="flex gap-4">
              <div className="flex flex-col items-center">
                <div className={`flex items-center justify-center w-6 h-6 rounded-full border-2 shrink-0 ${
                  log.status === 'completed' 
                    ? 'bg-green-500 border-green-500 text-white' 
                    : log.status === 'failed'
                    ? 'bg-red-500 border-red-500 text-white'
                    : 'bg-transparent border-indigo-400 text-indigo-500'
                }`}>
                  {log.status === 'completed' && (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M5 13l4 4L19 7" />
                    </svg>
                  )}
                  {log.status === 'failed' && (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  )}
                  {log.status === 'running' && (
                    <div className="w-2 h-2 bg-indigo-500 rounded-full animate-pulse"></div>
                  )}
                </div>
                {!isLast && (
                  <div className={`w-0.5 h-full my-1 ${log.status === 'completed' ? 'bg-green-200' : 'bg-border'}`}></div>
                )}
              </div>
              <div className="pb-4">
                <p className={`font-semibold text-sm ${
                  log.status === 'failed' ? 'text-red-600' : 'text-zinc-800'
                }`}>
                  {formatAgentName(log.agent)}
                </p>
                <p className="text-xs text-text-secondary mt-0.5 leading-relaxed">{log.message}</p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
