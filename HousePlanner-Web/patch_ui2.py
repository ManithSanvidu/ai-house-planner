import sys

with open('src/pages/WorkflowReviewPage.tsx', 'r') as f:
    content = f.read()

# 1. Add getRequirementValue helper before return
helper_code = """
  const getRequirementValue = (field: string, altField: string, fallback: any) => {
   const w = workflow as any;
   if (w?.input_data && w.input_data[field] !== undefined && w.input_data[field] !== null && w.input_data[field] !== '') return w.input_data[field];
   if (w?.design?.metadata && w.design.metadata[field] !== undefined && w.design.metadata[field] !== null && w.design.metadata[field] !== '') return w.design.metadata[field];
   if (w && w[altField] !== undefined && w[altField] !== null && w[altField] !== '') return w[altField];
   return fallback;
  };

  return (
"""
content = content.replace("  return (", helper_code, 1)

# 2. Fix Section B: Your Request
section_b_old = """        {/* Section B: Your Request */}
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
        </section>"""

section_b_new = """        {/* Section B: Your Request */}
        <section className="bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200 h-fit">
         <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <span className="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-500">📋</span>
          Your Request
         </h2>
         <div className="space-y-4 text-sm">
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Land Size</span>
           <span className="font-bold">{getRequirementValue('land_size_perches', 'landSizePerches', 'Not specified')} perches</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bedrooms</span>
           <span className="font-bold">{getRequirementValue('bedrooms', 'bedrooms', 'Not specified')}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bathrooms</span>
           <span className="font-bold">{getRequirementValue('bathrooms', 'bathrooms', 'Not specified')}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">House Type</span>
           <span className="font-bold capitalize">{getRequirementValue('architectural_style', 'houseType', 'Not specified')}</span>
          </div>
          <div className="flex justify-between items-center py-3">
           <span className="text-slate-500 font-medium">Floors</span>
           <span className="font-bold">{getRequirementValue('floors', 'floorCount', workflow.design?.floorCount || 'Not specified')}</span>
          </div>
         </div>
        </section>"""

content = content.replace(section_b_old, section_b_new)

# 3. Section C
section_c_old = """        {/* Section C: Generated Design */}
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
        </section>"""

section_c_new = """        {/* Section C: Generated Design */}
        <section className="bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200 h-fit">
         <h2 className="text-xl font-bold mb-1 flex items-center gap-2">
          <span className="w-8 h-8 rounded-full bg-indigo-50 flex items-center justify-center text-indigo-500">✨</span>
          Generated Floor Plan
         </h2>
         <p className="text-sm text-slate-500 mb-6">Created by deterministic spatial planning engine</p>
         <div className="bg-slate-50 rounded-2xl p-4 border border-slate-100 max-h-80 overflow-y-auto">
          <ul className="space-y-4">
           {workflow.design?.rooms.map(r => (
            <li key={r.roomId} className="flex flex-col gap-1 p-3 bg-white border border-slate-100 rounded-xl shadow-sm">
             <span className="font-bold text-slate-800">{r.name || formatRoomName(r.roomType)}</span>
             <span className="text-sm text-slate-600">{r.width}ft x {r.length}ft</span>
             <span className="text-xs text-slate-400 font-mono">Position X: {r.x} Y: {r.y}</span>
            </li>
           ))}
          </ul>
         </div>
        </section>"""

content = content.replace(section_c_old, section_c_new)

# 4. Visualization fallback
vis_old = """         ) : (
          <div className="text-zinc-500">Visualization failed to generate.</div>
         )}"""
vis_new = """         ) : (
          <div className="text-zinc-400 flex flex-col items-center gap-2 text-center p-6">
           <span className="text-lg font-bold text-zinc-300">AI visualization unavailable</span>
           <span className="text-sm">Your validated deterministic floor plan is available below.</span>
          </div>
         )}"""

content = content.replace(vis_old, vis_new)

# 5. Rename Section D
d_old = "Technical Blueprint (Advanced)"
d_new = "Architect / Developer View"
content = content.replace(d_old, d_new)

with open('src/pages/WorkflowReviewPage.tsx', 'w') as f:
    f.write(content)

print("Patched successfully")
