import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Building2, Clock, RefreshCw, CheckCircle2, AlertCircle, ChevronDown, ChevronUp } from 'lucide-react';
import { customerConstructionService, type ApprovedDesign, type ConstructorProfile, type CustomerConstruction } from '../services/customerConstructionService';
import CostBreakdownCard from '../components/cost/CostBreakdownCard';
import { formatDesignRef, formatProjectRef, formatRequestRef } from '../utils/referenceCode';

export default function CustomerConstructionPage() {
  const [params] = useSearchParams();
  const [data, setData] = useState<CustomerConstruction | null>(null);
  const [designs, setDesigns] = useState<ApprovedDesign[]>([]);
  const [constructors, setConstructors] = useState<ConstructorProfile[]>([]);
  const [selectedDesign, setSelectedDesign] = useState(params.get('design') || '');
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
        customerConstructionService.overview(),
        customerConstructionService.approvedDesigns()
      ]);
      setData(overview);
      setDesigns(approved);
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
    if (!selectedDesign || requestingConstructor) return;
    setRequestingConstructor(constructorId);
    try {
      await customerConstructionService.request(selectedDesign, constructorId);
      setMessage('Construction request sent successfully.');
      await load();
    } catch (e: any) {
      setMessage(e.response?.data?.message || 'Could not send construction request.');
    } finally {
      setRequestingConstructor(null);
    }
  };

  const cancelProject = async (projectId: string, hasProgress: boolean) => {
    const msg = hasProgress
      ? "This project already contains construction progress. Cancelling will stop further workflow updates but preserve all recorded history.\n\nCancel this construction project?"
      : "Cancel this construction project?\n\nThis will remove it from Active Construction. Existing construction history, phases, and daily logs will be preserved.";
    if (!window.confirm(msg)) return;
    try {
      await customerConstructionService.cancelProject(projectId);
      setMessage('Construction project cancelled.');
      await load();
    } catch (e: any) {
      setMessage(e.response?.data?.message || e.message || 'Could not cancel project.');
    }
  };

  if (loading) return <main className="mx-auto max-w-7xl p-4 sm:p-8 space-y-8 text-center text-text-muted py-4 md:py-20">Loading construction information...</main>;
  if (error) return <main className="mx-auto max-w-7xl p-4 sm:p-8 space-y-8 text-center py-4 md:py-20"><p className="text-red-500 mb-4">{error}</p><button onClick={() => void load()} className="rounded-xl border px-4 py-2 hover:bg-surface-elevated">Try Again</button></main>;

  const design = designs.find(d => d.designId === selectedDesign);
  const pendingRequests = data?.pendingRequests || [];
  const activeProjects = data?.activeProjects || [];
  const declinedRequests = data?.declinedRequests || [];
  const completedProjects = data?.completedProjects || [];

  return (
    <main className="mx-auto max-w-5xl p-4 sm:p-8 space-y-8">
      <header className="flex items-center justify-between border-b pb-6 dark:border-gray-800">
        <div>
          <h1 className="text-2xl md:text-3xl font-bold mb-2">Construction</h1>
          <p className="text-text-muted text-sm sm:text-base">Choose a constructor for an approved house design and track your construction requests.</p>
        </div>
        <button aria-label="Refresh construction" onClick={() => void load()} className="rounded-xl border p-3 hover:bg-surface-elevated transition-colors shrink-0 ml-4">
          <RefreshCw size={18}/>
        </button>
      </header>

      {message && <div role="status" className="rounded-xl bg-emerald-50 border border-emerald-100 p-4 text-emerald-800 font-medium flex items-center gap-2"><CheckCircle2 size={18} /> {message}</div>}

      {/* TOP PENDING REQUEST ALERT */}
      {pendingRequests.length === 1 && (
        <div className="rounded-2xl border border-amber-200 bg-amber-50 p-5 flex items-start gap-4">
          <Clock className="text-amber-600 mt-0.5" size={20} />
          <div>
            <h3 className="font-bold text-amber-900 text-lg">Construction request pending</h3>
            <p className="text-amber-800 mt-1">Your request with <strong>{pendingRequests[0].constructorName}</strong> is waiting for a response. <span className="text-sm ml-1 opacity-80">({formatRequestRef(pendingRequests[0].id)})</span></p>
            <p className="text-sm text-amber-700/80 mt-2">Requested on {new Date(pendingRequests[0].requestedAt).toLocaleDateString()} · {formatDesignRef(pendingRequests[0].houseDesignId)}</p>
          </div>
        </div>
      )}

      {/* DECLINED REQUESTS */}
      {declinedRequests.length > 0 && (
        <section className="space-y-3">
          {declinedRequests.map(r => (
            <div key={r.id} className="rounded-xl bg-red-50 border border-red-100 text-red-800 p-4 flex items-start gap-3">
              <AlertCircle size={20} className="mt-0.5 shrink-0" />
              <div>
                <b className="block mb-1">{r.constructorName} declined your request.</b>
                {r.declineReason && <p className="text-sm mb-2">{r.declineReason}</p>}
                <p className="text-xs opacity-80">You may choose another constructor below.</p>
              </div>
            </div>
          ))}
        </section>
      )}

      {/* ACTIVE CONSTRUCTION */}
      {activeProjects.length > 0 && (
        <section>
          <h2 className="text-xl font-bold mb-4 flex items-center gap-2"><Building2 size={20} className="text-indigo-600" /> Active Construction</h2>
          <div className="grid md:grid-cols-2 gap-4">
            {activeProjects.map(p => {
              const projDesign = designs.find(d => d.designId === p.houseDesignId) || { title: `Design v${p.designVersion}` };
              return (
                <article key={p.id} className="rounded-2xl border bg-surface p-5 shadow-sm">
                  <div className="flex justify-between items-start mb-4">
                    <div>
                      <h3 className="font-bold text-lg">{projDesign.title} <span className="text-sm font-normal text-text-muted ml-1">({formatDesignRef(p.houseDesignId)})</span></h3>
                      <p className="text-text-muted">{p.constructorName} <span className="text-sm ml-1">({formatProjectRef(p.id)})</span></p>
                    </div>
                    <span className="bg-indigo-50 text-indigo-700 text-xs font-semibold px-2.5 py-1 rounded-full border border-indigo-100">
                      In Progress
                    </span>
                  </div>

                  <div className="bg-gray-50 dark:bg-gray-800/50 rounded-xl p-4 mb-5 border border-gray-100 dark:border-gray-800">
                    <p className="text-xs text-text-muted mb-1 uppercase tracking-wider font-semibold">Current Phase</p>
                    <p className="font-medium">{p.currentPhase || 'Awaiting update'}</p>
                  </div>

                  <div className="flex items-center gap-3">
                    <Link to={`/dashboard/construction/${p.id}`} className="flex-1 text-center bg-indigo-600 text-white rounded-xl py-2.5 font-semibold hover:bg-indigo-700 transition-colors">
                      View Progress
                    </Link>
                    <button onClick={() => void cancelProject(p.id, p.status === 'in_progress')} className="px-4 py-2.5 text-sm font-semibold text-red-600 hover:bg-red-50 rounded-xl transition-colors">
                      Cancel
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        </section>
      )}

      {/* MULTIPLE PENDING REQUESTS */}
      {pendingRequests.length > 1 && (
        <section>
          <h2 className="text-xl font-bold mb-4">Pending Requests</h2>
          <div className="space-y-3">
            {pendingRequests.map(r => {
              const reqDesign = designs.find(d => d.designId === r.houseDesignId) || { title: `Design v${r.designVersion}` };
              return (
                <div key={r.id} className="rounded-xl border p-4 flex items-center justify-between bg-surface shadow-sm">
                  <div className="flex items-center gap-3">
                    <div className="bg-amber-100 p-2 rounded-lg text-amber-700"><Clock size={18} /></div>
                    <div>
                      <p className="font-medium">Waiting for {r.constructorName} <span className="text-xs text-text-muted font-normal ml-1">({formatRequestRef(r.id)})</span></p>
                      <p className="text-sm text-text-muted">{reqDesign.title} <span className="ml-1">({formatDesignRef(r.houseDesignId)})</span> · Requested {new Date(r.requestedAt).toLocaleDateString()}</p>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      )}

      {/* APPROVED DESIGNS & CONSTRUCTOR SELECTION */}
      <section className="bg-surface border rounded-3xl p-4 md:p-6 sm:p-8 shadow-sm">
        <h2 className="text-2xl font-bold mb-6">Start Construction</h2>

        {!designs.length ? (
          <div className="text-center py-4 md:py-10 bg-gray-50 dark:bg-gray-900/50 rounded-2xl border border-dashed border-gray-300 dark:border-gray-700">
            <p className="text-text-muted">No approved designs are available yet.</p>
            <p className="text-sm text-text-muted mt-2">Once an architect approves your design, it will appear here.</p>
          </div>
        ) : (
          <div className="space-y-8">

            {/* DESIGN SELECTION */}
            {selectedDesign && design ? (
              <div className="rounded-2xl border bg-gray-50 dark:bg-gray-900/50 p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold text-text-muted uppercase tracking-wider mb-1">Selected Design</p>
                  <h3 className="text-lg font-bold">{design.title} <span className="text-sm font-normal text-text-muted ml-1">({formatDesignRef(design.designId)})</span></h3>
                  <p className="text-sm text-text-muted mt-1">{design.bedrooms} Bedrooms · {design.bathrooms} Bathrooms · {design.floorCount} Floors</p>
                </div>
                <button onClick={() => setSelectedDesign('')} className="shrink-0 text-sm font-semibold text-indigo-600 hover:bg-indigo-50 px-4 py-2 rounded-xl transition-colors border border-indigo-100 bg-white dark:bg-gray-800 dark:border-gray-700 dark:hover:bg-gray-800/80">
                  Change design
                </button>
              </div>
            ) : (
              <div>
                <p className="font-semibold mb-3">1. Select an approved design</p>
                <div className="grid sm:grid-cols-2 gap-4">
                  {designs.map(d => (
                    <button key={d.designId} onClick={() => setSelectedDesign(d.designId)} className="text-left group rounded-2xl border p-4 bg-surface hover:border-indigo-300 transition-colors">
                      <div className="flex justify-between items-start mb-2">
                        <b className="font-bold text-lg group-hover:text-indigo-600 transition-colors">{d.title}</b>
                        <span className="text-xs font-medium bg-emerald-50 text-emerald-700 px-2 py-1 rounded-md border border-emerald-100">Approved</span>
                      </div>
                      <p className="text-sm text-text-muted mb-4">{d.bedrooms} Bedrooms · {d.bathrooms} Bathrooms · {d.floorCount} Floors</p>
                      <div className="text-sm font-semibold text-indigo-600 group-hover:underline">Select this design &rarr;</div>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* ESTIMATE & CONSTRUCTOR (Only visible if design selected) */}
            {selectedDesign && design && (
              <div className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">

                {/* COST ESTIMATE COMPACT */}
                <div>
                  <p className="font-semibold mb-3">2. Review estimate</p>
                  <div className="rounded-2xl border bg-surface p-5">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm text-text-muted font-medium mb-1">Estimated construction cost</p>
                        {design.cost ? (
                          <div className="text-2xl font-bold">LKR {design.cost.totalCostLkr.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>
                        ) : (
                          <div className="text-lg font-medium">Not available yet</div>
                        )}
                      </div>
                      {design.cost && (
                        <button onClick={() => setShowCostBreakdown(!showCostBreakdown)} className="text-sm font-semibold text-indigo-600 flex items-center gap-1 hover:bg-indigo-50 px-3 py-1.5 rounded-lg transition-colors shrink-0 ml-4">
                          {showCostBreakdown ? 'Hide breakdown' : 'View breakdown'}
                          {showCostBreakdown ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                        </button>
                      )}
                    </div>
                    {!design.cost && (
                      <p className="text-sm text-text-muted mt-2">The estimate will appear when pricing information is available.</p>
                    )}
                    {design.cost && showCostBreakdown && (
                      <div className="mt-6 pt-6 border-t border-dashed">
                        <CostBreakdownCard cost={design.cost} />
                      </div>
                    )}
                  </div>
                </div>

                {/* CONSTRUCTORS */}
                <div>
                  <p className="font-semibold mb-3">3. Choose a constructor</p>
                  {!constructors.length ? (
                    <p className="text-sm text-text-muted p-5 bg-gray-50 rounded-2xl border border-dashed">No registered constructors are currently available.</p>
                  ) : (
                    <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                      {constructors.map(c => {
                        const isPending = pendingRequests.some(r => r.houseDesignId === selectedDesign && r.constructorName === c.name);
                        const isRequesting = requestingConstructor === c.id;

                        return (
                          <article key={c.id} className="rounded-2xl border bg-surface p-5 flex flex-col">
                            <div className="flex items-center gap-3 mb-4">
                              <div className="bg-gray-100 p-2.5 rounded-xl dark:bg-gray-800 shrink-0">
                                <Building2 size={20} className="text-indigo-600" />
                              </div>
                              <h3 className="font-bold leading-tight">{c.name}</h3>
                            </div>

                            <div className="mt-auto pt-2">
                              {isPending ? (
                                <button disabled aria-disabled="true" className="w-full rounded-xl bg-amber-50 text-amber-700 py-2.5 font-semibold text-sm cursor-not-allowed border border-amber-200">
                                  Request pending
                                </button>
                              ) : (
                                <button disabled={requestingConstructor !== null} onClick={() => void request(c.id)} className={`w-full rounded-xl bg-indigo-600 hover:bg-indigo-700 transition-colors text-white py-2.5 font-semibold text-sm shadow-sm ${requestingConstructor ? 'opacity-50 cursor-not-allowed' : ''}`}>
                                  {isRequesting ? 'Sending...' : 'Send request'}
                                </button>
                              )}
                            </div>
                          </article>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        )}
      </section>

      {/* COMPLETED PROJECTS */}
      {completedProjects.length > 0 && (
        <section>
          <h2 className="text-xl font-bold mb-4">Completed Projects</h2>
          <div className="grid md:grid-cols-3 gap-4">
            {completedProjects.map(p => {
               const pDesign = designs.find(d => d.designId === p.houseDesignId) || { title: `Design v${p.designVersion}` };
               return (
                <Link key={p.id} to={`/dashboard/construction/${p.id}`} className="block rounded-2xl border p-5 hover:border-indigo-300 transition-colors bg-surface shadow-sm">
                  <h3 className="font-bold">{pDesign.title}</h3>
                  <p className="text-sm text-text-muted mt-1">{p.constructorName}</p>
                </Link>
               );
            })}
          </div>
        </section>
      )}

    </main>
  );
}
