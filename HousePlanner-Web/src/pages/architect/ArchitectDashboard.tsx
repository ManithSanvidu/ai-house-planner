import React, { useEffect, useState } from 'react';
import { validationRequestService } from '../../services/validationRequestService';
import { Link } from 'react-router-dom';

const ArchitectDashboard: React.FC = () => {
 const [stats, setStats] = useState({ pending: 0, underReview: 0, approved: 0, rejected: 0 });
 const [loading, setLoading] = useState(true);
 const [error, setError] = useState<string | null>(null);

 useEffect(() => {
  const fetchStats = async () => {
   try {
    const pending = await validationRequestService.getAll('Pending');
    const underReview = await validationRequestService.getAll('Under Review');
    const approved = await validationRequestService.getAll('Approved');
    const rejected = await validationRequestService.getAll('Rejected');
    
    setStats({
     pending: pending.length,
     underReview: underReview.length,
     approved: approved.length,
     rejected: rejected.length
    });
   } catch {
    setError('Failed to load dashboard statistics.');
   } finally {
    setLoading(false);
   }
  };
  fetchStats();
 }, []);

 if (loading) {
  return (
   <div className="flex justify-center items-center h-64">
    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
   </div>
  );
 }

 if (error) {
  return (
   <div className="p-4 mb-4 text-sm text-red-800 rounded-lg bg-red-50" role="alert">
    {error}
   </div>
  );
 }

 return (
  <div className="p-6">
   <h1 className="text-2xl font-bold text-gray-900 mb-6">Architect Dashboard</h1>
   <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
    
    <Link to="/architect/requests" className="bg-surface p-6 rounded-lg shadow-sm border border-border hover:border-indigo-300 hover:shadow-md transition-all cursor-pointer">
     <p className="text-sm font-medium text-text-muted mb-1">Pending Requests</p>
     <p className="text-3xl font-bold text-indigo-600">{stats.pending}</p>
    </Link>

    <Link to="/architect/requests" className="bg-surface p-6 rounded-lg shadow-sm border border-border hover:border-indigo-300 hover:shadow-md transition-all cursor-pointer">
     <p className="text-sm font-medium text-text-muted mb-1">Under Review</p>
     <p className="text-3xl font-bold text-indigo-600">{stats.underReview}</p>
    </Link>

    <Link to="/architect/approved" className="bg-surface p-6 rounded-lg shadow-sm border border-border hover:border-emerald-300 hover:shadow-md transition-all cursor-pointer">
     <p className="text-sm font-medium text-text-muted mb-1">Approved</p>
     <p className="text-3xl font-bold text-emerald-600">{stats.approved}</p>
    </Link>

    <Link to="/architect/approved" className="bg-surface p-6 rounded-lg shadow-sm border border-border hover:border-red-300 hover:shadow-md transition-all cursor-pointer">
     <p className="text-sm font-medium text-text-muted mb-1">Rejected</p>
     <p className="text-3xl font-bold text-red-600">{stats.rejected}</p>
    </Link>

   </div>
  </div>
 );
};

export default ArchitectDashboard;
