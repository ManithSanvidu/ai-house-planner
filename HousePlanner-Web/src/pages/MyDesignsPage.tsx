import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { RefreshCw } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { workflowService, type DesignHistoryDto, type WorkflowDesignHistoryDto } from '../services/workflowService';
import { countLabel, formatArea, formatGenerationMode, formatRoomName, formatTopology, formatWorkflowStatus } from '../utils/presentation';
import { formatDesignRef } from '../utils/referenceCode';
import { SHOW_TECHNICAL_PLAN } from '../config/features';

type PendingRemoval = { workflow: WorkflowDesignHistoryDto; design: DesignHistoryDto };

const MyDesignsPage: React.FC = () => {
 const [pendingRemoval, setPendingRemoval] = useState<PendingRemoval | null>(null);
 const [customError, setCustomError] = useState('');

 const { data: projects = [], isLoading: loading, error: queryError, refetch } = useQuery({
  queryKey: ['myDesigns'],
  queryFn: async ({ signal }) => await workflowService.getMyDesigns({ signal })
 });

 const error = customError || (queryError ? (queryError as any).message || 'Could not load saved designs.' : '');
 const load = async () => { setCustomError(''); await refetch(); };


 const submit = async (workflowId: string, designId: string) => {
  await workflowService.submitArchitectReview(workflowId, designId); await load();
 };

 const remove = async () => {
  if (!pendingRemoval) return;
  try {
   await workflowService.removeDesign(pendingRemoval.workflow.workflowId, pendingRemoval.design.designId);
   setPendingRemoval(null);
   await load();
  } catch (err: any) {
   setCustomError(err?.response?.data?.message || err.message || 'Failed to remove design');
   setPendingRemoval(null);
  }
 };

 if (loading) return <div className="p-4 md:p-10 text-center">Loading saved designs…</div>;
 return <div className="max-w-5xl mx-auto p-4 sm:p-7 pb-28">
  <div className="flex items-center justify-between mb-6"><div><h1 className="text-2xl md:text-3xl font-extrabold">My Designs</h1><p className="text-text-muted mt-1">Review and manage your saved house designs.</p></div><button onClick={load} aria-label="Refresh designs" className="p-3 rounded-xl border"><RefreshCw size={18}/></button></div>
  {error && <div role="alert" className="p-4 bg-red-50 text-red-700 rounded-xl">{error}</div>}
  {!projects.length && !error && <div className="rounded-2xl border border-dashed p-4 md:p-10 text-center"><p className="font-semibold">No designs yet</p><Link to="/dashboard/new-project" className="text-indigo-600">Create your first project</Link></div>}
  <div className="space-y-6">{projects.map(project => {
   const submitted = project.status === 'awaiting_architect_review' || project.architectReviewStatus === 'Pending' || project.architectReviewStatus === 'Under Review';
   const approved = project.status === 'approved';
   const rejected = project.architectReviewStatus === 'Rejected';
   return <section key={project.workflowId} className="max-w-5xl mx-auto rounded-2xl border bg-surface-elevated/60 p-3 sm:p-4 mb-4 shadow-sm">
    <div className="flex flex-wrap items-center justify-between gap-4 mb-4">
     <div><h2 className="font-bold">House Design Project</h2><div className="flex flex-wrap items-center gap-x-3 text-sm text-text-muted mt-1"><span>{project.designs.length} Design{project.designs.length === 1 ? '' : 's'}</span><span className="px-2 py-0.5 rounded border bg-surface-elevated text-xs">{formatWorkflowStatus(project.status)}</span></div></div>
     <div className="flex flex-wrap items-center gap-2">
      {approved?<span className="px-3 py-1 rounded-xl bg-emerald-100 text-emerald-800 font-bold text-sm">✓ Architect Approved</span>:submitted?<span className="px-3 py-1 rounded-xl bg-amber-100 text-amber-900 font-bold text-sm">Awaiting Architect Review</span>:null}
     </div>
    </div>
    {rejected&&<div className="mb-4 rounded-xl bg-red-50 text-red-800 p-3"><b>Design Needs Changes</b>{project.architectFeedback&&<p>Architect feedback: “{project.architectFeedback}”</p>}</div>}
    <div className="grid grid-cols-[repeat(auto-fit,minmax(280px,1fr))] gap-4">{project.designs.map(design => <DesignCard key={design.designId} design={design} workflow={project} onSubmit={() => submit(project.workflowId, design.designId)} onRemove={() => setPendingRemoval({ workflow: project, design })}/>)}</div>
   </section>;
  })}</div>

   {pendingRemoval && <RemovalModal pending={pendingRemoval} onCancel={() => setPendingRemoval(null)} onConfirm={remove}/>}
 </div>;
};

