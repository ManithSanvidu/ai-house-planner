import React, { useEffect, useState } from 'react';
import { validationRequestService } from '../../services/validationRequestService';
import { useNavigate } from 'react-router-dom';
import type { ValidationRequest } from '../../types/validation.types';
import { Eye, CheckCircle2 } from 'lucide-react';

const ApprovedRequestsPage: React.FC = () => {
  const [requests, setRequests] = useState<ValidationRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchRequests = async () => {
      try {
        const approved = await validationRequestService.getAll('Approved');
        const rejected = await validationRequestService.getAll('Rejected');
        // Sort by submission date descending
        const allHistory = [...approved, ...rejected].sort((a, b) => 
          new Date(b.submissionDate).getTime() - new Date(a.submissionDate).getTime()
        );
        setRequests(allHistory);
      } catch {
        setError('Failed to load request history.');
      } finally {
        setLoading(false);
      }
    };
    fetchRequests();
  }, []);

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    const d = new Date(dateString);
    return isNaN(d.getTime()) ? 'Invalid Date' : d.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-[50vh]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-emerald-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 m-6 text-sm text-red-800 rounded-lg bg-red-50 border border-red-200" role="alert">
        {error}
      </div>
    );
  }

  return (
    <div className="p-6 md:p-8 max-w-7xl mx-auto space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <CheckCircle2 className="text-emerald-500" /> Validation History
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Review previously approved and rejected requests.</p>
        </div>
      </div>
      
      <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left text-gray-500 dark:text-gray-400">
            <thead className="text-xs text-gray-700 dark:text-gray-300 uppercase bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-800">
              <tr>
                <th scope="col" className="px-6 py-4 font-semibold">Client Name</th>
                <th scope="col" className="px-6 py-4 font-semibold">Submission Date</th>
                <th scope="col" className="px-6 py-4 font-semibold">Status</th>
                <th scope="col" className="px-6 py-4 font-semibold">Budget (LKR)</th>
                <th scope="col" className="px-6 py-4 font-semibold">Land Size</th>
                <th scope="col" className="px-6 py-4 text-right font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-gray-800">
              {requests.map((req) => (
                <tr key={req.id} className="bg-white dark:bg-gray-900 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                  <td className="px-6 py-4 font-medium text-gray-900 dark:text-white">
                    {req.clientName || 'Anonymous Client'}
                  </td>
                  <td className="px-6 py-4 text-gray-600 dark:text-gray-400">
                    {formatDate(req.submissionDate)}
                  </td>
                  <td className="px-6 py-4">
                    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                      req.status === 'Approved' 
                        ? 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-400 dark:border-emerald-800'
                        : 'bg-red-50 text-red-700 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800'
                    }`}>
                      {req.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-gray-600 dark:text-gray-400">
                    {req.budget ? req.budget.toLocaleString() : 'N/A'}
                  </td>
                  <td className="px-6 py-4 text-gray-600 dark:text-gray-400">
                    {req.landSize ? `${req.landSize} perches` : 'N/A'}
                  </td>
                  <td className="px-6 py-4 text-right">
                    <button 
                      onClick={() => navigate(`/architect/requests/${req.id}`)}
                      className="inline-flex items-center gap-1.5 text-white bg-slate-600 hover:bg-slate-700 focus:ring-4 focus:ring-slate-300 font-medium rounded-lg text-xs px-4 py-2 transition-colors dark:focus:ring-slate-800"
                    >
                      <Eye size={14} />
                      View Details
                    </button>
                  </td>
                </tr>
              ))}
              
              {requests.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center">
                    <div className="flex flex-col items-center justify-center text-gray-500 dark:text-gray-400">
                      <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-full mb-3">
                        <CheckCircle2 className="w-6 h-6 text-gray-400" />
                      </div>
                      <p className="text-sm font-medium">No validation history found.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default ApprovedRequestsPage;
