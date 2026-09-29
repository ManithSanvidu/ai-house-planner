import sys

with open('src/pages/WorkflowReviewPage.tsx', 'r') as f:
    content = f.read()

target_mapper = """  interface HomeRequirements {
   landSizeCategory?: string;
   landSizePerches?: number;
   bedrooms?: number;
   bathrooms?: number;
   houseType?: string;
   floors?: number;
  }

  const mapWorkflowRequirements = (w: any): HomeRequirements => {
   if (!w) return {};
   
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

  const requirements = mapWorkflowRequirements(workflow);"""

replacement_mapper = """  interface WorkflowRequirements {
   landSizeCategory: string;
   landSizePerches: number;
   bedrooms: number;
   bathrooms: number;
   houseType: string;
   floors: number;
  }"""

if target_mapper in content:
    content = content.replace(target_mapper, replacement_mapper)
else:
    print("Mapper target not found!")
    sys.exit(1)

target_jsx = """          <div className="flex justify-between items-center py-3 border-b border-slate-100">
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
          </div>"""

replacement_jsx = """          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Land Size</span>
           <span className="font-bold">
            {workflow.requirements?.landSizePerches 
             ? `${workflow.requirements.landSizePerches} perches${workflow.requirements.landSizeCategory ? ` (${workflow.requirements.landSizeCategory} Plot)` : ''}` 
             : 'Data unavailable'}
           </span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bedrooms</span>
           <span className="font-bold">{workflow.requirements?.bedrooms ?? 'Data unavailable'}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">Bathrooms</span>
           <span className="font-bold">{workflow.requirements?.bathrooms ?? 'Data unavailable'}</span>
          </div>
          <div className="flex justify-between items-center py-3 border-b border-slate-100">
           <span className="text-slate-500 font-medium">House Type</span>
           <span className="font-bold capitalize">{workflow.requirements?.houseType ?? 'Data unavailable'}</span>
          </div>
          <div className="flex justify-between items-center py-3">
           <span className="text-slate-500 font-medium">Floors</span>
           <span className="font-bold">{workflow.requirements?.floors ?? 'Data unavailable'}</span>
          </div>"""

if target_jsx in content:
    content = content.replace(target_jsx, replacement_jsx)
else:
    print("JSX target not found!")
    sys.exit(1)

with open('src/pages/WorkflowReviewPage.tsx', 'w') as f:
    f.write(content)

print("Replaced successfully")
