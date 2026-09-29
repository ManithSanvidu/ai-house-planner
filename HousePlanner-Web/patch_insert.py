import sys

with open('src/pages/WorkflowReviewPage.tsx', 'r') as f:
    content = f.read()

target = """  const rejected = workflow.architectReviewStatus === 'Rejected' || workflow.status === 'revision_requested';



  return ("""

replacement = """  const rejected = workflow.architectReviewStatus === 'Rejected' || workflow.status === 'revision_requested';

  console.log("WORKFLOW RESPONSE", workflow);

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

if target in content:
    content = content.replace(target, replacement)
    with open('src/pages/WorkflowReviewPage.tsx', 'w') as f:
        f.write(content)
    print("Patched successfully")
else:
    print("Target not found")
