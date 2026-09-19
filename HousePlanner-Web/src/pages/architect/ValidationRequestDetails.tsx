import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { validationRequestService } from '../../services/validationRequestService';
import type { ValidationRequestDetails as ValidationRequestDetailsType } from '../../types/validation.types';
import { ArrowLeft, CheckCircle, XCircle, Clock, Ruler, Home, Bed, User, Map, FileText } from 'lucide-react';

const ValidationRequestDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  const [request, setRequest] = useState<ValidationRequestDetailsType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const [reviewNote, setReviewNote] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDetails = async () => {
      try {
        if (!id) return;
        const data = await validationRequestService.getById(id);
        setRequest(data);
      } catch {
        setError('Failed to load validation request details.');
      } finally {
        setLoading(false);
      }
    };
    fetchDetails();
  }, [id]);

  const handleApprove = async () => {
    if (!id) return;
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

  return (
    <div className="p-6 md:p-8 max-w-5xl mx-auto space-y-6">
      
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-start justify-between gap-4">
        <div>
          <Link to="/architect/requests" className="text-gray-500 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white inline-flex items-center gap-1.5 text-sm font-medium mb-3 transition-colors">
            <ArrowLeft size={16} /> Back to Requests
          </Link>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Validation Request</h1>
            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                request.status === 'Approved' ? 'bg-green-50 text-green-700 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800' :
                request.status === 'Rejected' ? 'bg-red-50 text-red-700 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800' :
                'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
              }`}>
              {request.status}
            </span>
          </div>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1 flex items-center gap-1">
            <Clock size={14} /> Submitted on {formatDate(request.submissionDate)}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        
        {/* Left Column: Details */}
        <div className="lg:col-span-2 space-y-6">
          
          {/* Client & Land Details Card */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-6">
            <h2 className="text-lg font-bold text-gray-900 dark:text-white flex items-center gap-2 mb-6">
              <FileText className="text-indigo-600" size={20} />
              Project Constraints
            </h2>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              
              <div className="space-y-4">
                <div className="flex items-start gap-3">
                  <div className="p-2 bg-gray-50 dark:bg-gray-800 rounded-lg shrink-0">
                    <User size={18} className="text-gray-500 dark:text-gray-400" />
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Client Info</p>
                    <p className="text-sm font-semibold text-gray-900 dark:text-white mt-0.5">{request.clientName || 'Anonymous'}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">{request.clientEmail}</p>
                  </div>
                </div>
                
                <div className="flex items-start gap-3">
                  <div className="p-2 bg-gray-50 dark:bg-gray-800 rounded-lg shrink-0">
                    <Map size={18} className="text-gray-500 dark:text-gray-400" />
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Land Details</p>
                    <p className="text-sm font-semibold text-gray-900 dark:text-white mt-0.5">{request.landSize ? `${request.landSize} Perches` : 'N/A'}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">Terrain: {request.terrainType || 'N/A'}</p>
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <div className="flex items-start gap-3">
                  <div className="p-2 bg-gray-50 dark:bg-gray-800 rounded-lg shrink-0">
                    <Home size={18} className="text-gray-500 dark:text-gray-400" />
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Preferences</p>
                    <p className="text-sm font-semibold text-gray-900 dark:text-white mt-0.5">{request.style || 'Any Style'}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">Budget: {request.budget ? `LKR ${request.budget.toLocaleString()}` : 'N/A'}</p>
                  </div>
                </div>

                <div className="flex items-start gap-3">
                  <div className="p-2 bg-gray-50 dark:bg-gray-800 rounded-lg shrink-0">
                    <Bed size={18} className="text-gray-500 dark:text-gray-400" />
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Requirements</p>
                    <p className="text-sm font-semibold text-gray-900 dark:text-white mt-0.5">{request.bedrooms ? `${request.bedrooms} Bedrooms` : 'N/A'}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">{request.floors ? `${request.floors} Floors` : 'N/A'}</p>
                  </div>
                </div>
              </div>

            </div>
          </div>

          {/* Design Layout JSON Preview */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-6">
            <h2 className="text-lg font-bold text-gray-900 dark:text-white flex items-center gap-2 mb-4">
              <Ruler className="text-indigo-600" size={20} />
              Proposed Design Layout
            </h2>
            
            {request.design ? (
              <div className="bg-gray-50 dark:bg-gray-950 border border-gray-200 dark:border-gray-800 rounded-xl p-4 overflow-x-auto">
                <pre className="text-xs text-gray-800 dark:text-gray-300 font-mono whitespace-pre-wrap">
                  {request.design.layoutJson ? JSON.stringify(JSON.parse(request.design.layoutJson), null, 2) : 'No layout JSON available.'}
                </pre>
              </div>
            ) : (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400 border border-dashed border-gray-200 dark:border-gray-800 rounded-xl">
                No active design generated for this request yet.
              </div>
            )}
          </div>
        </div>

        {/* Right Column: Actions */}
        <div className="space-y-6">
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-6 sticky top-6">
            <h2 className="text-lg font-bold text-gray-900 dark:text-white mb-4">Architect Validation</h2>
            
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
                    className="block w-full rounded-xl border-gray-300 dark:border-gray-700 bg-gray-50 dark:bg-gray-950 text-gray-900 dark:text-white shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm p-3 outline-none"
                    placeholder="Provide details about why this design is approved or rejected..."
                    value={reviewNote}
                    onChange={(e) => setReviewNote(e.target.value)}
                    disabled={isSubmitting}
                  />
                  <p className="mt-1.5 text-xs text-gray-500">A note is required if you are rejecting the design.</p>
                </div>

                <div className="pt-2 flex flex-col gap-3">
                  <button
                    onClick={handleApprove}
                    disabled={isSubmitting || !request.design}
                    className="w-full flex justify-center items-center gap-2 py-2.5 px-4 border border-transparent rounded-xl shadow-sm text-sm font-bold text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    {isSubmitting ? 'Processing...' : <><CheckCircle size={18} /> Approve Design</>}
                  </button>
                  <button
                    onClick={handleReject}
                    disabled={isSubmitting}
                    className="w-full flex justify-center items-center gap-2 py-2.5 px-4 border border-gray-300 dark:border-gray-700 rounded-xl shadow-sm text-sm font-bold text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    {isSubmitting ? 'Processing...' : <><XCircle size={18} /> Reject / Request Changes</>}
                  </button>
                </div>
              </div>
            )}
            
          </div>
        </div>
        
      </div>
    </div>
  );
};

export default ValidationRequestDetails;
