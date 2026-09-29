import sys

with open('src/pages/WorkflowReviewPage.tsx', 'r') as f:
    content = f.read()

# Remove the old getRequirementValue and getAreaText
old_helpers = """  const getRequirementValue = (field: string, altField: string, fallback: any) => {
   const w = workflow as any;
   if (w?.input_data && w.input_data[field] !== undefined && w.input_data[field] !== null && w.input_data[field] !== '') return w.input_data[field];
   if (w?.design?.metadata && w.design.metadata[field] !== undefined && w.design.metadata[field] !== null && w.design.metadata[field] !== '') return w.design.metadata[field];
   if (w && w[altField] !== undefined && w[altField] !== null && w[altField] !== '') return w[altField];
   return fallback;
  };

  const getAreaText = (x: number, y: number) => {
   const ns = y > 20 ? 'North' : 'South';
   const ew = x > 20 ? 'East' : 'West';
   return `${ns}-${ew} area`;
  };"""

content = content.replace(old_helpers, "")

# Add the interface and mapper outside the component, or inside it. The user said: 
# "Do not create global functions. Keep it inside the component scope."
# But interfaces can be global.

mapper_code = """
  interface HomeRequirements {
   landSizeCategory?: string;
   landSizePerches?: number;
   bedrooms?: number;
   bathrooms?: number;
   houseType?: string;
   floors?: number;
  }

  const mapWorkflowRequirements = (w: any): HomeRequirements => {
   if (!w) return {};
   
   // Safely look in all possible locations the backend might be sending it
   const input = w.inputData || w.input_data || w.requirements || w.houseRequirement || w.landSubmission || {};
   const metadata = w.design?.metadata || w.design?.layoutJson?.metadata || {};
   
   const resolve = (...paths: any[]) => paths.find(v => v !== undefined && v !== null && v !== '');

   return {
    landSizeCategory: resolve(input.landSizeCategory, input.land_size_category, metadata.landSizeCategory, w.landSizeCategory),
    landSizePerches: resolve(input.landSizePerches, input.land_size_perches, metadata.landSizePerches, w.landSizePerches),
    bedrooms: resolve(input.bedrooms, metadata.bedrooms, w.bedrooms),
    bathrooms: resolve(input.bathrooms, metadata.bathrooms, w.bathrooms),
    houseType: resolve(input.houseType, input.house_type, input.architecturalStyle, input.architectural_style, metadata.houseType, w.houseType),
    floors: resolve(input.floors, metadata.floors, w.design?.floorCount)
   };
  };

  const requirements = mapWorkflowRequirements(workflow);

  return ("""

content = content.replace("  return (", mapper_code, 1)

# Fix Section B
section_b_old = """        {/* Section B: Your Request */}
        <section className="bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200 h-fit">
         <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <span className="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-500">📋</span>
          Your Home Requirements
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

section_b_new = """        {/* Section B: Your Request */}
        <section className="bg-white rounded-3xl p-6 md:p-8 shadow-sm border border-slate-200 h-fit">
         <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <span className="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-500">📋</span>
          Your Home Requirements
         </h2>
         <div className="space-y-4 text-sm">
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Land Size</span>
           <span className="font-bold">{requirements.landSizePerches ? `${requirements.landSizePerches} perches${requirements.landSizeCategory ? ` (${requirements.landSizeCategory} Plot)` : ''}` : 'Data unavailable'}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bedrooms</span>
           <span className="font-bold">{requirements.bedrooms ?? 'Data unavailable'}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bathrooms</span>
           <span className="font-bold">{requirements.bathrooms ?? 'Data unavailable'}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">House Type</span>
           <span className="font-bold capitalize">{requirements.houseType ?? 'Data unavailable'}</span>
          </div>
          <div className="flex justify-between items-center py-3">
           <span className="text-slate-500 font-medium">Floors</span>
           <span className="font-bold">{requirements.floors ?? 'Data unavailable'}</span>
          </div>
         </div>
        </section>"""

content = content.replace(section_b_old, section_b_new)

# Fix Section C
section_c_old = """        {/* Section C: Generated Design */}
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
             <span className="text-sm text-slate-600">{r.width}ft × {r.length}ft</span>
             <span className="text-xs text-slate-500">{getAreaText(r.x, r.y)}</span>
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
             <span className="text-sm text-slate-600">Size: {r.width} ft × {r.length} ft</span>
             <span className="text-xs text-slate-400 font-mono">Coordinates: X: {r.x} Y: {r.y}</span>
            </li>
           ))}
          </ul>
         </div>
        </section>"""

content = content.replace(section_c_old, section_c_new)

with open('src/pages/WorkflowReviewPage.tsx', 'w') as f:
    f.write(content)

print("Replaced successfully")
