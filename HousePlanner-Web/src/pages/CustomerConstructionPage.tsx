import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Building2, Clock, RefreshCw } from 'lucide-react';
import { customerConstructionService, type ApprovedDesign, type ConstructorProfile, type CustomerConstruction } from '../services/customerConstructionService';
import CostBreakdownCard from '../components/cost/CostBreakdownCard';

export default function CustomerConstructionPage() {
  const [params] = useSearchParams();
  const [data, setData] = useState<CustomerConstruction | null>(null);
  const [designs, setDesigns] = useState<ApprovedDesign[]>([]);
  const [constructors, setConstructors] = useState<ConstructorProfile[]>([]);
  const [selectedDesign, setSelectedDesign] = useState(params.get('design') || '');
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [overview, approved] = await Promise.all([
        customerConstructionService.overview(),
        customerConstructionService.approvedDesigns()
      ]);
      setData(overview); setDesigns(approved);
      if (!selectedDesign && approved.length) setSelectedDesign(approved[0].designId);
      
      customerConstructionService.constructors().then(setConstructors).catch(e => console.error('Constructor load failed:', e));
    } catch (e: any) {
      setError(e.response?.data?.message || 'Construction information could not be loaded.');
    } finally {
      setLoading(false);
    }
  }, [selectedDesign]);
  useEffect(() => { void load(); }, [load]);
  const request = async (constructorId: string) => {
    if (!selectedDesign) return;
    try { await customerConstructionService.request(selectedDesign, constructorId); setMessage('Design approved for construction and request sent.'); await load(); }
    catch (e: any) { setMessage(e.response?.data?.message || 'Could not send construction request.'); }
  };
  if (loading) return <main className="mx-auto max-w-7xl p-4 sm:p-8 space-y-8 text-center text-zinc-500 py-20">Loading construction information...</main>;
  if (error) return <main className="mx-auto max-w-7xl p-4 sm:p-8 space-y-8 text-center py-20"><p className="text-red-500 mb-4">{error}</p><button onClick={() => void load()} className="rounded-xl border px-4 py-2 hover:bg-zinc-50">Try Again</button></main>;

  const design = designs.find(d => d.designId === selectedDesign);
  return <main className="mx-auto max-w-7xl p-4 sm:p-8 space-y-8">
    <header className="flex items-center justify-between"><div><h1 className="text-3xl font-bold">Construction</h1><p className="text-zinc-500">Choose a constructor and follow approved projects.</p></div><button aria-label="Refresh construction" onClick={() => void load()} className="rounded-xl border p-3"><RefreshCw size={18}/></button></header>
    {message && <div role="status" className="rounded-xl bg-indigo-50 p-3 text-indigo-800">{message}</div>}
    <section><h2 className="text-xl font-bold mb-3">Approved Designs</h2>{!designs.length ? <p className="rounded-xl border border-dashed p-6 text-zinc-500">No designs have been approved yet. Once an architect approves a design, it will appear here for you to request construction.</p> : <div className="grid md:grid-cols-2 gap-3">{designs.map(d => <button key={d.designId} onClick={() => setSelectedDesign(d.designId)} className={`text-left rounded-2xl border p-4 ${selectedDesign===d.designId?'border-emerald-500 ring-1 ring-emerald-500':'bg-white dark:bg-gray-950'}`}><b>✓ {d.title}</b><p className="text-sm text-zinc-500 mt-1">{d.bedrooms} Bedrooms · {d.bathrooms} Bathrooms · {d.floorCount} Floors</p><span className="text-xs text-emerald-700">Architect Approved</span></button>)}</div>}</section>
    {design && <section className="space-y-6"><div><h2 className="text-xl font-bold">Review Approved Design</h2><p className="text-sm text-zinc-500">Confirm the architect-approved design and estimate before choosing a constructor.</p></div><CostBreakdownCard cost={design.cost}/><div><h2 className="text-xl font-bold">Choose a Constructor</h2><p className="text-sm text-zinc-500 mb-4">Building: {design.title}</p>{!constructors.length ? <p>No registered constructors are currently available.</p> : <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">{constructors.map(c => <article key={c.id} className="rounded-2xl border bg-white dark:bg-gray-950 p-5"><Building2 className="text-indigo-600"/><h3 className="font-bold mt-3">{c.name}</h3><button onClick={() => void request(c.id)} className="mt-4 w-full rounded-xl bg-indigo-600 py-2.5 text-white font-semibold">Approve &amp; Request Construction</button></article>)}</div>}</div></section>}
    <section><h2 className="text-xl font-bold mb-3">Pending Requests</h2>{!data?.pendingRequests.length?<p className="text-zinc-500">No pending requests.</p>:data.pendingRequests.map(r=><div key={r.id} className="rounded-xl border p-4 mb-2"><Clock className="inline mr-2" size={16}/>Pending with <strong>{r.constructorName}</strong><p className="text-sm text-zinc-500 mt-1 ml-6">Design v{r.designVersion} · Requested {new Date(r.requestedAt).toLocaleDateString()}</p></div>)}</section>
    {!!data?.declinedRequests.length && <section><h2 className="text-xl font-bold mb-3">Declined Requests</h2>{data.declinedRequests.map(r=><div key={r.id} className="rounded-xl bg-red-50 text-red-800 p-4 mb-2"><b>{r.constructorName} declined this request.</b>{r.declineReason&&<p>{r.declineReason}</p>}<p className="text-sm">You may choose another constructor above.</p></div>)}</section>}
    <section><h2 className="text-xl font-bold mb-3">Active Construction</h2>{!data?.activeProjects.length?<p className="text-zinc-500">No active construction projects.</p>:<div className="grid md:grid-cols-2 gap-3">{data.activeProjects.map(p=><article key={p.id} className="rounded-2xl border p-5"><b>Design v{p.designVersion}</b><p>{p.constructorName}</p><p className="text-sm text-zinc-500">Current phase: {p.currentPhase||'Awaiting update'}</p><Link to={`/dashboard/construction/${p.id}`} className="inline-block mt-4 text-indigo-600 font-semibold">View Progress</Link></article>)}</div>}</section>
    {!!data?.completedProjects.length && <section><h2 className="text-xl font-bold mb-3">Completed Projects</h2>{data.completedProjects.map(p=><Link key={p.id} to={`/dashboard/construction/${p.id}`} className="block rounded-xl border p-4">Design v{p.designVersion} · {p.constructorName}</Link>)}</section>}
  </main>;
}

