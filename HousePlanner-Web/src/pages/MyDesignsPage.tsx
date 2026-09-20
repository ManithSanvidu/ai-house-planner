import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Archive, CheckCircle2, RefreshCw, Trash2, X } from 'lucide-react';
import { workflowService, type DesignHistoryDto, type WorkflowDesignHistoryDto } from '../services/workflowService';

type PendingRemoval = { workflow: WorkflowDesignHistoryDto; design: DesignHistoryDto };
type Comparison = { workflowId: string; designs: DesignHistoryDto[] } | null;

const MyDesignsPage: React.FC = () => {
  const [projects, setProjects] = useState<WorkflowDesignHistoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [compareIds, setCompareIds] = useState<Record<string, string[]>>({});
  const [comparison, setComparison] = useState<Comparison>(null);
  const [pendingRemoval, setPendingRemoval] = useState<PendingRemoval | null>(null);

  const load = async () => {
    setLoading(true);
    try { setProjects(await workflowService.getMyDesigns()); setError(''); }
    catch (err: any) { setError(err.message || 'Could not load saved designs.'); }
    finally { setLoading(false); }
  };
  useEffect(() => { void load(); }, []);

  const select = async (workflowId: string, designId: string) => {
    await workflowService.selectDesign(workflowId, designId); await load();
  };
  const unselect = async (workflowId: string) => {
    await workflowService.clearDesignSelection(workflowId); await load();
  };
  const submit = async (workflowId: string) => {
    await workflowService.submitArchitectReview(workflowId); await load();
  };
  const remove = async () => {
    if (!pendingRemoval) return;
    await workflowService.removeDesign(pendingRemoval.workflow.workflowId, pendingRemoval.design.designId);
    setPendingRemoval(null);
    setCompareIds(previous => ({ ...previous, [pendingRemoval.workflow.workflowId]: (previous[pendingRemoval.workflow.workflowId] || []).filter(id => id !== pendingRemoval.design.designId) }));
    await load();
  };
  const toggleCompare = (workflowId: string, designId: string) => setCompareIds(previous => {
    const selected = previous[workflowId] || [];
    if (selected.includes(designId)) return { ...previous, [workflowId]: selected.filter(id => id !== designId) };
    if (selected.length === 2) return previous;
    return { ...previous, [workflowId]: [...selected, designId] };
  });
  const openCompare = (project: WorkflowDesignHistoryDto) => {
    const ids = compareIds[project.workflowId] || [];
    if (ids.length === 2) setComparison({ workflowId: project.workflowId, designs: ids.map(id => project.designs.find(d => d.designId === id)!).filter(Boolean) });
  };
  const allCompared = Object.values(compareIds).flat();
  const activeCompareProject = projects.find(project => (compareIds[project.workflowId] || []).length > 0);

  if (loading) return <div className="p-10 text-center">Loading saved designs…</div>;
  return <div className="max-w-[1500px] mx-auto p-4 sm:p-7 pb-28">
    <div className="flex items-center justify-between mb-6"><div><h1 className="text-3xl font-extrabold">My Designs</h1><p className="text-zinc-500 mt-1">Review, compare, and manage every saved version.</p></div><button onClick={load} aria-label="Refresh designs" className="p-3 rounded-xl border"><RefreshCw size={18}/></button></div>
    {error && <div role="alert" className="p-4 bg-red-50 text-red-700 rounded-xl">{error}</div>}
    {!projects.length && !error && <div className="rounded-2xl border border-dashed p-10 text-center"><p className="font-semibold">No designs yet</p><Link to="/dashboard/new-project" className="text-indigo-600">Create your first project</Link></div>}
    <div className="space-y-6">{projects.map(project => {
      const selected = project.designs.find(d => d.isPreferred);
      const submitted = project.status === 'awaiting_architect_review';
      const approved = project.status === 'approved';
      const compareCount = (compareIds[project.workflowId] || []).length;
      return <section key={project.workflowId} className="rounded-2xl border bg-zinc-50/60 dark:bg-gray-900/50 p-4 sm:p-5">
        <div className="flex flex-wrap items-center justify-between gap-4 mb-4">
          <div><h2 className="font-bold">Project {project.workflowId.slice(0, 8)}</h2><div className="flex flex-wrap gap-x-4 text-sm text-zinc-500 mt-1"><span>{project.designs.length} saved design{project.designs.length === 1 ? '' : 's'}</span><span>{selected ? `Selected: Version ${selected.version}` : 'No design selected'}</span><span>{formatWorkflowStatus(project.status)}</span></div></div>
          <div className="flex flex-wrap gap-2"><Link to={`/dashboard/workflows/${project.workflowId}`} className="px-4 py-2 rounded-xl border bg-white dark:bg-gray-950 font-semibold text-sm">Generate Another</Link><button onClick={() => submit(project.workflowId)} disabled={!selected || submitted || approved} className="px-4 py-2 rounded-xl bg-emerald-600 text-white font-semibold text-sm disabled:opacity-40">Submit Selected to Architect</button></div>
        </div>
        <div className="grid sm:grid-cols-2 xl:grid-cols-4 gap-3">{project.designs.map(design => <DesignCard key={design.designId} design={design} workflow={project} compared={compareIds[project.workflowId]?.includes(design.designId) || false} compareFull={compareCount === 2} onCompare={() => toggleCompare(project.workflowId, design.designId)} onSelect={() => select(project.workflowId, design.designId)} onUnselect={() => unselect(project.workflowId)} onRemove={() => setPendingRemoval({ workflow: project, design })}/>)}</div>
        {approved && project.projectId && (
          <div className="mt-6 border-t border-gray-200 pt-6 dark:border-gray-800">
            <ConstructorRequestsView projectId={project.projectId} />
          </div>
        )}
      </section>;
    })}</div>

    {allCompared.length > 0 && activeCompareProject && <div className="fixed bottom-5 left-1/2 -translate-x-1/2 z-30 rounded-2xl bg-zinc-950 text-white shadow-2xl px-5 py-3 flex items-center gap-4"><span className="font-bold">{(compareIds[activeCompareProject.workflowId] || []).length} designs selected</span><button onClick={() => openCompare(activeCompareProject)} disabled={(compareIds[activeCompareProject.workflowId] || []).length !== 2} className="px-4 py-2 rounded-xl bg-indigo-500 font-bold disabled:opacity-40">Compare Now</button><button onClick={() => setCompareIds({})} className="px-3 py-2 text-zinc-300">Clear</button></div>}
    {comparison && <CompareModal comparison={comparison} onClose={() => setComparison(null)} onSelect={async designId => { await select(comparison.workflowId, designId); setComparison(null); }}/>} 
    {pendingRemoval && <RemovalModal pending={pendingRemoval} onCancel={() => setPendingRemoval(null)} onConfirm={remove}/>} 
  </div>;
};

