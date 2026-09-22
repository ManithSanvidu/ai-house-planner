import sys

filepath = "HousePlanner-Web/src/routes/AppRoutes.tsx"
with open(filepath, 'r') as f:
    content = f.read()

target1 = "import ConstructorProjectWorkflow from '../pages/constructor/workflow/ConstructorProjectWorkflow';"
replacement1 = """import ConstructorProjectWorkflow from '../pages/constructor/workflow/ConstructorProjectWorkflow';
import ConstructorProjectDetails from '../pages/constructor/workflow/ConstructorProjectDetails';"""

target2 = """    <Route path="/constructor/workflows/:id" element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorProjectWorkflow /></PageContainer></ProtectedRoute>} />"""
replacement2 = """    <Route path="/constructor/workflows/:id" element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorProjectWorkflow /></PageContainer></ProtectedRoute>} />
    <Route path="/constructor/projects/:projectId" element={<ProtectedRoute allowedRoles={['Constructor']}><PageContainer><ConstructorProjectDetails /></PageContainer></ProtectedRoute>} />"""

if target1 in content and target2 in content:
    content = content.replace(target1, replacement1)
    content = content.replace(target2, replacement2)
    with open(filepath, 'w') as f:
        f.write(content)
    print("AppRoutes.tsx updated")
else:
    print("Targets not found!")
