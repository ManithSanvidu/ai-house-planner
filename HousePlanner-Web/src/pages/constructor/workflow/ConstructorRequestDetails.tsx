import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';
import CostBreakdownCard from '../../../components/cost/CostBreakdownCard';
import { ArrowLeft, Check, X } from 'lucide-react';
import { FloorPlanViewer } from '../../../components/floorplan/FloorPlanViewer';

export const ConstructorRequestDetails: React.FC = () => {
  const { requestId } = useParams<{ requestId: string }>();
  const navigate = useNavigate();
  const [request, setRequest] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    const fetchRequest = async () => {
      try {
        if (!requestId) return;
        const data = await constructorWorkflowService.getConstructorRequest(requestId);
        setRequest(data);
      } catch (err) {
        setError('Failed to load request details.');
      } finally {
        setLoading(false);
      }
    };
    void fetchRequest();
  }, [requestId]);

  const handleAccept = async () => {
    if (!requestId) return;
    setActionLoading(true);
    try {
      await constructorWorkflowService.acceptRequest(requestId);
      navigate('/constructor/dashboard');
    } catch (e: any) {
      alert(e.response?.data?.message || 'Could not accept request.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleDecline = async () => {
    if (!requestId) return;
    const reason = window.prompt('Optional reason for declining') || undefined;
    setActionLoading(true);
    try {
      await constructorWorkflowService.declineRequest(requestId, reason);
      navigate('/constructor/dashboard');
    } catch (e: any) {
      alert(e.response?.data?.message || 'Could not decline request.');
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent" />
      </div>
    );
  }

  if (error || !request) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-red-500">{error || 'Request not found'}</p>
        <Link to="/constructor/dashboard" className="text-indigo-600 hover:underline">Back to Dashboard</Link>
      </div>
    );
  }

  const parsedLayout = request.layoutJson ? JSON.parse(request.layoutJson) : null;

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <Link to="/constructor/dashboard" className="inline-flex items-center gap-2 text-sm text-gray-500 hover:text-gray-900 dark:hover:text-white mb-6">
        <ArrowLeft size={16} /> Back to Dashboard
      </Link>

      <div className="flex flex-col lg:flex-row gap-8">
        {/* Left Column: Floor Plan */}
        <div className="flex-1 space-y-6">
          <div className="rounded-2xl border bg-white p-6 dark:bg-gray-900 dark:border-gray-800">
            <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4">Floor Plan</h2>
            <div className="aspect-square w-full rounded-xl border border-gray-100 bg-gray-50/50 dark:border-gray-800 dark:bg-gray-900/50 overflow-hidden">
              {parsedLayout ? (
                <FloorPlanViewer data={parsedLayout} />
              ) : (
                <div className="flex h-full items-center justify-center text-gray-500">
                  No layout geometry available.
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right Column: Details & Actions */}
        <div className="w-full lg:w-[400px] space-y-6">
          <div className="rounded-2xl border bg-white p-6 dark:bg-gray-900 dark:border-gray-800">
            <p className="text-xs font-semibold uppercase tracking-wider text-indigo-600 dark:text-indigo-400 mb-1">New Construction Request</p>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{request.title}</h1>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-2">Customer: <strong className="text-gray-900 dark:text-white">{request.customerName}</strong></p>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Requested: {new Date(request.requestedAt).toLocaleDateString()}</p>
            
            <div className="mt-6 border-t pt-6 dark:border-gray-800">
              <h3 className="text-sm font-semibold mb-3 dark:text-white">Design Information</h3>
              <dl className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <dt className="text-gray-500 dark:text-gray-400">Bedrooms</dt>
                  <dd className="font-medium dark:text-white">{request.bedrooms}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-gray-500 dark:text-gray-400">Bathrooms</dt>
                  <dd className="font-medium dark:text-white">{request.bathrooms}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-gray-500 dark:text-gray-400">Floors</dt>
                  <dd className="font-medium dark:text-white">{request.floorCount}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-gray-500 dark:text-gray-400">Built-up Area</dt>
                  <dd className="font-medium dark:text-white">{Number(request.area).toLocaleString()} sq ft</dd>
                </div>
                {request.terrainType && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500 dark:text-gray-400">Terrain / Site</dt>
                    <dd className="font-medium dark:text-white capitalize">{request.terrainType}</dd>
                  </div>
                )}
                {request.planReference && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500 dark:text-gray-400">Plan Reference</dt>
                    <dd className="font-medium dark:text-white">{request.planReference}</dd>
                  </div>
                )}
                {request.layoutType && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500 dark:text-gray-400">Layout Type</dt>
                    <dd className="font-medium dark:text-white">{request.layoutType}</dd>
                  </div>
                )}
              </dl>
            </div>
            
            <div className="mt-6 border-t pt-6 dark:border-gray-800">
              <h3 className="text-sm font-semibold mb-3 dark:text-white">Estimated Cost</h3>
              {request.cost ? (
                <CostBreakdownCard cost={request.cost} hideTitle />
              ) : (
                <p className="text-sm text-gray-500">Cost estimate is not available yet.</p>
              )}
            </div>

            {request.status === 'Pending' && (
              <div className="mt-8 flex gap-3">
                <button
                  onClick={handleDecline}
                  disabled={actionLoading}
                  className="flex-1 flex items-center justify-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-semibold hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-50"
                >
                  <X size={16} /> Decline
                </button>
                <button
                  onClick={handleAccept}
                  disabled={actionLoading}
                  className="flex-1 flex items-center justify-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-white text-sm font-semibold hover:bg-emerald-500 disabled:opacity-50"
                >
                  <Check size={16} /> Accept Request
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
export default ConstructorRequestDetails;