const DesignCard = ({ design, workflow, compared, compareFull, onCompare, onSelect, onUnselect, onRemove }: { design: DesignHistoryDto; workflow: WorkflowDesignHistoryDto; compared: boolean; compareFull: boolean; onCompare: () => void; onSelect: () => void; onUnselect: () => void; onRemove: () => void }) => {
  const submitted = workflow.status === 'awaiting_architect_review'; const approved = workflow.status === 'approved';
  return <article className={`rounded-2xl bg-white dark:bg-gray-950 border p-4 ${design.isPreferred ? 'border-emerald-500 ring-1 ring-emerald-500' : compared ? 'border-indigo-500 ring-1 ring-indigo-500' : 'border-zinc-200 dark:border-gray-700'}`}>
    <div className="flex justify-between gap-2"><div><span className="text-xs uppercase text-zinc-400 font-bold">Version {design.version}</span><h3 className="font-bold mt-1">{formatTopology(design.topology)}</h3></div>{design.isPreferred && <span className="text-emerald-600 flex gap-1 text-xs font-bold"><CheckCircle2 aria-hidden="true" size={16}/>Selected</span>}</div>
    <MiniPlan design={design}/>
    <dl className="grid grid-cols-2 gap-2 text-sm my-3"><div><dt className="text-zinc-400">Rooms</dt><dd>{countLabel(design.bedrooms,'Bedroom')} · {countLabel(design.bathrooms,'Bathroom')}</dd></div><div><dt className="text-zinc-400">Floors</dt><dd>{countLabel(design.floorCount,'Floor')}</dd></div><div><dt className="text-zinc-400">Area</dt><dd>{formatArea(design.totalBuiltUpAreaSqft)}</dd></div><div><dt className="text-zinc-400">Created as</dt><dd>{formatGenerationMode(design.generationMode)}</dd></div></dl>
    <p className="text-xs text-zinc-400 mb-3">{new Date(design.createdAt).toLocaleString()}</p>
    <div className="flex flex-wrap gap-2"><Link to={`/dashboard/workflows/${workflow.workflowId}?design=${design.designId}`} className="px-3 py-2 rounded-lg border text-xs font-semibold">Preview</Link><button onClick={onCompare} disabled={!compared && compareFull} className={`px-3 py-2 rounded-lg border text-xs font-semibold disabled:opacity-40 ${compared ? 'bg-indigo-50 text-indigo-700' : ''}`}>{compared ? 'Remove Compare' : 'Add to Compare'}</button>{design.isPreferred ? <button onClick={onUnselect} className="px-3 py-2 rounded-lg bg-zinc-700 text-white text-xs font-semibold">Unselect</button> : <button onClick={onSelect} className="px-3 py-2 rounded-lg bg-indigo-600 text-white text-xs font-semibold">Select</button>}<button onClick={onRemove} disabled={approved} title={approved ? 'Approved designs cannot be deleted' : undefined} className="p-2 rounded-lg border text-red-600 disabled:opacity-35" aria-label={`${submitted ? 'Archive' : 'Delete'} Version ${design.version}`}>{submitted ? <Archive size={15}/> : <Trash2 size={15}/>}</button></div>
  </article>;
};

