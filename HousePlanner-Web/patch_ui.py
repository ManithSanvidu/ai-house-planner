import sys

with open('src/pages/WorkflowReviewPage.tsx', 'r') as f:
    lines = f.readlines()

# find the return statement around line 333
idx = -1
for i, line in enumerate(lines):
    if line.strip() == 'return (' and 'const rejected =' in lines[i-2]:
        idx = i
        break

if idx == -1:
    print("Could not find return statement")
    sys.exit(1)

new_return = """  return (
   <div className="min-h-[calc(100vh-65px)] bg-slate-50 text-zinc-900">
    <div className="max-w-6xl mx-auto px-4 py-8">
     {/* Tabs for Design vs Construction Plan */}
     <div className="flex gap-2 mb-6 bg-slate-200/50 p-1.5 rounded-xl w-fit">
      <button
       onClick={() => setActiveTab('floorplan')}
       className={`px-6 py-2 rounded-lg text-sm font-bold transition-all ${
        activeTab === 'floorplan' ? 'bg-white text-indigo-600 shadow-sm' : 'text-text-muted hover:text-slate-700'
       }`}
      >
       Architectural Review
      </button>
      <button
       onClick={() => setActiveTab('construction')}
       className={`px-6 py-2 rounded-lg text-sm font-bold transition-all flex items-center gap-2 ${
        activeTab === 'construction' ? 'bg-white text-indigo-600 shadow-sm' : 'text-text-muted hover:text-slate-700'
       }`}
      >
       🚧 Construction Plan
      </button>
     </div>

     {activeTab === 'floorplan' ? (
      <div className="space-y-8 animate-fade-in">
       
       {/* Section A: AI Home Design */}
       <section className="bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200">
        <div className="flex flex-col md:flex-row md:items-center justify-between mb-6 gap-4">
         <div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-900">AI Home Design</h1>
          <p className="text-slate-500 mt-1">Generated specifically for {workflow.landSizeCategory === 'small' ? 'a small plot' : 'your plot'}.</p>
         </div>
         <div className="flex items-center gap-3">
          <span className="text-sm font-bold text-slate-500 uppercase tracking-widest">Status</span>
          <span className="px-4 py-2 bg-indigo-50 text-indigo-700 rounded-full text-sm font-bold border border-indigo-100">
           {formatWorkflowStatus(workflow.status)}
          </span>
         </div>
        </div>

        <div className="bg-zinc-950 rounded-2xl overflow-hidden shadow-inner min-h-[400px] flex items-center justify-center relative">
         {visualizationData?.status === 'completed' && visualizationData?.imageUrl ? (
          <img src={visualizationData.imageUrl} alt="AI Visualization" className="w-full max-h-[600px] object-contain" />
         ) : visualizationData?.status === 'generating' || visualizationData === null ? (
          <div className="text-zinc-500 flex flex-col items-center gap-4">
           <div className="w-10 h-10 border-4 border-zinc-800 border-t-indigo-500 rounded-full animate-spin"></div>
           <span className="text-sm font-medium">Generating AI visualization...</span>
          </div>
         ) : (
          <div className="text-zinc-500">Visualization failed to generate.</div>
         )}
        </div>
       </section>

       <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        {/* Section B: Your Request */}
        <section className="bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200 h-fit">
         <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <span className="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-500">📋</span>
          Your Request
         </h2>
         <div className="space-y-4 text-sm">
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Land Size</span>
           <span className="font-bold">{workflow.landSizePerches} perches</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bedrooms</span>
           <span className="font-bold">{workflow.bedrooms}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bathrooms</span>
           <span className="font-bold">{workflow.bathrooms}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">House Type</span>
           <span className="font-bold capitalize">{workflow.houseType}</span>
          </div>
          <div className="flex justify-between items-center py-3">
           <span className="text-slate-500 font-medium">Floors</span>
           <span className="font-bold">{workflow.design?.floorCount}</span>
          </div>
         </div>
        </section>

        {/* Section C: Generated Design */}
        <section className="bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200 h-fit">
         <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <span className="w-8 h-8 rounded-full bg-indigo-50 flex items-center justify-center text-indigo-500">✨</span>
          Generated Design Rooms
         </h2>
         <p className="text-sm text-slate-500 mb-4">The AI deterministic engine arranged these rooms for your plan:</p>
         <div className="bg-slate-50 rounded-2xl p-4 border border-slate-100 max-h-80 overflow-y-auto">
          <ul className="space-y-2">
           {workflow.design?.rooms.map(r => (
            <li key={r.roomId} className="flex items-center gap-3 p-2 hover:bg-slate-100 rounded-lg transition-colors">
             <div className="w-2 h-2 rounded-full bg-indigo-400 shrink-0"></div>
             <span className="font-semibold text-slate-700 text-sm">{r.name || formatRoomName(r.roomType)}</span>
            </li>
           ))}
          </ul>
         </div>
        </section>
       </div>

       {/* Section D: Technical Blueprint (Advanced) */}
       {showTechnicalPlan && (
        <section className="bg-white rounded-3xl shadow-sm border border-slate-200 overflow-hidden">
         <details className="group">
          <summary className="p-6 md:p-8 cursor-pointer list-none flex items-center justify-between font-bold text-xl hover:bg-slate-50 transition-colors focus-visible:outline-none">
           <div className="flex items-center gap-2">
            <span className="w-8 h-8 rounded-full bg-slate-800 flex items-center justify-center text-white text-sm">📐</span>
            Technical Blueprint (Advanced)
           </div>
           <span className="text-slate-400 group-open:rotate-180 transition-transform duration-300">▼</span>
          </summary>
          
          <div className="p-6 md:p-8 border-t border-slate-100 bg-slate-50">
           <p className="text-sm text-slate-500 mb-6">Deterministic geometry generated from LayoutJson.</p>
           
           <div className="overflow-x-auto bg-white rounded-xl border border-slate-200 shadow-sm mb-8">
            <table className="w-full text-left text-sm">
             <thead className="bg-slate-100 text-slate-600">
              <tr>
               <th className="p-4 font-bold border-b border-slate-200">Room Name</th>
               <th className="p-4 font-bold border-b border-slate-200">X Coordinate</th>
               <th className="p-4 font-bold border-b border-slate-200">Y Coordinate</th>
               <th className="p-4 font-bold border-b border-slate-200">Width (ft)</th>
               <th className="p-4 font-bold border-b border-slate-200">Length (ft)</th>
              </tr>
             </thead>
             <tbody className="divide-y divide-slate-100">
              {workflow.design?.rooms.map(r => (
               <tr key={r.roomId} className="hover:bg-slate-50">
                <td className="p-4 font-semibold text-slate-700">{r.name || formatRoomName(r.roomType)}</td>
                <td className="p-4 font-mono text-slate-500">{r.x}</td>
                <td className="p-4 font-mono text-slate-500">{r.y}</td>
                <td className="p-4 font-mono text-slate-500">{r.width}</td>
                <td className="p-4 font-mono text-slate-500">{r.length}</td>
               </tr>
              ))}
             </tbody>
            </table>
           </div>

           <div className="h-[600px] bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden relative">
            <div className="absolute top-4 left-4 z-10 flex gap-2">
             {floorNumbers.length > 1 && floorNumbers.map(floor => (
              <button
               key={floor}
               onClick={() => setSelectedFloor(floor)}
               className={`px-4 py-1.5 rounded-lg text-sm font-bold shadow-sm ${
                selectedFloor === floor ? 'bg-slate-800 text-white' : 'bg-white text-slate-600 hover:bg-slate-100'
               }`}
              >
               {formatFloorName(floor)}
              </button>
             ))}
            </div>
            <FloorPlanViewer data={floorPlanData} pixelsPerFoot={24} floorFilter={selectedFloor} />
           </div>
          </div>
         </details>
        </section>
       )}

      </div>
     ) : (
      // Construction Plan View
      <div className="animate-fade-in bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200">
       {!workflow.constructionPlan ? (
        <div className="text-center p-12 bg-slate-50 rounded-2xl border border-dashed border-slate-300">
         <p className="text-text-muted font-medium">No construction plan has been generated for this design yet.</p>
        </div>
       ) : (
        <div className="space-y-8">

         {/* Summary Header */}
         <div className="bg-slate-50 p-6 rounded-2xl border border-slate-100 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
           <div className="flex items-center gap-3 mb-1">
            <h2 className="text-2xl font-bold text-slate-800">Project Timeline Estimate</h2>
            {isAdmin && !isEditingPlan && (
             <button onClick={startEditingPlan} className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors">
              <Edit2 size={16} />
             </button>
            )}
           </div>
           <p className="text-slate-500 text-sm">AI-generated construction roadmap based on architectural design</p>
          </div>
          <div className="text-right">
           <div className="text-3xl font-black text-indigo-600">
            {workflow.constructionPlan.project_summary.estimated_duration_days} <span className="text-lg text-slate-500 font-medium">days</span>
           </div>
           <div className="text-sm font-bold text-slate-400">
            (~{workflow.constructionPlan.project_summary.estimated_duration_months} months)
           </div>
          </div>
         </div>

         {/* Target & Status */}
         <div className="grid grid-cols-2 gap-4">
          <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
           <h4 className="text-xs uppercase tracking-widest text-slate-400 font-bold mb-1">Target Duration</h4>
           {isEditingPlan ? (
            <input
             type="number"
             value={editTargetDuration}
             onChange={e => setEditTargetDuration(e.target.value ? Number(e.target.value) : '')}
             className="w-full bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-sm font-semibold text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
             placeholder="e.g. 150"
            />
           ) : (
            <p className="text-lg font-semibold text-slate-700">
             {workflow.constructionPlan.project_summary.target_duration_days ? `${workflow.constructionPlan.project_summary.target_duration_days} days` : 'Not Provided'}
            </p>
           )}
          </div>
          <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
           <h4 className="text-xs uppercase tracking-widest text-slate-400 font-bold mb-1">Schedule Status</h4>
           {isEditingPlan ? (
            <select
             value={editScheduleStatus}
             onChange={e => setEditScheduleStatus(e.target.value)}
             className="w-full bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-sm font-bold focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
             <option value="ON_SCHEDULE">ON SCHEDULE</option>
             <option value="DELAYED">DELAYED</option>
             <option value="AHEAD_OF_SCHEDULE">AHEAD OF SCHEDULE</option>
            </select>
           ) : (
            <p className={`text-lg font-bold ${
             workflow.constructionPlan.project_summary.schedule_status === 'ON_SCHEDULE' ? 'text-emerald-600' : 
             workflow.constructionPlan.project_summary.schedule_status === 'DELAYED' ? 'text-red-600' : 'text-amber-600'
            }`}>
             {workflow.constructionPlan.project_summary.schedule_status.replace(/_/g, ' ')}
            </p>
           )}
          </div>
         </div>
         
         {isEditingPlan && (
          <div className="flex justify-end gap-3 mt-4">
           <button onClick={() => setIsEditingPlan(false)} className="px-4 py-2 rounded-lg text-slate-600 font-semibold hover:bg-slate-100 text-sm">Cancel</button>
           <button onClick={handleSavePlan} disabled={isSavingPlan} className="px-4 py-2 rounded-lg bg-indigo-600 text-white text-sm font-semibold disabled:opacity-50 flex items-center gap-2">
            <Save size={16} /> {isSavingPlan ? 'Saving...' : 'Save Plan'}
           </button>
          </div>
         )}

         {/* Phases List */}
         <div>
          <div className="flex items-center justify-between mb-4 mt-8">
           <h3 className="text-xl font-bold text-slate-800">Construction Phases</h3>
          </div>
          <div className="space-y-3">
           {workflow.constructionPlan.phases.map((phase: any) => (
            <div key={phase.id} className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col md:flex-row md:items-center justify-between hover:border-indigo-300 transition-colors gap-4">
             {editingPhaseId === phase.id ? (
              // Edit Phase Form
              <div className="w-full grid grid-cols-1 md:grid-cols-2 gap-4">
               <div className="space-y-3">
                <div><label className="text-xs font-bold text-slate-500 uppercase">Phase Name</label><input type="text" value={phaseFormData.name} onChange={e => setPhaseFormData({...phaseFormData, name: e.target.value})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" /></div>
                <div className="grid grid-cols-3 gap-2">
                 <div><label className="text-xs font-bold text-slate-500 uppercase">Duration</label><input type="number" value={phaseFormData.duration_days} onChange={e => setPhaseFormData({...phaseFormData, duration_days: Number(e.target.value)})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" /></div>
                 <div><label className="text-xs font-bold text-slate-500 uppercase">Start Day</label><input type="number" value={phaseFormData.start_day} onChange={e => setPhaseFormData({...phaseFormData, start_day: Number(e.target.value)})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" /></div>
                 <div><label className="text-xs font-bold text-slate-500 uppercase">End Day</label><input type="number" value={phaseFormData.end_day} onChange={e => setPhaseFormData({...phaseFormData, end_day: Number(e.target.value)})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" /></div>
                </div>
               </div>
               <div className="flex items-end justify-end gap-2 pb-1">
                <button onClick={cancelPhaseEdit} className="p-2 text-slate-500 hover:bg-slate-100 rounded-lg"><X size={18}/></button>
                <button onClick={savePhase} disabled={isSavingPlan} className="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-bold disabled:opacity-50 flex items-center gap-2"><Save size={16}/> Save</button>
               </div>
              </div>
             ) : (
              // View Phase
              <>
               <div className="flex items-center gap-4">
                <div className="w-10 h-10 rounded-full bg-indigo-50 text-indigo-600 font-bold flex items-center justify-center shrink-0 border border-indigo-100">
                 {phase.id}
                </div>
                <div>
                 <h4 className="font-bold text-slate-800">{phase.name}</h4>
                 <p className="text-xs text-slate-500 mt-1 font-medium">
                  {phase.depends_on.length > 0 ? `Depends on: ${phase.depends_on.join(', ')}` : 'No dependencies'}
                 </p>
                </div>
               </div>
               <div className="flex items-center gap-6">
                <div className="text-right">
                 <div className="font-bold text-slate-800">{phase.duration_days} days</div>
                 <div className="text-xs font-bold text-indigo-600 bg-indigo-50 border border-indigo-100 px-2 py-1 rounded-md mt-1 inline-block">
                  Day {phase.start_day} – {phase.end_day}
                 </div>
                </div>
                {isAdmin && (
                 <div className="flex gap-1 border-l border-slate-100 pl-4">
                  <button onClick={() => startEditingPhase(phase)} className="p-2 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"><Edit2 size={16}/></button>
                  <button onClick={() => deletePhase(phase.id)} className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"><Trash2 size={16}/></button>
                 </div>
                )}
               </div>
              </>
             )}
            </div>
           ))}
           
           {/* Add Phase Form */}
           {isAddingPhase && (
            <div className="bg-white p-4 rounded-xl border-2 border-indigo-200 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4">
              <div className="w-full grid grid-cols-1 md:grid-cols-2 gap-4">
               <div className="space-y-3">
                <div><label className="text-xs font-bold text-slate-500 uppercase">Phase Name</label><input type="text" value={phaseFormData.name} onChange={e => setPhaseFormData({...phaseFormData, name: e.target.value})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" placeholder="e.g. Foundation" /></div>
                <div className="grid grid-cols-3 gap-2">
                 <div><label className="text-xs font-bold text-slate-500 uppercase">Duration</label><input type="number" value={phaseFormData.duration_days} onChange={e => setPhaseFormData({...phaseFormData, duration_days: Number(e.target.value)})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" /></div>
                 <div><label className="text-xs font-bold text-slate-500 uppercase">Start Day</label><input type="number" value={phaseFormData.start_day} onChange={e => setPhaseFormData({...phaseFormData, start_day: Number(e.target.value)})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" /></div>
                 <div><label className="text-xs font-bold text-slate-500 uppercase">End Day</label><input type="number" value={phaseFormData.end_day} onChange={e => setPhaseFormData({...phaseFormData, end_day: Number(e.target.value)})} className="w-full border border-slate-200 bg-slate-50 rounded-lg px-3 py-2 text-sm mt-1" /></div>
                </div>
               </div>
               <div className="flex items-end justify-end gap-2 pb-1">
                <button onClick={cancelPhaseEdit} className="p-2 text-slate-500 hover:bg-slate-100 rounded-lg"><X size={18}/></button>
                <button onClick={savePhase} disabled={isSavingPlan} className="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-bold disabled:opacity-50 flex items-center gap-2"><Save size={16}/> Save</button>
               </div>
              </div>
            </div>
           )}

           {isAdmin && !isAddingPhase && (
            <button onClick={startAddingPhase} className="w-full py-4 border-2 border-dashed border-slate-300 rounded-xl text-slate-500 font-bold hover:border-indigo-400 hover:text-indigo-600 hover:bg-indigo-50/50 transition-colors flex items-center justify-center gap-2 mt-4">
             <Plus size={20} /> Add New Phase
            </button>
           )}
          </div>
         </div>

         {/* Critical Path & Notes */}
         <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-8">
          <div className="bg-indigo-50 p-6 rounded-2xl border border-indigo-100">
           <h4 className="font-bold text-indigo-900 mb-4">Critical Path</h4>
           <ol className="list-decimal list-inside text-sm text-indigo-800 space-y-2 font-medium">
            {workflow.constructionPlan.critical_path.map((cp: any) => <li key={cp}>{cp}</li>)}
           </ol>
          </div>

          <div className="space-y-6">
           {workflow.constructionPlan.optimization_notes.length > 0 && (
            <div className="bg-amber-50 p-6 rounded-2xl border border-amber-100">
             <h4 className="font-bold text-amber-900 mb-4">Optimization Notes</h4>
             <ul className="list-disc list-inside text-sm text-amber-800 space-y-2 font-medium">
              {workflow.constructionPlan.optimization_notes.map((note: any) => <li key={note}>{note}</li>)}
             </ul>
            </div>
           )}

           <div className="bg-slate-50 p-6 rounded-2xl border border-slate-200">
            <h4 className="font-bold text-slate-700 mb-4">AI Assumptions</h4>
            <ul className="list-disc list-inside text-sm text-slate-600 space-y-2 font-medium">
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
  );
};
"""

new_content = lines[:idx] + [new_return]

with open('src/pages/WorkflowReviewPage.tsx', 'w') as f:
    f.writelines(new_content)

print("Patched successfully")
