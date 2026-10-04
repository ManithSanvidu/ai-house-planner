import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Building2, Clock, RefreshCw, CheckCircle2, AlertCircle, ChevronDown, ChevronUp, ChevronRight } from 'lucide-react';
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
  const [isStartOpen, setIsStartOpen] = useState(!!params.get('design'));

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
      customerConstructionService.constructors().then(setConstructors).catch(e => console.error('Constructor load failed:', e));
    } catch (e: any) {
      setError(e.response?.data?.message || 'Construction information could not be loaded.');
    } finally {
      setLoading(false);
    }
  }, []);

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

  if (loading) return <main className="mx-auto max-w-5xl p-4 sm:p-8 space-y-6 text-center text-text-muted py-10">Loading construction information...</main>;
  if (error) return <main className="mx-auto max-w-5xl p-4 sm:p-8 space-y-6 text-center py-10"><p className="text-red-500 mb-4">{error}</p><button onClick={() => void load()} className="rounded-lg border px-4 py-2 hover:bg-surface-elevated text-sm font-semibold">Try Again</button></main>;

  const design = designs.find(d => d.designId === selectedDesign);
  const pendingRequests = data?.pendingRequests || [];
  const activeProjects = data?.activeProjects || [];
  const declinedRequests = data?.declinedRequests || [];
  const completedProjects = data?.completedProjects || [];

  return (
    <main className="mx-auto max-w-5xl p-4 sm:p-6 md:p-8 space-y-6">
      <header className="flex items-center justify-between pb-4 border-b dark:border-gray-800">
        <div>
          <h1 className="text-2xl font-bold mb-1">Construction</h1>
          <p className="text-text-muted text-sm">Choose a constructor for an approved house design and track your construction requests.</p>
        </div>
        <button aria-label="Refresh construction" onClick={() => void load()} className="rounded-lg border p-2 hover:bg-surface-elevated transition-colors shrink-0 ml-4">
          <RefreshCw size={18}/>
        </button>
      </header>

      {message && <div role="status" className="rounded-lg bg-emerald-50 border border-emerald-100 p-3 text-emerald-800 text-sm font-medium flex items-center gap-2"><CheckCircle2 size={16} /> {message}</div>}

      {/* DECLINED REQUESTS */}
      {declinedRequests.length > 0 && (
        <section className="space-y-2">
          {declinedRequests.map(r => (
            <div key={r.id} className="rounded-lg bg-red-50 border border-red-100 text-red-800 p-3 flex items-start gap-2 text-sm">
              <AlertCircle size={16} className="mt-0.5 shrink-0" />
              <div>
                <b className="block">{r.constructorName} declined your request.</b>
                {r.declineReason && <p className="text-xs mt-0.5 opacity-90">{r.declineReason}</p>}
              </div>
            </div>
          ))}
        </section>
      )}

      {/* ACTIVE CONSTRUCTION */}
      <section>
        <h2 className="text-lg font-bold mb-3 flex items-center gap-2"><Building2 size={18} className="text-indigo-600" /> Active Construction</h2>
        {activeProjects.length === 0 ? (
          <p className="text-sm text-text-muted">Your active construction projects will appear here.</p>
        ) : (
          <div className="grid md:grid-cols-2 gap-3">
            {activeProjects.map(p => {
              const projDesign = designs.find(d => d.designId === p.houseDesignId) || { title: `Design v${p.designVersion}` };
              return (
                <article key={p.id} className="rounded-xl border bg-surface p-4 flex flex-col justify-between shadow-sm">
                  <div>
                    <div className="flex justify-between items-start mb-3">
                      <div>
                        <h3 className="font-bold text-base leading-tight">{projDesign.title} <span className="text-xs font-normal text-text-muted ml-1">({formatDesignRef(p.houseDesignId)})</span></h3>
                        <p className="text-sm text-text-muted mt-0.5">{p.constructorName} <span className="text-xs ml-1">({formatProjectRef(p.id)})</span></p>
                      </div>
                      <span className="bg-indigo-50 text-indigo-700 text-[10px] font-semibold px-2 py-0.5 rounded border border-indigo-100 whitespace-nowrap">
                        In Progress
                      </span>
                    </div>
                    <p className="text-sm mb-4"><span className="text-text-muted font-medium">Current phase:</span> {p.currentPhase || 'Awaiting update'}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Link to={`/dashboard/construction/${p.id}`} className="flex-1 text-center bg-indigo-600 text-white rounded-lg py-2 text-sm font-semibold hover:bg-indigo-700 transition-colors">
                      View Progress
                    </Link>
                    <button onClick={() => void cancelProject(p.id, p.status === 'in_progress')} className="px-3 py-2 text-xs font-semibold text-red-600 hover:bg-red-50 rounded-lg transition-colors">
                      Cancel
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>

      {/* PENDING REQUESTS */}
      <section>
        <h2 className="text-lg font-bold mb-3">Pending Requests</h2>
        {pendingRequests.length === 0 ? (
          <p className="text-sm text-text-muted">No construction requests are waiting for a response.</p>
        ) : (
          <div className="space-y-2">
            {pendingRequests.map(r => {
              const reqDesign = designs.find(d => d.designId === r.houseDesignId) || { title: `Design v${r.designVersion}` };
              return (
                <div key={r.id} className="rounded-lg border p-3 flex items-center justify-between bg-surface shadow-sm">
                  <div>
                    <p className="font-semibold text-sm">Waiting for {r.constructorName} <span className="text-xs text-text-muted font-normal ml-1">({formatRequestRef(r.id)})</span></p>
                    <p className="text-xs text-text-muted mt-0.5">{reqDesign.title} <span className="ml-1">({formatDesignRef(r.houseDesignId)})</span> · Requested {new Date(r.requestedAt).toLocaleDateString()}</p>
                  </div>
                  <div className="text-amber-600 bg-amber-50 p-1.5 rounded-md border border-amber-100 shrink-0">
                    <Clock size={16} />
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>

      {/* START CONSTRUCTION */}
      <section className="bg-surface border rounded-xl p-4 md:p-5 shadow-sm">
        {!isStartOpen ? (
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <h2 className="text-lg font-bold">Start New Construction</h2>
              <p className="text-sm text-text-muted">Select an approved design and request quotes from constructors.</p>
            </div>
            <button onClick={() => setIsStartOpen(true)} className="bg-indigo-600 text-white text-sm font-semibold px-4 py-2 rounded-lg hover:bg-indigo-700 transition-colors shrink-0">
              Start
            </button>
          </div>
        ) : (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-bold">Start New Construction</h2>
              <button onClick={() => setIsStartOpen(false)} className="text-sm font-semibold text-text-muted hover:text-text-primary">Cancel</button>
            </div>

            {!designs.length ? (
              <p className="text-sm text-text-muted bg-gray-50 dark:bg-gray-800/50 p-4 rounded-lg border border-dashed text-center">No approved designs are available yet.</p>
            ) : (
              <div className="space-y-6">
                
                {/* Step 1 */}
                <div>
                  <p className="font-semibold text-sm mb-2 text-indigo-600">Step 1 — Select design</p>
                  {selectedDesign && design ? (
                    <div className="flex items-center justify-between bg-gray-50 dark:bg-gray-800/50 rounded-lg p-3 border dark:border-gray-700">
                      <div>
                        <p className="font-semibold text-sm">{design.title} <span className="text-xs font-normal text-text-muted ml-1">({formatDesignRef(design.designId)})</span></p>
                        <p className="text-xs text-text-muted mt-0.5">{design.bedrooms} Bedrooms · {design.bathrooms} Bathrooms · {design.floorCount} Floors</p>
                      </div>
                      <button onClick={() => setSelectedDesign('')} className="text-xs font-semibold text-indigo-600 hover:bg-indigo-50 px-3 py-1.5 rounded-md border border-indigo-100 bg-white dark:bg-gray-800 dark:border-gray-700 transition-colors">
                        Change
                      </button>
                    </div>
                  ) : (
                    <div className="grid sm:grid-cols-2 gap-2">
                      {designs.map(d => (
                        <button key={d.designId} onClick={() => setSelectedDesign(d.designId)} className="flex items-center justify-between text-left group rounded-lg border p-3 bg-surface hover:border-indigo-300 transition-colors">
                          <div>
                            <p className="font-semibold text-sm group-hover:text-indigo-600 transition-colors">{d.title} <span className="text-xs font-normal text-text-muted ml-1">({formatDesignRef(d.designId)})</span></p>
                            <p className="text-xs text-text-muted mt-0.5">{d.bedrooms} Beds · {d.bathrooms} Baths · {d.floorCount} Floors</p>
                          </div>
                          <ChevronRight size={16} className="text-text-muted group-hover:text-indigo-600 ml-2 shrink-0" />
                        </button>
                      ))}
                    </div>
                  )}
                </div>

                {/* Step 2 & 3 */}
                {selectedDesign && design && (
                  <div className="space-y-6 animate-in fade-in slide-in-from-bottom-2 duration-300">
                    <div>
                      <p className="font-semibold text-sm mb-2 text-indigo-600">Step 2 — Review estimate</p>
                      <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-3 border dark:border-gray-700">
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="text-xs text-text-muted mb-0.5 font-medium">Estimated construction cost</p>
                            {design.cost ? (
                              <p className="font-bold text-sm">LKR {design.cost.totalCostLkr.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
                            ) : (
                              <p className="text-sm font-medium text-text-muted">Not available yet</p>
                            )}
                          </div>
                          {design.cost && (
                            <button onClick={() => setShowCostBreakdown(!showCostBreakdown)} className="text-xs font-semibold text-indigo-600 flex items-center gap-1 hover:bg-indigo-50 px-2 py-1 rounded transition-colors shrink-0">
                              {showCostBreakdown ? 'Hide breakdown' : 'View breakdown'} {showCostBreakdown ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                            </button>
                          )}
                        </div>
                        {design.cost && showCostBreakdown && (
                          <div className="mt-3 pt-3 border-t border-dashed dark:border-gray-700">
                            <CostBreakdownCard cost={design.cost} />
                          </div>
                        )}
                      </div>
                    </div>

                    <div>
                      <p className="font-semibold text-sm mb-2 text-indigo-600">Step 3 — Choose constructor</p>
                      {!constructors.length ? (
                        <p className="text-xs text-text-muted p-3 bg-gray-50 rounded-lg border border-dashed dark:bg-gray-800/50 text-center">No registered constructors are currently available.</p>
                      ) : (
                        <div className="grid sm:grid-cols-2 gap-2">
                          {constructors.map(c => {
                            const isPending = pendingRequests.some(r => r.houseDesignId === selectedDesign && r.constructorName === c.name);
                            const isRequesting = requestingConstructor === c.id;
                            return (
                              <div key={c.id} className="flex flex-col justify-between rounded-lg border bg-surface p-3">
                                <div className="flex items-center gap-2 mb-3">
                                  <div className="bg-gray-100 p-1.5 rounded dark:bg-gray-800 shrink-0">
                                    <Building2 size={16} className="text-indigo-600" />
                                  </div>
                                  <p className="font-semibold text-sm leading-tight">{c.name}</p>
                                </div>
                                <div>
                                  {isPending ? (
                                    <span className="block text-center text-xs font-semibold text-amber-700 bg-amber-50 py-1.5 rounded-md border border-amber-100 w-full">Request pending</span>
                                  ) : (
                                    <button disabled={requestingConstructor !== null} onClick={() => void request(c.id)} className={`block w-full text-center bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold py-1.5 rounded-md transition-colors ${requestingConstructor ? 'opacity-50 cursor-not-allowed' : ''}`}>
                                      {isRequesting ? 'Sending...' : 'Send request'}
                                    </button>
                                  )}
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </section>

      {/* COMPLETED PROJECTS */}
      {completedProjects.length > 0 && (
        <section>
          <h2 className="text-lg font-bold mb-3">Completed Projects</h2>
          <div className="grid md:grid-cols-3 gap-3">
            {completedProjects.map(p => {
               const pDesign = designs.find(d => d.designId === p.houseDesignId) || { title: `Design v${p.designVersion}` };
               return (
                <Link key={p.id} to={`/dashboard/construction/${p.id}`} className="block rounded-xl border p-3 hover:border-indigo-300 transition-colors bg-surface shadow-sm">
                  <h3 className="font-semibold text-sm">{pDesign.title} <span className="text-xs text-text-muted font-normal ml-1">({formatDesignRef(p.houseDesignId)})</span></h3>
                  <p className="text-xs text-text-muted mt-0.5">{p.constructorName} <span className="ml-1">({formatProjectRef(p.id)})</span></p>
                </Link>
               );
            })}
          </div>
        </section>
      )}

    </main>
  );
}
