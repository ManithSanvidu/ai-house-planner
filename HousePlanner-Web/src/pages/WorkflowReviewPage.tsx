import React, { useEffect, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { workflowService, type WorkflowStatusResponseDto } from '../services/workflowService';
import { FloorPlanViewer, type FloorPlanData } from '../components/floorplan/FloorPlanViewer';
import { Menu } from 'lucide-react';
import { countLabel, formatArea, formatFloorName, formatFoundation, formatRoomName, formatTerrain, formatTopology, formatWorkflowStatus } from '../utils/presentation';

const roomGroup = (roomType: string) => {
  const type = roomType.toLowerCase();
  if (type.includes('bedroom')) return 'Bedrooms';
  if (type.includes('bathroom') || type.includes('ensuite')) return 'Bathrooms';
  if (['kitchen', 'utility', 'pantry', 'laundry'].some(value => type.includes(value))) return 'Kitchen & Utility';
  if (['hall', 'stair', 'foyer', 'landing', 'corridor'].some(value => type.includes(value))) return 'Circulation';
  if (['living', 'dining', 'lounge', 'family'].some(value => type.includes(value))) return 'Living Spaces';
  return 'Other Spaces';
};

export const WorkflowReviewPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const previewDesignId = searchParams.get('design') || undefined;
  const [workflow, setWorkflow] = useState<WorkflowStatusResponseDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedFloor, setSelectedFloor] = useState<number>(1);
  const [isSidebarOpen, setIsSidebarOpen] = useState<boolean>(true);
  const [activeTab, setActiveTab] = useState<'floorplan' | 'construction'>('floorplan');
  const [pollCycle, setPollCycle] = useState(0);
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    if (!id) return;

    let interval: ReturnType<typeof setInterval>;

    const fetchWorkflow = async () => {
      try {
        const data = await workflowService.getWorkflowStatus(id, previewDesignId);
        setWorkflow(data);
        setError(null);
        setLoading(false);

        // If the status is no longer running, we can stop polling
        if (data.status !== 'running' && data.status !== 'pending') {
          clearInterval(interval);
        }
      } catch (err: any) {
        if (err.response?.status === 404 || err.message?.includes('404')) {
          setError(null);
        } else {
          setError(err.message || 'Failed to fetch workflow status');
          setLoading(false);
          clearInterval(interval);
        }
      }
    };

    fetchWorkflow();
    interval = setInterval(() => { fetchWorkflow(); }, 3000);
    return () => clearInterval(interval);
  }, [id, pollCycle, previewDesignId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-65px)] bg-gradient-to-br from-slate-50 to-zinc-100">
        <div className="text-center p-8 bg-white/60 backdrop-blur-md rounded-2xl shadow-sm border border-white">
          <div className="relative inline-block mb-4">
            <div className="w-12 h-12 border-4 border-indigo-200 rounded-full"></div>
            <div className="w-12 h-12 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0 left-0"></div>
          </div>
          <p className="text-zinc-600 font-medium tracking-wide">Loading design environment...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-65px)] bg-gradient-to-br from-red-50 to-red-100/50">
        <div className="bg-white/80 backdrop-blur-xl p-10 rounded-[2rem] shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-red-100 max-w-md text-center">
          <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-6 shadow-inner">
            <div className="text-red-500 text-3xl font-bold">!</div>
          </div>
          <h2 className="text-2xl font-bold text-zinc-900 mb-3 tracking-tight">Error Loading Design</h2>
          <p className="text-red-600 text-sm font-medium bg-red-50 p-4 rounded-xl">{error}</p>
        </div>
      </div>
    );
  }

  if (workflow?.status === 'failed') {
    let failureData = null;
    try {
      failureData = JSON.parse(workflow.failureReason || '');
    } catch {
      // Ignored
    }

    if (failureData?.code === 'BUILDABLE_ENVELOPE_VIOLATION') {
      return <div role="alert" className="p-8 max-w-xl mx-auto text-center mt-10 bg-white rounded-3xl shadow-sm border border-zinc-200">
        <h2 className="text-2xl font-bold text-red-600 mb-3">Design generation could not complete</h2>
        <p className="text-zinc-600 mb-6">{failureData.message}</p>
        <div className="flex justify-center gap-4">
           <Link to="/dashboard/new-project" className="px-5 py-2.5 rounded-xl border border-zinc-300 font-bold hover:bg-zinc-50">Edit Land Details</Link>
        </div>
      </div>;
    }

    return <div role="alert" className="p-8 text-center mt-10">
      <h2 className="text-2xl font-bold text-red-600 mb-3">Design generation could not complete</h2>
      <p className="text-zinc-600">{workflow.failureReason || (workflow.terrainType === 'unknown'
        ? 'Provide a manual terrain classification and submit again.'
        : 'No valid layout was saved. Review plot dimensions and room requirements, then submit again.')}</p>
    </div>;
  }

  if (!workflow || !workflow.design) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-65px)] bg-gradient-to-br from-slate-50 to-zinc-100 relative overflow-hidden">
        <div className="absolute top-1/4 right-1/3 w-64 h-64 bg-indigo-200/40 rounded-full mix-blend-multiply filter blur-3xl opacity-50 animate-blob"></div>
        <div className="bg-white/90 backdrop-blur-xl p-12 rounded-[2rem] shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-white/60 max-w-md text-center relative z-10">
          <div className="w-20 h-20 bg-indigo-50 rounded-2xl flex items-center justify-center mx-auto mb-6 shadow-inner border border-indigo-100/50">
            <span className="text-4xl">🏗️</span>
          </div>
          <h2 className="text-2xl font-bold text-zinc-900 mb-3 tracking-tight">Design In Progress</h2>
          <p className="text-zinc-500 text-sm leading-relaxed mb-6 font-medium">
            The AI Architect is actively computing geometries and generating your house layout. This page will update automatically.
          </p>
          <div className="inline-flex items-center gap-2 bg-zinc-100/80 px-4 py-2 rounded-full border border-zinc-200/50">
            <span className="relative flex h-2.5 w-2.5">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-indigo-500"></span>
            </span>
            <p className="text-xs text-zinc-600 font-bold uppercase tracking-wider">{workflow?.status?.replace(/_/g, ' ') || 'initializing'}</p>
          </div>
        </div>
      </div>
    );
  }

  const floorPlanData: FloorPlanData = {
    design_id: workflow.design.designId,
    floor_count: workflow.design.floorCount,
    total_built_up_area_sqft: workflow.design.totalBuiltUpAreaSqft,
    entrances: workflow.design.entrances,
    rooms: workflow.design.rooms.map(r => ({
      room_id: r.roomId,
      room_type: r.roomType,
      name: r.name || undefined,
      floor: r.floorNumber,
      x: r.x,
      y: r.y,
      width: r.width,
      length: r.length,
      wall_height: r.wallHeight,
      doors: r.doors || [],
      windows: r.windows || []
    }))
  };

  const floorNumbers = Array.from({ length: workflow.design.floorCount }, (_, i) => i + 1);

  const handleAction = async (decision: 'approve' | 'reject' | 'request_revision', fixedNotes?: string) => {
    if (!id || actionLoading) return;
    setActionLoading(true);
    try {
      const notes = fixedNotes ?? (decision === 'request_revision'
        ? window.prompt('Describe the design change you want:')
        : decision === 'reject' ? 'Architect rejected' : undefined);
      if (decision === 'request_revision' && !notes) return;
      await workflowService.approveWorkflow(id, decision, notes || undefined);
      if (decision === 'request_revision') {
        setWorkflow(current => current ? { ...current, status: 'running' } : current);
        setPollCycle(cycle => cycle + 1);
      }
      alert(`Workflow ${decision} submitted successfully!`);
    } catch (e: any) {
      alert(`Error: ${e.message}`);
    } finally {
      setActionLoading(false);
    }
  };

  const selectThisDesign = async () => {if(!id||!workflow?.design||actionLoading)return;setActionLoading(true);try{await workflowService.selectDesign(id,workflow.design.designId);setPollCycle(x=>x+1)}catch(e:any){setError(e.response?.data?.message||'Could not select this design.')}finally{setActionLoading(false)}};
  const sendToArchitect = async () => {if(!id||actionLoading)return;setActionLoading(true);try{await workflowService.submitArchitectReview(id);setPollCycle(x=>x+1)}catch(e:any){setError(e.response?.data?.message||'Could not send this design to the architect.')}finally{setActionLoading(false)}};

  const bedroomCount = workflow.design.rooms.filter(r => r.roomType.includes('bedroom')).length;
  const bathroomCount = workflow.design.rooms.filter(r => r.roomType.includes('bathroom')).length;
  const groups = workflow.design.rooms.reduce<Record<string, typeof workflow.design.rooms>>((result, room) => {
    const group = roomGroup(room.roomType);
    (result[group] ||= []).push(room);
    return result;
  }, {});
  const groupOrder = ['Bedrooms', 'Living Spaces', 'Kitchen & Utility', 'Bathrooms', 'Circulation', 'Other Spaces'];
  const topology = formatTopology(workflow.design.templateFamily);
  const site = formatTerrain(workflow.terrainType);
  const approved = workflow.status === 'approved' || workflow.architectReviewStatus === 'Approved';
  const pendingReview = workflow.status === 'awaiting_architect_review' || workflow.architectReviewStatus === 'Pending' || workflow.architectReviewStatus === 'Under Review';
  const rejected = workflow.architectReviewStatus === 'Rejected' || workflow.status === 'revision_requested';
  const selected = workflow.preferredHouseDesignId === workflow.design.designId;

  return (
    <div className="min-h-[calc(100vh-65px)] bg-slate-50 text-zinc-900">
      <header className="bg-white border-b border-zinc-200 px-5 py-6 md:px-8">
        <div className="max-w-7xl mx-auto flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <p className="text-sm font-semibold text-indigo-600">YOUR HOME DESIGN</p>
            <h1 className="text-2xl md:text-3xl font-bold mt-1">Your Home Design</h1>
            <p className="text-base md:text-lg text-zinc-700 mt-2">
              {countLabel(bedroomCount, 'Bedroom')} <span aria-hidden="true">•</span> {countLabel(bathroomCount, 'Bathroom')} <span aria-hidden="true">•</span> {countLabel(workflow.design.floorCount, 'Floor')} <span aria-hidden="true">•</span> {formatArea(workflow.design.totalBuiltUpAreaSqft)}
            </p>
            <p className="text-sm text-zinc-500 mt-2">Designed for {site === 'Not specified' ? 'your site' : `${site.toLowerCase()} land`} with a practical {topology.toLowerCase()} layout.</p>
          </div>
          <div className="md:text-right">
            <span className="text-xs font-semibold text-zinc-500">Status</span>
            <p className="mt-1 inline-flex md:flex px-3 py-1.5 rounded-full bg-emerald-100 text-emerald-800 font-semibold">{formatWorkflowStatus(workflow.status)}</p>
          </div>
        </div>
      </header>
      <div className="flex flex-col xl:flex-row min-h-[680px]">
      {/* ─── Sidebar: Details ─── */}
      {isSidebarOpen && (
        <aside className="w-full xl:w-80 bg-white border-b xl:border-b-0 xl:border-r border-zinc-200 flex flex-col shrink-0 z-10">
          <div className="p-6 border-b border-zinc-100 flex justify-between items-center bg-gradient-to-b from-zinc-50/50 to-transparent">
            <div>
              <h2 className="text-xl font-bold text-zinc-900 tracking-tight">Home Summary</h2>
              <p className="text-sm text-zinc-500 mt-1">The key details of this design.</p>
            </div>
          </div>
          
        <div className="p-6 space-y-5 flex-1">
          {/* Specifications */}
          <div className="bg-zinc-50 p-4 rounded-xl border border-zinc-100 shadow-sm">
            <dl className="text-sm text-zinc-700 space-y-3">
              <div className="flex justify-between gap-3"><dt className="font-medium text-zinc-500">Bedrooms</dt><dd className="font-bold">{bedroomCount}</dd></div>
              <div className="flex justify-between gap-3"><dt className="font-medium text-zinc-500">Bathrooms</dt><dd className="font-bold">{bathroomCount}</dd></div>
              <div className="flex justify-between gap-3"><dt className="font-medium text-zinc-500">Floors</dt><dd className="font-bold">{workflow.design.floorCount}</dd></div>
              <div className="flex justify-between gap-3"><dt className="font-medium text-zinc-500">Area</dt><dd className="font-bold">{formatArea(workflow.design.totalBuiltUpAreaSqft)}</dd></div>
              <div className="border-t border-zinc-200 pt-3 flex justify-between gap-3"><dt className="font-medium text-zinc-500">Site</dt><dd className="font-bold text-right">{site}</dd></div>
              <div className="flex justify-between gap-3"><dt className="font-medium text-zinc-500">Foundation</dt><dd className="font-bold text-right">{formatFoundation(workflow.design.foundationType)}</dd></div>
              <div className="flex justify-between gap-3"><dt className="font-medium text-zinc-500">Layout</dt><dd className="font-bold text-right">{topology}</dd></div>
            </dl>
            {workflow.design.designScore != null && <details className="mt-4 border-t border-zinc-200 pt-3 text-sm"><summary className="cursor-pointer font-semibold text-zinc-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-400">Design details</summary><p className="mt-2 text-zinc-500">Architectural quality score: {workflow.design.designScore}</p></details>}
          </div>

          <div className="flex flex-col gap-3">
            {approved ? <div className="p-3 rounded-xl bg-emerald-100 text-emerald-800 font-bold text-center">✓ Architect Approved</div>
            : pendingReview ? <div className="p-3 rounded-xl bg-amber-100 text-amber-900 font-bold text-center">Awaiting Architect Review</div>
            : rejected ? <><div className="p-3 rounded-xl bg-red-50 text-red-800"><b>Design Needs Changes</b>{workflow.architectFeedback&&<p className="mt-1 font-normal">Architect feedback: “{workflow.architectFeedback}”</p>}</div><button disabled={actionLoading} onClick={()=>handleAction('request_revision','Generate Another')} className="w-full py-3 bg-indigo-600 text-white rounded-xl font-bold disabled:opacity-50">Generate New Design</button></>
            : selected ? <><div className="p-3 rounded-xl bg-emerald-50 text-emerald-800 font-bold text-center">✓ Selected Design</div><button disabled={actionLoading} onClick={sendToArchitect} className="w-full py-3 bg-indigo-600 text-white rounded-xl font-bold disabled:opacity-50">{actionLoading?'Sending…':'Send to Architect for Review'}</button><button disabled={actionLoading} onClick={()=>handleAction('request_revision','Generate Another')} className="w-full py-3 border border-indigo-200 text-indigo-700 rounded-xl font-bold">Generate Another</button></>
            : <><button disabled={actionLoading} onClick={selectThisDesign} className="w-full py-3 bg-indigo-600 text-white rounded-xl font-bold disabled:opacity-50">{actionLoading?'Selecting…':'Select This Design'}</button><button disabled={actionLoading} onClick={()=>handleAction('request_revision','Generate Another')} className="w-full py-3 border border-indigo-200 text-indigo-700 rounded-xl font-bold">Generate Another</button></>}
          </div>
          
          {/* Room Summary */}
          <div className="bg-zinc-50 p-4 rounded-xl border border-zinc-100 shadow-sm">
            <h3 className="text-sm text-zinc-700 font-bold mb-3 flex items-center justify-between">
              Rooms <span className="bg-zinc-200 text-zinc-600 px-2 py-0.5 rounded-full text-xs">{workflow.design.rooms.length}</span>
            </h3>
            <div className="space-y-4 max-h-80 overflow-y-auto pr-1">
              {groupOrder.filter(group => groups[group]?.length).map(group => <section key={group}><h4 className="text-xs uppercase tracking-wide text-zinc-400 font-bold mb-1.5">{group}</h4><ul className="space-y-1.5">{groups[group].map(r => <li key={r.roomId} className="flex justify-between gap-3 text-xs p-2 bg-white rounded-lg border border-zinc-100"><span className="font-semibold text-zinc-700">{r.name || formatRoomName(r.roomType)}</span><span className="text-zinc-500 whitespace-nowrap">{r.width} × {r.length} ft{floorPlanData.floor_count > 1 ? ` · ${formatFloorName(r.floorNumber)}` : ''}</span></li>)}</ul></section>)}
            </div>
          </div>
        </div>
      </aside>
      )}

      {/* ─── Main Area ─── */}
      <div className="flex-1 min-w-0 flex flex-col relative bg-white min-h-[560px]">
        {/* Top Action Bar */}
        <div className="bg-white border-b border-slate-200 px-3 md:px-4 py-3 flex flex-wrap items-center gap-3">
          <button aria-label={isSidebarOpen ? 'Hide design summary' : 'Show design summary'} onClick={() => setIsSidebarOpen(!isSidebarOpen)} className="p-2 text-slate-500 hover:bg-slate-100 rounded-md transition-colors focus-visible:ring-2 focus-visible:ring-indigo-400" title="Toggle design summary">
            <Menu size={20} />
          </button>
          
          {/* Main View Tabs */}
          <div className="flex gap-1 bg-slate-100 p-1 rounded-lg">
            <button
              onClick={() => setActiveTab('floorplan')}
              className={`px-4 py-1.5 rounded-md text-sm font-bold transition-all ${
                activeTab === 'floorplan' ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              Floor Plan
            </button>
            <button
              onClick={() => setActiveTab('construction')}
              className={`px-4 py-1.5 rounded-md text-sm font-bold transition-all flex items-center gap-2 ${
                activeTab === 'construction' ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              🚧 Construction Plan
            </button>
          </div>

          {/* Floor Tabs (Only show in floorplan view) */}
          {activeTab === 'floorplan' && floorNumbers.length > 1 && (
            <div className="flex flex-wrap gap-2 md:ml-auto w-full md:w-auto">
              {floorNumbers.map(floor => (
                <button
                  key={floor}
                  onClick={() => setSelectedFloor(floor)}
                  className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-colors focus-visible:ring-4 focus-visible:ring-indigo-300 ${
                    selectedFloor === floor
                      ? 'bg-indigo-600 text-white shadow-sm'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {formatFloorName(floor)}
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Content Area */}
        <div className="flex-1 overflow-auto relative min-h-[480px]">
          {activeTab === 'floorplan' ? (
            <>
              {workflow.design.plotConstraints?.dimensions_estimated &&
                <p className="px-4 py-2 text-sm text-zinc-500 bg-amber-50 border-b border-amber-100">Plot dimensions are estimated. Supply measured width and length to refine the plan.</p>}
              <FloorPlanViewer
                data={floorPlanData}
                pixelsPerFoot={22}
                floorFilter={selectedFloor}
              />
            </>
          ) : (
            // Construction Plan View
            <div className="p-8 max-w-4xl mx-auto">
              {!workflow.constructionPlan ? (
                <div className="text-center p-12 bg-slate-50 rounded-2xl border border-dashed border-slate-300">
                  <p className="text-slate-500 font-medium">No construction plan has been generated for this design yet.</p>
                </div>
              ) : (
                <div className="space-y-8 animate-fade-in">
                  
                  {/* Summary Header */}
                  <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex items-center justify-between">
                    <div>
                      <h2 className="text-2xl font-bold text-slate-800 mb-1">Project Timeline Estimate</h2>
                      <p className="text-slate-500">AI-generated construction roadmap based on architectural design</p>
                    </div>
                    <div className="text-right">
                      <div className="text-3xl font-black text-indigo-600">
                        {workflow.constructionPlan.project_summary.estimated_duration_days} <span className="text-lg text-slate-400 font-medium">days</span>
                      </div>
                      <div className="text-sm font-bold text-slate-400">
                        (~{workflow.constructionPlan.project_summary.estimated_duration_months} months)
                      </div>
                    </div>
                  </div>

                  {/* Target & Status */}
                  <div className="grid grid-cols-2 gap-4">
                    <div className="bg-slate-50 p-4 rounded-xl border border-slate-100">
                      <h4 className="text-xs uppercase tracking-widest text-slate-400 font-bold mb-1">Target Duration</h4>
                      <p className="text-lg font-semibold text-slate-700">
                        {workflow.constructionPlan.project_summary.target_duration_days ? `${workflow.constructionPlan.project_summary.target_duration_days} days` : 'Not Provided'}
                      </p>
                    </div>
                    <div className="bg-slate-50 p-4 rounded-xl border border-slate-100">
                      <h4 className="text-xs uppercase tracking-widest text-slate-400 font-bold mb-1">Schedule Status</h4>
                      <p className={`text-lg font-bold ${
                        workflow.constructionPlan.project_summary.schedule_status === 'ON_SCHEDULE' ? 'text-emerald-600' : 'text-amber-600'
                      }`}>
                        {workflow.constructionPlan.project_summary.schedule_status.replace('_', ' ')}
                      </p>
                    </div>
                  </div>

                  {/* Phases List */}
                  <div>
                    <h3 className="text-lg font-bold text-slate-800 mb-4">Construction Phases</h3>
                    <div className="space-y-3">
                      {workflow.constructionPlan.phases.map((phase: any) => (
                        <div key={phase.id} className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center justify-between hover:border-indigo-200 transition-colors">
                          <div className="flex items-center gap-4">
                            <div className="w-10 h-10 rounded-full bg-indigo-50 text-indigo-600 font-bold flex items-center justify-center shrink-0">
                              {phase.id}
                            </div>
                            <div>
                              <h4 className="font-bold text-slate-700">{phase.name}</h4>
                              <p className="text-xs text-slate-500 mt-1">
                                {phase.depends_on.length > 0 ? `Depends on: ${phase.depends_on.join(', ')}` : 'No dependencies'}
                              </p>
                            </div>
                          </div>
                          <div className="text-right">
                            <div className="font-bold text-slate-800">{phase.duration_days} days</div>
                            <div className="text-xs font-bold text-indigo-500 bg-indigo-50 px-2 py-1 rounded mt-1 inline-block">
                              Day {phase.start_day} – {phase.end_day}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Critical Path & Notes */}
                  <div className="grid grid-cols-2 gap-6">
                    <div className="bg-indigo-50/50 p-5 rounded-2xl border border-indigo-100">
                      <h4 className="font-bold text-indigo-900 mb-3">Critical Path</h4>
                      <ol className="list-decimal list-inside text-sm text-indigo-700/80 space-y-1">
                        {workflow.constructionPlan.critical_path.map((cp: any) => <li key={cp}>{cp}</li>)}
                      </ol>
                    </div>
                    
                    <div className="space-y-4">
                      {workflow.constructionPlan.optimization_notes.length > 0 && (
                        <div className="bg-amber-50 p-5 rounded-2xl border border-amber-100">
                          <h4 className="font-bold text-amber-900 mb-3">Optimization Notes</h4>
                          <ul className="list-disc list-inside text-sm text-amber-700/80 space-y-1">
                            {workflow.constructionPlan.optimization_notes.map((note: any) => <li key={note}>{note}</li>)}
                          </ul>
                        </div>
                      )}
                      
                      <div className="bg-slate-50 p-5 rounded-2xl border border-slate-200">
                        <h4 className="font-bold text-slate-700 mb-3">AI Assumptions</h4>
                        <ul className="list-disc list-inside text-sm text-slate-500 space-y-1">
                          {workflow.constructionPlan.assumptions.map((assumption: any) => <li key={assumption}>{assumption}</li>)}
                        </ul>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
      </div>
    </div>
  );
};