const DesignCard = ({ design, workflow, onSubmit, onRemove }: { design: DesignHistoryDto; workflow: WorkflowDesignHistoryDto; onSubmit: () => Promise<void>; onRemove: () => void }) => {
 const [isSubmitting, setIsSubmitting] = useState(false);
 const [submitError, setSubmitError] = useState('');
 const [submitSuccess, setSubmitSuccess] = useState(false);

 const handleSubmit = async () => {
  if (isSubmitting) return;
  setIsSubmitting(true);
  setSubmitError('');
  setSubmitSuccess(false);
  try {
   await onSubmit();
   setSubmitSuccess(true);
   setTimeout(() => setSubmitSuccess(false), 3000);
  } catch (err: any) {
   setSubmitError(err?.response?.data?.message || err.message || 'Failed to send to architect');
  } finally {
   setIsSubmitting(false);
  }
 };

 const submitted = workflow.status === 'awaiting_architect_review' && design.isPreferred; const approved = design.isArchitectApproved;
 const rejected = workflow.architectReviewStatus === 'Rejected' && design.isPreferred;
 const managementLocked = workflow.status === 'approved' || workflow.status === 'awaiting_architect_review' || rejected;
 return <article className={`flex flex-col h-full rounded-xl bg-surface bg-background border p-3 border-border`}>
  <div className="mb-2"><span className="text-xs uppercase text-text-secondary font-bold">Version {design.version} · {formatDesignRef(design.designId)}</span><h3 className="font-bold leading-tight">{formatTopology(design.topology)}</h3></div>
  {SHOW_TECHNICAL_PLAN && <MiniPlan design={design}/>}
  <div className="grid grid-cols-1 md:grid-cols-2 gap-x-2 gap-y-1 text-sm mb-3"><span>{countLabel(design.bedrooms,'Bedroom')}</span><span>{countLabel(design.bathrooms,'Bathroom')}</span><span>{countLabel(design.floorCount,'Floor')}</span><span>{formatArea(design.totalBuiltUpAreaSqft)}</span><span className="col-span-2 text-text-secondary">{formatGenerationMode(design.generationMode)}</span></div>

  {submitSuccess && <div className="mb-3 rounded-lg bg-emerald-50 text-emerald-700 p-2 text-xs font-bold border border-emerald-200">Sent to architect successfully!</div>}
  {submitError && <div className="mb-3 rounded-lg bg-red-50 text-red-700 p-2 text-xs font-bold border border-red-200">{submitError}</div>}

  {approved && <div className="mb-3 rounded-lg bg-emerald-500/10 px-3 py-2 text-xs font-bold text-emerald-500">✓ Architect Approved</div>}
  {submitted && <div className="mb-3 rounded-lg bg-amber-500/10 px-3 py-2 text-xs font-bold text-amber-500">Awaiting Architect Review</div>}
  <div className="mt-auto flex flex-col gap-2"><div className="flex gap-2"><Link to={`/dashboard/workflows/${workflow.workflowId}?design=${design.designId}`} className="flex-1 text-center px-3 py-2 rounded-lg bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-700 transition-colors">Open Design</Link>{approved?<Link to={`/dashboard/construction?design=${design.designId}`} className="flex-1 text-center px-3 py-2 rounded-lg bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 transition-colors">Find Constructor</Link>:!managementLocked?<button onClick={handleSubmit} disabled={isSubmitting} className="flex-1 px-3 py-2 rounded-lg bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-700 transition-colors disabled:opacity-50">{isSubmitting ? 'Sending...' : 'Send to Architect'}</button>:null}</div><div className="flex flex-wrap items-center gap-2"><Link to={`/dashboard/construction/${design.designId}/readiness`} className="px-3 py-1.5 rounded-lg bg-indigo-500/10 text-indigo-400 border border-indigo-500/30 hover:bg-indigo-500/20 text-xs font-semibold transition-colors">Readiness Planner</Link>{!managementLocked && <button onClick={onRemove} className="px-3 py-1.5 rounded-lg border border-red-900/30 text-red-500 hover:bg-red-500/10 text-xs font-semibold ml-auto transition-colors" aria-label={`Delete Version ${design.version}`}>Delete</button>}</div></div>
 </article>;
};

const MiniPlan = ({ design }: { design: DesignHistoryDto }) => {
 const rooms = design.previewRooms?.filter(room => room.floor === 1) || [];
 const maxX = Math.max(1, ...rooms.map(room => room.x + room.width)); const maxY = Math.max(1, ...rooms.map(room => room.y + room.length));
 return <svg aria-label={`Version ${design.version} floor-plan preview`} viewBox={`0 0 ${maxX} ${maxY}`} className="w-full h-24 mb-3 rounded-lg bg-slate-50 border">{rooms.map((room, index) => <g key={`${room.roomType}-${index}`}><rect x={room.x} y={room.y} width={room.width} height={room.length} fill={index % 2 ? '#e0e7ff' : '#eef2ff'} stroke="#64748b" strokeWidth=".18"/><text x={room.x + room.width / 2} y={room.y + room.length / 2} fontSize="1.3" textAnchor="middle" fill="#334155">{formatRoomName(room.roomType)}</text></g>)}</svg>;
};



const RemovalModal = ({ pending, onCancel, onConfirm }: { pending: PendingRemoval; onCancel: () => void; onConfirm: () => void }) => {
 const archive = pending.workflow.status === 'awaiting_architect_review' || pending.design.isArchitectApproved;
 const isApproved = pending.design.isArchitectApproved;
 return <div role="dialog" aria-label={`Delete Version ${pending.design.version}`} className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"><div className="max-w-md bg-surface rounded-2xl p-4 md:p-6"><h2 className="text-xl font-bold">{archive ? 'Archive' : 'Delete'} Version {pending.design.version}?</h2>
 {isApproved ? (
  <p className="text-text-secondary dark:text-zinc-300 mt-3">This design has been architect approved. Removing it will hide/archive it from My Designs, but approval history will be preserved.</p>
 ) : (
  <p className="text-text-secondary dark:text-zinc-300 mt-3">Are you sure you want to remove this design from My Designs? Approved or submitted designs are retained and archived instead of being hard-deleted.</p>
 )}
 <div className="flex justify-end gap-3 mt-6"><button onClick={onCancel} className="px-4 py-2 rounded-xl border">Cancel</button><button onClick={onConfirm} className="px-4 py-2 rounded-xl bg-red-600 text-text-primary font-bold">Remove Design</button></div></div></div>;
};

export default MyDesignsPage;
