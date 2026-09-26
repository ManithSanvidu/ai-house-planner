import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { FloorPlanViewer } from '../components/floorplan/FloorPlanViewer';
import { preDesignedPlanService, type PreDesignedPlanDetail } from '../services/preDesignedPlanService';
import { customerConstructionService, type ConstructorProfile } from '../services/customerConstructionService';
import { countLabel, formatArea, formatFloorName, formatTerrain, formatTopology, getCustomerPlanName, getPlanTopology } from '../utils/presentation';
import type { CostSummaryDto } from '../services/workflowService';

export default function PlanDetailPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();

  const [plan, setPlan] = useState<PreDesignedPlanDetail & { estimatedCost?: CostSummaryDto | null } | null>(null);
  const [floor, setFloor] = useState(1);
  const [error, setError] = useState('');
  
  const [constructionState, setConstructionState] = useState<'LOADING' | 'NO_REQUEST' | 'PENDING' | 'ACCEPTED_OR_ACTIVE' | 'DECLINED' | 'CANCELLED'>('LOADING');

  const [constructors, setConstructors] = useState<ConstructorProfile[]>([]);
  const [selectedConstructor, setSelectedConstructor] = useState('');
  const [showConstructorSelect, setShowConstructorSelect] = useState(false);
  const [submittingRequest, setSubmittingRequest] = useState(false);
  const [pendingConstructorName, setPendingConstructorName] = useState('');

  useEffect(() => {
    preDesignedPlanService.detail(id)
      .then(setPlan)
      .catch(() => setError('Plan not found or unavailable.'));
  }, [id]);

  useEffect(() => {
    if (!plan) return;
    Promise.all([
      customerConstructionService.overview().catch(() => null),
      customerConstructionService.approvedDesigns().catch(() => []),
      customerConstructionService.constructors().catch(() => [])
    ]).then(([overview, approvedDesigns, availableConstructors]) => {
      setConstructors(availableConstructors);
      if (!overview) {
        setConstructionState('NO_REQUEST');
        return;
      }
      
      const activeProject = overview.activeProjects.find(p => p.houseDesignId === approvedDesigns.find(d => d.basePreDesignedPlanId === plan.id)?.designId);
      const pendingRequest = overview.pendingRequests.find(r => r.houseDesignId === approvedDesigns.find(d => d.basePreDesignedPlanId === plan.id)?.designId);
      const declinedRequest = overview.declinedRequests.find(r => r.houseDesignId === approvedDesigns.find(d => d.basePreDesignedPlanId === plan.id)?.designId);

      if (activeProject) {
        setConstructionState('ACCEPTED_OR_ACTIVE');
      } else if (pendingRequest) {
        setConstructionState('PENDING');
        setPendingConstructorName(pendingRequest.constructorName);
      } else if (declinedRequest) {
        setConstructionState('DECLINED');
      } else {
        setConstructionState('NO_REQUEST');
      }
    });
  }, [plan]);

  const handleRequestConstructor = async () => {
    if (!selectedConstructor || !plan) return;
    setSubmittingRequest(true);
    try {
      await customerConstructionService.requestFromPlan(plan.id, selectedConstructor);
      setConstructionState('PENDING');
      const c = constructors.find(x => x.id === selectedConstructor);
      if (c) setPendingConstructorName(c.name);
      setShowConstructorSelect(false);
    } catch (e: any) {
      alert(e.response?.data?.message || 'Failed to submit request');
    } finally {
      setSubmittingRequest(false);
    }
  };

  if (error) return <div role="alert" className="p-10">{error} <Link className="text-indigo-500" to="/dashboard/plans">Back to library</Link></div>;
  if (!plan) return <p className="p-10 text-zinc-500">Loading plan...</p>;

  const topology = getPlanTopology(plan);
  const displayCost = plan.estimatedCost;

  return (
    <main className="p-4 md:p-8 lg:p-10 max-w-7xl mx-auto text-zinc-900 dark:text-white space-y-8">
      <div>
        <Link className="text-indigo-600 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-400 font-medium" to="/dashboard/plans">
          ← Back to Plan Library
        </Link>
        <div className="mt-6 mb-2">
          <h1 className="text-3xl md:text-4xl font-bold">{getCustomerPlanName(plan)}</h1>
          <div className="flex items-center gap-3 mt-3 text-zinc-500 dark:text-zinc-400">
            <span>{plan.bedrooms} Bed • {plan.bathrooms} Bath • {plan.floorCount} Floor{plan.floorCount > 1 ? 's' : ''} • {formatArea(plan.totalBuiltUpAreaSqft)}</span>
            <span className="px-2 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">Architect Validated</span>
          </div>
          <p className="text-xs text-zinc-400 mt-2">Plan reference: {plan.designCode}</p>
        </div>
      </div>

      <div className="grid lg:grid-cols-[minmax(0,1fr)_340px] gap-8">
        
        {/* LEFT COLUMN - VIEWER */}
        <div className="space-y-6">
          <section className="h-[500px] md:h-[700px] min-w-0 rounded-2xl overflow-hidden border border-zinc-200 dark:border-gray-800 relative bg-white dark:bg-gray-950 shadow-sm">
            {plan.floorCount > 1 && (
              <div className="absolute z-10 m-4 flex flex-wrap gap-2">
                {Array.from({ length: plan.floorCount }, (_, i) => (
                  <button 
                    key={i} 
                    onClick={() => setFloor(i + 1)} 
                    className={`px-4 py-2 rounded-xl text-sm font-bold shadow-sm transition-colors focus-visible:ring-4 focus-visible:ring-indigo-300 ${floor === i + 1 ? 'bg-indigo-600 text-white border-transparent' : 'bg-white dark:bg-gray-900 text-zinc-700 dark:text-zinc-300 border border-zinc-200 dark:border-gray-700'}`}
                  >
                    {formatFloorName(i + 1)}
                  </button>
                ))}
              </div>
            )}
            {plan.floorCount === 1 && (
              <div className="absolute z-10 m-4">
                <span className="px-4 py-2 rounded-xl text-sm font-bold shadow-sm bg-white dark:bg-gray-900 text-zinc-700 dark:text-zinc-300 border border-zinc-200 dark:border-gray-700">Ground Floor</span>
              </div>
            )}
            <FloorPlanViewer data={plan.layout} floorFilter={floor} />
          </section>
        </div>

        {/* RIGHT COLUMN - SIDEBAR */}
        <aside className="space-y-6">
          
          <section className="bg-white dark:bg-gray-900 border border-zinc-100 dark:border-gray-800 rounded-2xl p-6 shadow-sm">
            <h2 className="text-lg font-bold mb-4">Plan Details</h2>
            <div className="space-y-3 text-sm">
              {[
                ['Bedrooms', countLabel(plan.bedrooms, '')],
                ['Bathrooms', countLabel(plan.bathrooms, '')],
                ['Floors', countLabel(plan.floorCount, '')],
                ['Built-up Area', formatArea(plan.totalBuiltUpAreaSqft)],
                ['Layout Type', topology ? formatTopology(topology) : 'Practical Layout'],
                ['Minimum Land', `${plan.minimumLandSizePerches} perches`],
                ['Minimum Plot Size', plan.minimumPlotWidthFt && plan.minimumPlotLengthFt ? `${plan.minimumPlotWidthFt} × ${plan.minimumPlotLengthFt} ft` : 'Flexible'],
                ['Terrain / Site', formatTerrain(plan.suitableTerrain)],
                ['Parking', `${plan.parkingSpaces} ${plan.parkingSpaces === 1 ? 'space' : 'spaces'}`]
              ].map(([k, v]) => (
                <div key={k} className="flex justify-between gap-3 border-b border-zinc-100 dark:border-gray-800 pb-2 last:border-0 last:pb-0">
                  <span className="text-zinc-500 dark:text-zinc-400">{k}</span>
                  <b className="text-right text-zinc-900 dark:text-zinc-100">{v}</b>
                </div>
              ))}
            </div>

            <div className="mt-5 pt-5 border-t border-zinc-100 dark:border-gray-800">
              <h3 className="text-xs font-bold text-zinc-400 uppercase tracking-wider mb-3">Supported Features</h3>
              <ul className="flex flex-wrap gap-2">
                {[
                  { key: 'hasSeparateDining', label: 'Separate Dining' },
                  { key: 'hasOffice', label: 'Home Office' },
                  { key: 'isAccessibleFriendly', label: 'Accessibility' }
                ].filter(f => (plan as any)[f.key]).map(f => (
                  <li key={f.key} className="bg-zinc-100 dark:bg-gray-800 text-zinc-700 dark:text-zinc-300 px-2.5 py-1 rounded text-xs font-medium border border-zinc-200 dark:border-gray-700">{f.label}</li>
                ))}
                {['hasSeparateDining', 'hasOffice', 'isAccessibleFriendly'].filter(k => (plan as any)[k]).length === 0 && (
                  <li className="text-xs text-zinc-500">Standard layout only</li>
                )}
              </ul>
            </div>
          </section>

          <section className="bg-white dark:bg-gray-900 border border-zinc-100 dark:border-gray-800 rounded-2xl p-6 shadow-sm">
            <h2 className="text-lg font-bold mb-3">Estimated Construction Cost</h2>
            {displayCost ? (
              <div>
                <p className="text-3xl font-bold text-zinc-900 dark:text-white">
                  LKR {displayCost.totalCostLkr.toLocaleString()}
                </p>
                <p className="text-xs text-zinc-500 mt-2 leading-relaxed">
                  Estimated project cost based on the current design.
                </p>
              </div>
            ) : (
              <p className="text-sm text-zinc-500 dark:text-zinc-400">
                Cost estimate currently unavailable.
              </p>
            )}
          </section>

          <section className="bg-white dark:bg-gray-900 border border-zinc-100 dark:border-gray-800 rounded-2xl p-6 shadow-sm">
            <h2 className="text-lg font-bold mb-3">Build This Design</h2>
            
            {constructionState === 'LOADING' && (
              <p className="text-sm text-zinc-500">Checking workflow status...</p>
            )}

            {(constructionState === 'NO_REQUEST' || constructionState === 'DECLINED' || constructionState === 'CANCELLED') && !showConstructorSelect && (
              <div>
                <p className="text-sm text-zinc-600 dark:text-zinc-400 mb-4">
                  Choose a constructor and send a request to build this design.
                </p>
                <button
                  onClick={() => setShowConstructorSelect(true)}
                  className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 px-4 rounded-xl transition-colors shadow-sm"
                >
                  Request Constructor
                </button>
              </div>
            )}

            {showConstructorSelect && (
              <div className="space-y-4">
                <p className="text-sm font-medium text-zinc-900 dark:text-zinc-100">Select a Constructor</p>
                <select 
                  className="w-full bg-white dark:bg-gray-950 border border-zinc-200 dark:border-gray-700 rounded-xl p-3 text-sm focus:ring-2 focus:ring-indigo-500 outline-none"
                  value={selectedConstructor}
                  onChange={(e) => setSelectedConstructor(e.target.value)}
                >
                  <option value="">-- Choose Constructor --</option>
                  {constructors.map(c => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
                {selectedConstructor && (
                  <button
                    disabled={submittingRequest}
                    onClick={handleRequestConstructor}
                    className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-bold py-3 px-4 rounded-xl transition-colors shadow-sm"
                  >
                    {submittingRequest ? 'Sending...' : `Send construction request to ${constructors.find(c => c.id === selectedConstructor)?.name}?`}
                  </button>
                )}
                <button onClick={() => setShowConstructorSelect(false)} className="w-full py-2 text-sm text-zinc-500 hover:text-zinc-800 dark:hover:text-zinc-300">
                  Cancel
                </button>
              </div>
            )}

            {constructionState === 'PENDING' && (
              <div className="bg-amber-50 dark:bg-amber-900/20 text-amber-900 dark:text-amber-300 p-4 rounded-xl text-sm font-medium border border-amber-100 dark:border-amber-800/30">
                Construction Request Pending with {pendingConstructorName}
              </div>
            )}

            {constructionState === 'ACCEPTED_OR_ACTIVE' && (
              <div>
                <button
                  onClick={() => navigate('/dashboard/construction')}
                  className="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-3 px-4 rounded-xl transition-colors shadow-sm"
                >
                  View Construction Progress
                </button>
              </div>
            )}
          </section>

          <p className="text-xs text-zinc-500 bg-zinc-50 dark:bg-gray-950 p-4 rounded-xl border border-zinc-100 dark:border-gray-800">
            {plan.conceptualDisclaimer}
          </p>

        </aside>
      </div>
    </main>
  );
}