const MiniPlan = ({ design }: { design: DesignHistoryDto }) => {
  const rooms = design.previewRooms?.filter(room => room.floor === 1) || [];
  const maxX = Math.max(1, ...rooms.map(room => room.x + room.width)); const maxY = Math.max(1, ...rooms.map(room => room.y + room.length));
  return <svg aria-label={`Version ${design.version} floor-plan preview`} viewBox={`0 0 ${maxX} ${maxY}`} className="w-full h-28 mt-3 rounded-lg bg-slate-50 border">{rooms.map((room, index) => <g key={`${room.roomType}-${index}`}><rect x={room.x} y={room.y} width={room.width} height={room.length} fill={index % 2 ? '#e0e7ff' : '#eef2ff'} stroke="#64748b" strokeWidth=".18"/><text x={room.x + room.width / 2} y={room.y + room.length / 2} fontSize="1.3" textAnchor="middle" fill="#334155">{formatRoomName(room.roomType)}</text></g>)}</svg>;
};

const CompareModal = ({ comparison, onClose, onSelect }: { comparison: NonNullable<Comparison>; onClose: () => void; onSelect: (id: string) => void }) => <div role="dialog" aria-label="Compare designs" className="fixed inset-0 z-50 bg-black/50 p-4 overflow-auto"><div className="max-w-6xl mx-auto bg-white dark:bg-gray-900 rounded-3xl p-6"><div className="flex justify-between mb-5"><h2 className="text-2xl font-bold">Compare Designs</h2><button aria-label="Close comparison" onClick={onClose}><X/></button></div><div className="grid md:grid-cols-2 gap-5">{comparison.designs.map(design => <article key={design.designId} className="border rounded-2xl p-5"><h3 className="text-xl font-bold">Version {design.version}</h3><MiniPlan design={design}/><dl className="grid grid-cols-2 gap-3 mt-4 text-sm">{[['Layout',formatTopology(design.topology)],['Bedrooms',design.bedrooms],['Bathrooms',design.bathrooms],['Floors',design.floorCount],['Area',formatArea(design.totalBuiltUpAreaSqft)],['Created as',formatGenerationMode(design.generationMode)],['Suitability score',design.suitabilityScore ?? 'N/A'],['Architectural quality',design.architecturalQualityScore ?? 'N/A'],['Plan reference',design.selectedBasePlan || 'N/A'],['Created',new Date(design.createdAt).toLocaleString()]].map(([label,value]) => <div key={String(label)}><dt className="text-zinc-400">{label}</dt><dd className="font-semibold">{value}</dd></div>)}</dl><button onClick={() => onSelect(design.designId)} className="w-full mt-5 py-2.5 rounded-xl bg-indigo-600 text-white font-bold">Select Version {design.version}</button></article>)}</div></div></div>;

const RemovalModal = ({ pending, onCancel, onConfirm }: { pending: PendingRemoval; onCancel: () => void; onConfirm: () => void }) => {
  const archive = pending.workflow.status === 'awaiting_architect_review';
  return <div role="dialog" aria-label={`Delete Version ${pending.design.version}`} className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"><div className="max-w-md bg-white dark:bg-gray-900 rounded-2xl p-6"><h2 className="text-xl font-bold">{archive ? 'Archive' : 'Delete'} Version {pending.design.version}?</h2><p className="text-zinc-600 dark:text-zinc-300 mt-3">This version will no longer be available for selection or approval. Approved or submitted designs are retained and archived instead of being hard-deleted.</p>{pending.design.isPreferred && <p className="mt-3 text-amber-700 bg-amber-50 p-3 rounded-lg">This is your selected design. Removing it will clear the project selection.</p>}<div className="flex justify-end gap-3 mt-6"><button onClick={onCancel} className="px-4 py-2 rounded-xl border">Cancel</button><button onClick={onConfirm} className="px-4 py-2 rounded-xl bg-red-600 text-white font-bold">{archive ? 'Archive' : 'Delete'}</button></div></div></div>;
};

export default MyDesignsPage;
