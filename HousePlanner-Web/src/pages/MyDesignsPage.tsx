import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { CheckCircle2, GitCompareArrows, RefreshCw } from 'lucide-react';
import { workflowService, type DesignHistoryDto, type WorkflowDesignHistoryDto } from '../services/workflowService';

const MyDesignsPage: React.FC = () => {
  const [projects, setProjects] = useState<WorkflowDesignHistoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [compare, setCompare] = useState<Record<string, string[]>>({});

  const load = async () => {
    setLoading(true);
    try { setProjects(await workflowService.getMyDesigns()); setError(''); }
    catch (err: any) { setError(err.message || 'Could not load saved designs.'); }
    finally { setLoading(false); }
  };
  useEffect(() => { void load(); }, []);

  const select = async (workflowId: string, designId: string) => {
    await workflowService.selectDesign(workflowId, designId);
    await load();
  };
  const submit = async (workflowId: string) => {
    await workflowService.submitArchitectReview(workflowId);
    await load();
  };
  const toggleCompare = (workflowId: string, designId: string) => setCompare(previous => {
    const selected = previous[workflowId] || [];
    return { ...previous, [workflowId]: selected.includes(designId)
      ? selected.filter(id => id !== designId) : [...selected.slice(-1), designId] };
  });

  if (loading) return <div className="p-10 text-center">Loading saved designs…</div>;
  return <div className="max-w-7xl mx-auto p-5 sm:p-8">
    <div className="flex items-center justify-between mb-7"><div><h1 className="text-3xl font-extrabold">My Designs</h1><p className="text-zinc-500 mt-1">Every generated and revised version is saved here.</p></div><button onClick={load} aria-label="Refresh designs" className="p-3 rounded-xl border"><RefreshCw size={18}/></button></div>
    {error && <div role="alert" className="p-4 bg-red-50 text-red-700 rounded-xl">{error}</div>}
    {!projects.length && !error && <div className="rounded-2xl border border-dashed p-10 text-center"><p className="font-semibold">No designs yet</p><Link to="/dashboard/new-project" className="text-indigo-600">Create your first project</Link></div>}
    <div className="space-y-8">{projects.map(project => {
      const compared = project.designs.filter(d => compare[project.workflowId]?.includes(d.designId));
      return <section key={project.workflowId} className="rounded-3xl border bg-zinc-50/60 dark:bg-gray-900/50 p-5">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-5"><div><h2 className="font-bold">Project {project.workflowId.slice(0, 8)}</h2><p className="text-sm text-zinc-500">{project.designs.length} saved version{project.designs.length === 1 ? '' : 's'} · {project.status.replaceAll('_', ' ')}</p></div>{project.preferredHouseDesignId && project.status !== 'awaiting_architect_review' && <button onClick={() => submit(project.workflowId)} className="px-5 py-2.5 rounded-xl bg-emerald-600 text-white font-bold">Submit to Architect</button>}</div>
        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">{project.designs.map(design => <DesignCard key={design.designId} design={design} workflowId={project.workflowId} compared={compare[project.workflowId]?.includes(design.designId) || false} onCompare={() => toggleCompare(project.workflowId, design.designId)} onSelect={() => select(project.workflowId, design.designId)}/>)}</div>
        {compared.length === 2 && <div className="mt-5 rounded-2xl bg-white dark:bg-gray-950 border p-4"><h3 className="font-bold flex items-center gap-2 mb-3"><GitCompareArrows size={18}/>Version comparison</h3><div className="grid grid-cols-2 gap-4">{compared.map(d => <div key={d.designId}><b>Version {d.version}</b><p className="text-sm text-zinc-500">{d.topology?.replaceAll('_',' ')} · {d.bedrooms} bed · {d.bathrooms} bath · {d.totalBuiltUpAreaSqft} sq ft</p></div>)}</div></div>}
      </section>;
    })}</div>
  </div>;
};

const DesignCard = ({ design, workflowId, compared, onCompare, onSelect }: { design: DesignHistoryDto; workflowId: string; compared: boolean; onCompare: () => void; onSelect: () => void }) =>
  <article className={`rounded-2xl bg-white dark:bg-gray-950 border p-5 ${design.isPreferred ? 'border-emerald-500 ring-1 ring-emerald-500' : 'border-zinc-200 dark:border-gray-700'}`}>
    <div className="flex justify-between"><div><span className="text-xs uppercase text-zinc-400 font-bold">Version {design.version}</span><h3 className="font-bold mt-1">{design.topology?.replaceAll('_', ' ') || 'House design'}</h3></div>{design.isPreferred && <span className="text-emerald-600 flex gap-1 text-xs font-bold"><CheckCircle2 size={16}/>Selected</span>}</div>
    <dl className="grid grid-cols-2 gap-2 text-sm my-4"><div><dt className="text-zinc-400">Rooms</dt><dd>{design.bedrooms} bed · {design.bathrooms} bath</dd></div><div><dt className="text-zinc-400">Floors</dt><dd>{design.floorCount}</dd></div><div><dt className="text-zinc-400">Area</dt><dd>{design.totalBuiltUpAreaSqft} sq ft</dd></div><div><dt className="text-zinc-400">Mode</dt><dd>{design.generationMode?.replaceAll('_',' ') || 'Generated'}</dd></div></dl>
    <p className="text-xs text-zinc-400 mb-4">Created {new Date(design.createdAt).toLocaleString()}</p>
    <div className="flex flex-wrap gap-2"><Link to={`/dashboard/workflows/${workflowId}?design=${design.designId}`} className="px-3 py-2 rounded-lg border text-sm font-semibold">Preview</Link><button onClick={onCompare} className={`px-3 py-2 rounded-lg border text-sm font-semibold ${compared ? 'bg-indigo-50 text-indigo-700' : ''}`}>Compare</button><button onClick={onSelect} disabled={design.isPreferred} className="px-3 py-2 rounded-lg bg-indigo-600 text-white text-sm font-semibold disabled:opacity-50">Select Design</button></div>
  </article>;

export default MyDesignsPage;
