import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Building2, ChevronDown, ChevronUp, CheckCircle2, RefreshCw } from 'lucide-react';
import { customerConstructionService, type ApprovedDesign, type ConstructorProfile, type CustomerConstruction } from '../services/customerConstructionService';
import CostBreakdownCard from '../components/cost/CostBreakdownCard';

export default function CustomerConstructionPage() {
  const [params] = useSearchParams();
  const [data, setData] = useState<CustomerConstruction | null>(null);
  const [designs, setDesigns] = useState<ApprovedDesign[]>([]);
  const [constructors, setConstructors] = useState<ConstructorProfile[]>([]);
  const [selectedDesign, setSelectedDesign] = useState(params.get('design') || '');
  const [setupOpen, setSetupOpen] = useState(false);
  const [setupStep, setSetupStep] = useState(1);
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCostBreakdown, setShowCostBreakdown] = useState(false);
  const [requestingConstructor, setRequestingConstructor] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [overview, approved] = await Promise.all([
        customerConstructionService.overview(), customerConstructionService.approvedDesigns(),
      ]);
      setData(overview);
      setDesigns(approved);
      setSelectedDesign(current => current || approved[0]?.designId || '');
      customerConstructionService.constructors().then(setConstructors)
        .catch(e => console.error('Constructor load failed:', e));
    } catch (e: any) {
      setError(e.response?.data?.message || 'Construction information could not be loaded.');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const request = async (constructorId: string) => {
    if (!selectedDesign || requestingConstructor) return;
    setRequestingConstructor(constructorId);
    try {
      await customerConstructionService.request(selectedDesign, constructorId);
      setMessage('Construction request sent successfully.');
      setSetupOpen(false);
      setSetupStep(1);
      await load();
    } catch (e: any) {
      setMessage(e.response?.data?.message || 'Could not send construction request.');
    } finally { setRequestingConstructor(null); }
  };

  const cancelProject = async (projectId: string, hasProgress: boolean) => {
    const prompt = hasProgress
      ? 'This project already contains construction progress. Cancelling will stop further workflow updates but preserve all recorded history.\n\nCancel this construction project?'
      : 'Cancel this construction project?\n\nThis will remove it from Active Projects. Existing construction history, phases, and daily logs will be preserved.';
    if (!window.confirm(prompt)) return;
    try {
      await customerConstructionService.cancelProject(projectId);
      setMessage('Construction project cancelled.');
      await load();
    } catch (e: any) {
      setMessage(e.response?.data?.message || e.message || 'Could not cancel project.');
    }
  };

  if (loading) return <main className="mx-auto max-w-5xl p-4 py-20 text-center text-text-muted sm:p-8">Loading construction information...</main>;
  if (error) return <main className="mx-auto max-w-5xl p-4 py-20 text-center sm:p-8"><p className="mb-4 text-red-500">{error}</p><button type="button" onClick={() => void load()} className="rounded-xl border px-4 py-2">Try Again</button></main>;

  const design = designs.find(item => item.designId === selectedDesign);
  const pendingRequests = data?.pendingRequests || [];
  const activeProjects = data?.activeProjects || [];
  const declinedRequests = data?.declinedRequests || [];
  const completedProjects = data?.completedProjects || [];
  const designTitle = (id: string, version: number) => designs.find(item => item.designId === id)?.title || `Approved Design v${version}`;
  const designSummary = (item: ApprovedDesign) =>
    `${item.bedrooms} ${item.bedrooms === 1 ? 'Bedroom' : 'Bedrooms'} • ${item.bathrooms} ${item.bathrooms === 1 ? 'Bathroom' : 'Bathrooms'} • ${item.floorCount} ${item.floorCount === 1 ? 'Floor' : 'Floors'}`;

  return <main className="mx-auto max-w-5xl space-y-7 overflow-x-hidden p-4 sm:p-8">
    <header className="flex items-start justify-between gap-4 border-b pb-5 dark:border-gray-800">
      <div className="min-w-0"><h1 className="text-2xl font-bold sm:text-3xl">Construction</h1><p className="mt-1 text-sm text-text-muted">Track projects and connect an approved design with a constructor.</p></div>
      <button type="button" aria-label="Refresh construction" onClick={() => void load()} className="shrink-0 rounded-xl border p-2.5"><RefreshCw size={18}/></button>
    </header>
    {message && <div role="status" className="flex items-center gap-2 rounded-xl border border-emerald-100 bg-emerald-50 p-3 text-sm font-medium text-emerald-800"><CheckCircle2 size={18}/>{message}</div>}

    <section aria-labelledby="active-projects-heading" className="space-y-3">
      <h2 id="active-projects-heading" className="flex items-center gap-2 text-xl font-bold"><Building2 size={20} className="text-indigo-600"/>Active Projects</h2>
      {activeProjects.length ? <div className="grid grid-cols-1 gap-4 md:grid-cols-2">{activeProjects.map(project =>
        <article key={project.id} className="min-w-0 rounded-2xl border bg-surface p-4 shadow-sm">
          <div className="flex items-start justify-between gap-3"><div className="min-w-0"><h3 className="truncate font-bold">{designTitle(project.houseDesignId, project.designVersion)}</h3><p className="truncate text-sm text-text-muted">Constructor: {project.constructorName}</p></div><span className="shrink-0 rounded-full border border-indigo-100 bg-indigo-50 px-2.5 py-1 text-xs font-semibold text-indigo-700">Status: In Progress</span></div>
          <p className="mt-3 text-sm"><span className="text-text-muted">Current phase:</span> <strong>{project.currentPhase || 'Awaiting update'}</strong></p>
          <div className="mt-4 flex items-center gap-2"><Link to={`/dashboard/construction/${project.id}`} className="flex-1 rounded-lg bg-indigo-600 px-4 py-2 text-center text-sm font-semibold text-white">View Progress</Link><button type="button" onClick={() => void cancelProject(project.id, project.status === 'in_progress')} className="rounded-lg px-3 py-2 text-sm font-semibold text-red-600">Cancel</button></div>
        </article>)}</div>
      : <div className="rounded-xl border border-dashed p-5 text-sm text-text-muted"><strong className="block text-gray-800 dark:text-gray-200">No active projects</strong>Your accepted construction projects will appear here.</div>}
      {completedProjects.length > 0 && <div className="pt-2"><h3 className="mb-2 text-sm font-semibold text-text-muted">Completed projects</h3><div className="flex flex-wrap gap-2">{completedProjects.map(project => <Link key={project.id} to={`/dashboard/construction/${project.id}`} className="rounded-lg border bg-surface px-3 py-2 text-sm">{designTitle(project.houseDesignId, project.designVersion)} · {project.constructorName}</Link>)}</div></div>}
    </section>

    <section aria-labelledby="pending-requests-heading" className="space-y-3">
      <h2 id="pending-requests-heading" className="text-xl font-bold">Pending Requests</h2>
      {pendingRequests.length ? <div className="divide-y overflow-hidden rounded-xl border bg-surface">{pendingRequests.map(item =>
        <div key={item.id} className="grid min-w-0 grid-cols-1 gap-1 px-4 py-3 text-sm sm:grid-cols-[1fr_1fr_auto_auto] sm:items-center sm:gap-4"><span className="truncate font-semibold">{item.constructorName || 'Waiting for constructor'}</span><span className="truncate text-text-muted">{designTitle(item.houseDesignId, item.designVersion)}</span><span className="text-text-muted">{new Date(item.requestedAt).toLocaleDateString()}</span><span className="font-medium text-amber-700">Status: {item.status || 'Pending'}</span></div>)}</div>
      : <div className="rounded-xl border border-dashed p-5 text-sm text-text-muted"><strong className="block text-gray-800 dark:text-gray-200">No pending requests</strong>You have no requests waiting for constructor response.</div>}
      {declinedRequests.map(item => <div key={item.id} className="rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-800"><strong>{item.constructorName} declined your request.</strong>{item.declineReason && <span> {item.declineReason}</span>} You may choose another constructor.</div>)}
    </section>

    <section aria-labelledby="start-construction-heading" className="rounded-2xl border bg-surface shadow-sm">
      <div className="flex items-center justify-between gap-4 p-4 sm:p-5"><div><h2 id="start-construction-heading" className="text-xl font-bold">Start New Construction</h2><p className="text-sm text-text-muted">Use an architect-approved design to request a constructor.</p></div><button type="button" aria-expanded={setupOpen} onClick={() => setSetupOpen(open => !open)} className="shrink-0 rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white">{setupOpen ? 'Close' : 'Start New Construction'}</button></div>
      {setupOpen && <div className="border-t p-4 sm:p-5">{!designs.length
        ? <div className="rounded-xl border border-dashed p-5 text-sm text-text-muted"><strong className="block text-gray-800 dark:text-gray-200">No approved designs</strong>You need an approved design before starting construction.</div>
        : <><ol className="mb-5 flex flex-wrap gap-2 text-xs font-semibold" aria-label="Construction request steps">{[1, 2, 3].map(step => <li key={step} className={`rounded-full px-3 py-1.5 ${setupStep === step ? 'bg-indigo-100 text-indigo-700' : 'bg-gray-100 text-text-muted'}`}>Step {step}: {step === 1 ? 'Select Design' : step === 2 ? 'Review Estimate' : 'Choose Constructor'}</li>)}</ol>
          {setupStep === 1 && <div className="space-y-4"><h3 className="font-semibold">Step 1 — Select Design</h3>{design
            ? <div className="flex flex-col gap-3 rounded-xl border p-4 sm:flex-row sm:items-center sm:justify-between"><div><strong>{design.title}</strong><p className="text-sm text-text-muted">{designSummary(design)}</p></div><button type="button" onClick={() => setSelectedDesign('')} className="text-left text-sm font-semibold text-indigo-600">Change Design</button></div>
            : <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">{designs.map(item => <button type="button" key={item.designId} onClick={() => setSelectedDesign(item.designId)} className="min-w-0 rounded-xl border p-3 text-left"><strong className="block truncate">{item.title}</strong><span className="text-sm text-text-muted">{designSummary(item)}</span></button>)}</div>}
            <div className="flex justify-end"><button type="button" disabled={!design} onClick={() => setSetupStep(2)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">Continue to Estimate</button></div></div>}
          {setupStep === 2 && design && <div className="space-y-4"><h3 className="font-semibold">Step 2 — Review Estimate</h3><div className="rounded-xl border p-4"><p className="text-sm text-text-muted">Estimated Cost</p><p className="text-2xl font-bold">{design.cost ? `LKR ${design.cost.totalCostLkr.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : 'Not available yet'}</p>{design.cost && <button type="button" aria-expanded={showCostBreakdown} onClick={() => setShowCostBreakdown(show => !show)} className="mt-2 inline-flex items-center gap-1 text-sm font-semibold text-indigo-600">{showCostBreakdown ? 'Hide breakdown' : 'View breakdown'}{showCostBreakdown ? <ChevronUp size={16}/> : <ChevronDown size={16}/>}</button>}{design.cost && showCostBreakdown && <div className="mt-4 border-t pt-4"><CostBreakdownCard cost={design.cost}/></div>}</div><div className="flex justify-between"><button type="button" onClick={() => setSetupStep(1)} className="rounded-lg border px-4 py-2 text-sm font-semibold">Back</button><button type="button" onClick={() => setSetupStep(3)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white">Choose Constructor</button></div></div>}
          {setupStep === 3 && design && <div className="space-y-4"><h3 className="font-semibold">Step 3 — Choose Constructor</h3>{!constructors.length ? <p className="rounded-xl border border-dashed p-4 text-sm text-text-muted">No registered constructors are currently available.</p> : <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">{constructors.map(constructor => { const isPending = pendingRequests.some(item => item.houseDesignId === selectedDesign && item.constructorName === constructor.name); const isRequesting = requestingConstructor === constructor.id; return <article key={constructor.id} className="min-w-0 rounded-xl border p-4"><h4 className="truncate font-bold">{constructor.name}</h4><p className="mb-3 text-xs text-text-muted">Registered constructor</p><button type="button" disabled={requestingConstructor !== null || isPending} onClick={() => void request(constructor.id)} className="w-full rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white disabled:bg-gray-100 disabled:text-text-muted">{isPending ? 'Request pending' : isRequesting ? 'Sending...' : 'Send Request'}</button></article>; })}</div>}<button type="button" onClick={() => setSetupStep(2)} className="rounded-lg border px-4 py-2 text-sm font-semibold">Back</button></div>}
        </>}
      </div>}
    </section>
  </main>;
}
