import React, { useState } from 'react';
import { Search, CheckCircle2, ArrowRight } from 'lucide-react';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';

export const ProjectSearch: React.FC = () => {
  const [projectId, setProjectId] = useState('');
  const [searchResult, setSearchResult] = useState<any>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [requestStatus, setRequestStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!projectId.trim()) return;

    setLoading(true);
    setError('');
    setSearchResult(null);
    setRequestStatus('idle');

    try {
      const result = await constructorWorkflowService.searchProject(projectId);
      setSearchResult(result);
    } catch (err: any) {
      setError(err.response?.data || 'Project not found. Please check the ID and try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleRequestAssignment = async () => {
    if (!searchResult) return;
    
    setRequestStatus('loading');
    try {
      await constructorWorkflowService.requestProject(searchResult.id);
      setRequestStatus('success');
    } catch (err: any) {
      setError(err.response?.data || 'Failed to send request. You may have already requested this project.');
      setRequestStatus('error');
    }
  };

  return (
    <div className="rounded-2xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 mb-8 overflow-hidden">
      <div className="border-b border-gray-200 bg-gray-50 px-6 py-4 dark:border-gray-800 dark:bg-gray-800/50">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Search & Request Project</h2>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Enter a Project ID provided by a client to request construction assignment.
        </p>
      </div>
      
      <div className="p-6">
        <form onSubmit={handleSearch} className="flex gap-4">
          <div className="relative flex-1">
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
              <Search className="h-5 w-5 text-gray-400" />
            </div>
            <input
              type="text"
              value={projectId}
              onChange={(e) => setProjectId(e.target.value)}
              className="block w-full rounded-lg border border-gray-300 bg-white py-2.5 pl-10 pr-3 text-sm placeholder-gray-500 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white dark:placeholder-gray-400 dark:focus:border-indigo-500 dark:focus:ring-indigo-500"
              placeholder="e.g. 123e4567-e89b-12d3-a456-426614174000"
            />
          </div>
          <button
            type="submit"
            disabled={loading || !projectId.trim()}
            className="flex items-center justify-center rounded-lg bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50"
          >
            {loading ? 'Searching...' : 'Search'}
          </button>
        </form>

        {error && (
          <div className="mt-4 rounded-lg bg-red-50 p-4 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
            {error}
          </div>
        )}

        {searchResult && (
          <div className="mt-6 rounded-xl border border-gray-100 bg-gray-50 p-5 dark:border-gray-800 dark:bg-gray-800/50">
            <h3 className="mb-4 text-sm font-medium text-gray-900 dark:text-white">Project Found</h3>
            
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">Project ID</p>
                <p className="font-mono text-sm text-gray-900 dark:text-white">{searchResult.id}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">Design Name</p>
                <p className="text-sm font-medium text-gray-900 dark:text-white">{searchResult.designName}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">Current Status</p>
                <p className="text-sm text-gray-900 dark:text-white">{searchResult.status}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">Created Date</p>
                <p className="text-sm text-gray-900 dark:text-white">
                  {new Date(searchResult.createdAt).toLocaleDateString()}
                </p>
              </div>
            </div>

            <div className="mt-6 border-t border-gray-200 pt-4 dark:border-gray-700">
              {requestStatus === 'idle' && (
                <button
                  onClick={handleRequestAssignment}
                  className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-500"
                >
                  Request Assignment
                  <ArrowRight className="h-4 w-4" />
                </button>
              )}
              
              {requestStatus === 'loading' && (
                <div className="flex items-center gap-2 text-sm text-gray-500">
                  <div className="h-4 w-4 animate-spin rounded-full border-2 border-indigo-600 border-t-transparent" />
                  Sending request...
                </div>
              )}
              
              {requestStatus === 'success' && (
                <div className="flex items-center gap-2 text-sm font-medium text-green-600 dark:text-green-400">
                  <CheckCircle2 className="h-5 w-5" />
                  Request sent successfully! Waiting for client approval.
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
