import React, { useEffect, useState } from 'react';
import { validationRequestService } from '../../services/validationRequestService';
import { useNavigate } from 'react-router-dom';
import type { ValidationRequest } from '../../types/validation.types';
import { Eye } from 'lucide-react';
// Force Vite HMR

const ValidationRequestsPage: React.FC = () => {
  const [requests, setRequests] = useState<ValidationRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchRequests = async () => {
      try {
        const pending = await validationRequestService.getAll('Pending');
        const underReview = await validationRequestService.getAll('Under Review');
        setRequests([...pending, ...underReview]);
      } catch (err) {
        setError('Failed to load validation requests.');
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
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
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
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Validation Requests</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Review and validate client house designs.</p>
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
                      req.status === 'Pending' 
                        ? 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
                        : 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800'
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
                      className="inline-flex items-center gap-1.5 text-white bg-indigo-600 hover:bg-indigo-700 focus:ring-4 focus:ring-indigo-300 font-medium rounded-lg text-xs px-4 py-2 transition-colors dark:focus:ring-indigo-800"
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
                        <svg className="w-6 h-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                      </div>
                      <p className="text-sm font-medium">No active validation requests found.</p>
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

export default ValidationRequestsPage;
