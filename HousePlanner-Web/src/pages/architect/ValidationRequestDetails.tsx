import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { validationRequestService } from '../../services/validationRequestService';
import { workflowService } from '../../services/workflowService';
import type { ValidationRequestDetails as ValidationRequestDetailsType } from '../../types/validation.types';
import { ArrowLeft, CheckCircle, XCircle, Clock, Home, Bed, User, Map, FileText } from 'lucide-react';
import CostBreakdownCard from '../../components/cost/CostBreakdownCard';
import { formatRoomName } from '../../utils/presentation';

const ValidationRequestDetails: React.FC = () => {
 const { id } = useParams<{ id: string }>();
 const navigate = useNavigate();
 
 const [request, setRequest] = useState<ValidationRequestDetailsType | null>(null);
 const [loading, setLoading] = useState(true);
 const [error, setError] = useState<string | null>(null);
 
 const [reviewNote, setReviewNote] = useState('');
 const [isSubmitting, setIsSubmitting] = useState(false);
 const [actionError, setActionError] = useState<string | null>(null);
 const [visualizationData, setVisualizationData] = useState<any>(null);

 useEffect(() => {
  const controller = new AbortController();

  const fetchDetails = async () => {
   try {
    if (!id) {
     setLoading(false);
     return;
    }
    const data = await validationRequestService.getById(id, controller.signal);
    if (controller.signal.aborted) return;
    setRequest(data);
   } catch {
    if (controller.signal.aborted) return;
    setError('Failed to load validation request details.');
   } finally {
    if (controller.signal.aborted) return;
    setLoading(false);
   }
  };

  fetchDetails();

  return () => controller.abort();
 }, [id]);

 // Poll visualization — mirrors WorkflowReviewPage behaviour exactly
 useEffect(() => {
  const designId = request?.design?.designId;
  if (!designId) return;

  const controller = new AbortController();
  let timer: ReturnType<typeof setTimeout> | undefined;

  const loadVisualization = async () => {
   if (controller.signal.aborted) return;

   try {
    const result = await workflowService.getDesignVisualization(designId, controller.signal);
    if (controller.signal.aborted) return;
    setVisualizationData(result);
    if (result.status === 'generating' && !controller.signal.aborted) {
     timer = setTimeout(loadVisualization, 3000);
    }
   } catch {
    if (!controller.signal.aborted) setVisualizationData({ status: 'failed', imageUrl: null });
   }
  };

  setVisualizationData(null);
  loadVisualization();

  return () => {
   controller.abort();
   if (timer) clearTimeout(timer);
  };
 }, [request?.design?.designId]);

 const handleApprove = async () => {
  if (!id) return;
  if (!window.confirm('Approve this submitted design? This decision is final.')) return;
  setIsSubmitting(true);
  setActionError(null);
  try {
   await validationRequestService.approve(id, reviewNote);
   navigate('/architect/requests');
  } catch (err: any) {
   setActionError(err.response?.data?.message || 'Failed to approve request.');
  } finally {
   setIsSubmitting(false);
  }
 };

 const handleReject = async () => {
  if (!id) return;
  if (!reviewNote.trim()) {
   setActionError('A review note is required to reject a design.');
   return;
  }
  
  setIsSubmitting(true);
  setActionError(null);
  try {
   await validationRequestService.reject(id, reviewNote);
   navigate('/architect/requests');
  } catch (err: any) {
   setActionError(err.response?.data?.message || 'Failed to reject request.');
  } finally {
   setIsSubmitting(false);
  }
 };

 const formatDate = (dateString?: string | null) => {
  if (!dateString) return 'N/A';
  const d = new Date(dateString);
  return isNaN(d.getTime()) ? 'Invalid Date' : d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'});
 };

 if (loading) {
  return (
   <div className="flex justify-center items-center h-[50vh]">
    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
   </div>
  );
 }

 if (error || !request) {
  return (
   <div className="p-6 max-w-5xl mx-auto">
    <div className="p-4 mb-4 text-sm text-red-800 rounded-lg bg-red-50 border border-red-200">
     {error || 'Request not found'}
    </div>
    <Link to="/architect/requests" className="text-indigo-600 hover:underline inline-flex items-center gap-1">
     <ArrowLeft size={16} /> Back to Requests
    </Link>
   </div>
  );
 }

 const isPending = request.status === 'Pending' || request.status === 'Under Review';
 const canApprove = request.approvalEligibility.canApprove;

 return (
  <div className="p-6 md:p-8 max-w-5xl mx-auto space-y-6">
   
   {/* Header */}
   <div className="flex flex-col md:flex-row md:items-start justify-between gap-4">
    <div>
     <Link to="/architect/requests" className="text-text-muted hover:text-gray-900 text-text-secondary dark:hover:text-text-primary inline-flex items-center gap-1.5 text-sm font-medium mb-3 transition-colors">
      <ArrowLeft size={16} /> Back to Requests
     </Link>
     <div className="flex items-center gap-3">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-text-primary">Validation Request</h1>
      <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
        request.status === 'Approved' ? 'bg-green-50 text-green-700 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800' :
        request.status === 'Rejected' ? 'bg-red-50 text-red-700 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800' :
        'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
       }`}>
       {request.status}
      </span>
     </div>
     <p className="text-sm text-text-secondary mt-1 flex items-center gap-1">
      <Clock size={14} /> Submitted on {formatDate(request.submissionDate)}
     </p>
    </div>
   </div>

   <div className="space-y-6">
    {/* Client & Land Details Card */}
     <div className="bg-surface border border-border dark:border-border-strong rounded-2xl shadow-sm p-6">
      <h2 className="text-lg font-bold text-gray-900 dark:text-text-primary flex items-center gap-2 mb-6">
       <FileText className="text-indigo-600" size={20} />
       Project Constraints
      </h2>
      
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
       
       <div className="space-y-4">
        <div className="flex items-start gap-3">
         <div className="p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg shrink-0">
          <User size={18} className="text-text-secondary" />
         </div>
         <div>
          <p className="text-xs font-medium text-text-secondary uppercase tracking-wider">Client Info</p>
          <p className="text-sm font-semibold text-gray-900 dark:text-text-primary mt-0.5">{request.clientName || 'Anonymous'}</p>
          <p className="text-xs text-text-secondary">{request.clientEmail}</p>
         </div>
        </div>
        
        <div className="flex items-start gap-3">
         <div className="p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg shrink-0">
          <Map size={18} className="text-text-secondary" />
         </div>
         <div>
          <p className="text-xs font-medium text-text-secondary uppercase tracking-wider">Land Details</p>
          <p className="text-sm font-semibold text-gray-900 dark:text-text-primary mt-0.5">{request.landSize ? `${request.landSize} Perches` : 'N/A'}</p>
          <p className="text-xs text-text-secondary">Terrain: {request.terrainType || 'N/A'}</p>
         </div>
        </div>
       </div>

       <div className="space-y-4">
        <div className="flex items-start gap-3">
         <div className="p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg shrink-0">
          <Home size={18} className="text-text-secondary" />
         </div>
         <div>
          <p className="text-xs font-medium text-text-secondary uppercase tracking-wider">Preferences</p>
          <p className="text-sm font-semibold text-gray-900 dark:text-text-primary mt-0.5">{request.style || 'Any Style'}</p>
          <p className="text-xs text-text-secondary">Budget: {request.budget ? `LKR ${request.budget.toLocaleString()}` : 'N/A'}</p>
         </div>
        </div>

        <div className="flex items-start gap-3">
         <div className="p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg shrink-0">
          <Bed size={18} className="text-text-secondary" />
         </div>
         <div>
          <p className="text-xs font-medium text-text-secondary uppercase tracking-wider">Requirements</p>
          <p className="text-sm font-semibold text-gray-900 dark:text-text-primary mt-0.5">{request.bedrooms ? `${request.bedrooms} Bedrooms` : 'N/A'}</p>
          <p className="text-xs text-text-secondary">{request.floors ? `${request.floors} Floors` : 'N/A'}</p>
         </div>
        </div>
       </div>

      </div>
     </div>

    {/* Main Review Area */}
    <div className="space-y-6">
     {/* Architectural Visualization */}
     {request.design && (
      <div className="bg-white dark:bg-white border border-slate-200 dark:border-slate-200 rounded-2xl shadow-sm p-6 md:p-8">
       <h2 className="text-lg font-bold text-slate-900 dark:text-slate-900 mb-5">Architectural Visualization</h2>
       <div className="bg-white rounded-xl overflow-hidden min-h-[420px] md:min-h-[520px] flex items-center justify-center">
        {visualizationData?.status === 'completed' && visualizationData?.imageUrl ? (
         <img
          src={visualizationData.imageUrl}
          alt="AI Architectural Visualization"
          className="w-full max-h-[720px] object-contain"
         />
        ) : visualizationData?.status === 'generating' || visualizationData === null ? (
         <div className="text-zinc-500 flex flex-col items-center gap-3">
          <div className="w-8 h-8 border-4 border-zinc-800 border-t-indigo-500 rounded-full animate-spin" />
          <span className="text-sm font-medium">Generating AI visualization…</span>
         </div>
        ) : (
         <div className="text-zinc-400 flex flex-col items-center gap-2 text-center p-6">
          <span className="text-base font-bold text-zinc-300">AI visualization unavailable</span>
          <span className="text-sm">The floor plan below is the validated deterministic layout.</span>
         </div>
        )}
       </div>
      </div>
     )}

     <section className="bg-white dark:bg-white border border-slate-200 dark:border-slate-200 rounded-2xl shadow-sm p-6 md:p-8">
      <h2 className="text-lg font-bold text-slate-900 dark:text-slate-900 mb-1">Generated Floor Plan</h2>
      <p className="text-sm text-slate-500 dark:text-slate-500 mb-4">Created by deterministic spatial planning engine</p>

      {request.design?.rooms?.length ? (
       <div className="overflow-x-auto rounded-xl border border-slate-200 dark:border-slate-200">
        <table className="w-full min-w-[440px] text-left text-sm">
         <thead className="bg-slate-50 text-xs uppercase tracking-wider text-slate-500">
          <tr>
           <th scope="col" className="px-4 py-3 font-bold">Room</th>
           <th scope="col" className="px-4 py-3 font-bold">Size</th>
           <th scope="col" className="px-4 py-3 font-bold">Position</th>
          </tr>
         </thead>
         <tbody className="divide-y divide-slate-100">
          {request.design.rooms.map(room => (
           <tr key={room.roomId} className="bg-white transition-colors hover:bg-slate-50">
            <td className="px-4 py-3 font-bold text-slate-800 whitespace-nowrap">{room.name || formatRoomName(room.roomType)}</td>
            <td className="px-4 py-3 text-slate-600 whitespace-nowrap">{room.width} ft × {room.length} ft</td>
            <td className="px-4 py-3 font-mono text-xs text-slate-500 whitespace-nowrap">X: {room.x} · Y: {room.y}</td>
           </tr>
          ))}
         </tbody>
        </table>
       </div>
      ) : (
       <div className="p-8 text-center text-slate-500 border border-dashed border-slate-200 rounded-xl">
        No active design generated for this request yet.
       </div>
      )}
     </section>
    </div>

    <CostBreakdownCard cost={request.cost} />

    {/* Architect Validation / Review Decision */}
    <div className="bg-surface border border-border dark:border-border-strong rounded-2xl shadow-sm p-6">
      <h2 className="text-lg font-bold text-gray-900 dark:text-text-primary mb-4">Architect Validation</h2>

      {isPending && (
       <div className={`mb-4 rounded-xl border p-4 ${canApprove ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-amber-200 bg-amber-50 text-amber-800'}`}>
        <p className="text-sm font-semibold">{canApprove ? 'Ready for architect decision' : 'Not ready for approval'}</p>
        <p className="mt-1 text-xs">
         {request.approvalEligibility.reason
          || (request.approvalEligibility.budgetStatus === 'over_budget'
           ? 'The estimate is over budget. Review the design and cost before deciding.'
           : 'The selected design has a cost estimate and can be approved.')}
        </p>
       </div>
      )}
      
      {actionError && (
       <div className="mb-4 p-3 text-sm text-red-800 rounded-lg bg-red-50 border border-red-200">
        {actionError}
       </div>
      )}

      {!isPending ? (
       <div className="space-y-4">
        <div className={`p-4 rounded-xl border ${
         request.status === 'Approved' ? 'bg-green-50 border-green-200 dark:bg-green-900/20 dark:border-green-800/50' : 'bg-red-50 border-red-200 dark:bg-red-900/20 dark:border-red-800/50'
        }`}>
         <p className={`text-sm font-semibold mb-2 flex items-center gap-1.5 ${
          request.status === 'Approved' ? 'text-green-800 dark:text-green-400' : 'text-red-800 dark:text-red-400'
         }`}>
          {request.status === 'Approved' ? <CheckCircle size={16} /> : <XCircle size={16} />}
          {request.status} on {formatDate(request.decisionAt)}
         </p>
         <div className="text-sm text-gray-700 dark:text-gray-300">
          <span className="font-semibold block mb-1">Architect Notes:</span>
          {request.architectReview || 'No notes provided.'}
         </div>
        </div>
       </div>
      ) : (
       <div className="space-y-4">
        <div>
         <label htmlFor="review" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
          Review Notes / Feedback
         </label>
         <textarea
          id="review"
          rows={5}
          className="block w-full rounded-xl border-border dark:border-border-strong bg-gray-50 dark:bg-gray-800/50 text-gray-900 dark:text-text-primary shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm p-3 outline-none"
          placeholder="Provide details about why this design is approved or rejected..."
          value={reviewNote}
          onChange={(e) => setReviewNote(e.target.value)}
          disabled={isSubmitting}
         />
         <p className="mt-1.5 text-xs text-text-muted">A note is required if you are rejecting the design.</p>
        </div>

        <div className="pt-2 flex flex-col gap-3">
         <button
          onClick={handleApprove}
          disabled={isSubmitting || !canApprove}
          className="w-full flex justify-center items-center gap-2 py-2.5 px-4 border border-transparent rounded-xl shadow-sm text-sm font-bold text-text-primary bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
         >
          {isSubmitting ? 'Processing...' : <><CheckCircle size={18} /> Approve Design</>}
         </button>
         <button
          onClick={handleReject}
          disabled={isSubmitting}
          className="w-full flex justify-center items-center gap-2 py-2.5 px-4 border border-border dark:border-border-strong rounded-xl shadow-sm text-sm font-bold text-gray-700 dark:text-gray-300 bg-surface-elevated hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
         >
          {isSubmitting ? 'Processing...' : <><XCircle size={18} /> Reject / Request Changes</>}
         </button>
        </div>
       </div>
      )}
      
    </div>
   </div>
  </div>
 );
};

export default ValidationRequestDetails;
