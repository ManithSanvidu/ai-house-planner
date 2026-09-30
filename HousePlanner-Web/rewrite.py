import re

path = "/Users/kushancs/Desktop/ai-house-system/HousePlanner-Web/src/pages/MyDesignsPage.tsx"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Remove select and unselect functions, modify submit function
content = re.sub(
    r" const select = async .*? await load\(\);\n };\n const unselect = async .*? await load\(\);\n };",
    "",
    content,
    flags=re.DOTALL
)

submit_old = """ const submit = async (workflowId: string) => {
  const project = projects.find(p => p.workflowId === workflowId);
  if (!project || !project.preferredHouseDesignId) return;
  await workflowService.submitArchitectReview(workflowId, project.preferredHouseDesignId); await load();
 };"""
submit_new = """ const submit = async (workflowId: string, designId: string) => {
  await workflowService.submitArchitectReview(workflowId, designId); await load();
 };"""
content = content.replace(submit_old, submit_new)

# 2. Remove "Generate Another" and the top-level "Send to Architect"
# Line 94 currently:
# <div className="flex flex-wrap gap-2">{approved?<span className="px-4 py-2 rounded-xl bg-emerald-100 text-emerald-800 font-bold">✓ Architect Approved</span>:submitted?<span className="px-4 py-2 rounded-xl bg-amber-100 text-amber-900 font-bold">Awaiting Architect Review</span>:<><Link to={`/dashboard/workflows/${project.workflowId}`} className="px-4 py-2 rounded-xl border bg-surface bg-background font-semibold text-sm">{rejected?'Generate New Design':'Generate Another'}</Link><button onClick={() => submit(project.workflowId)} disabled={!selected} className="px-4 py-2 rounded-xl bg-emerald-600 text-text-primary font-semibold text-sm disabled:opacity-40">Send Selected to Architect</button></>}</div>
new_buttons = """<div className="flex flex-wrap gap-2">{approved?<span className="px-4 py-1.5 rounded-xl bg-emerald-100 text-emerald-800 font-bold text-sm">✓ Architect Approved</span>:submitted?<span className="px-4 py-1.5 rounded-xl bg-amber-100 text-amber-900 font-bold text-sm">Awaiting Architect Review</span>:null}</div>"""
content = re.sub(
    r'<div className="flex flex-wrap gap-2">\{approved\?<span.*?\}</div>',
    new_buttons,
    content
)

# Remove selected logic in project loop
content = content.replace("const selected = project.designs.find(d => d.isPreferred);\n   ", "")
content = content.replace("<span>{selected ? `Selected: Version ${selected.version}` : 'No design selected'}</span>", "")

# 3. Reduce vertical spacing on project section
content = content.replace(
    'className="max-w-5xl mx-auto rounded-3xl border bg-surface-elevated/60 p-6 sm:p-8 mb-6 shadow-sm"',
    'className="max-w-5xl mx-auto rounded-2xl border bg-surface-elevated/60 p-4 sm:p-5 mb-5 shadow-sm"'
)

# Update map over designs to pass onSubmit
content = content.replace(
    'onCompare={() => toggleCompare(project.workflowId, design.designId)} onSelect={() => select(project.workflowId, design.designId)} onUnselect={() => unselect(project.workflowId)} onRemove={() => setPendingRemoval({ workflow: project, design })}/>',
    'onCompare={() => toggleCompare(project.workflowId, design.designId)} onSubmit={() => submit(project.workflowId, design.designId)} onRemove={() => setPendingRemoval({ workflow: project, design })}/>'
)

# 4. Redesign DesignCard
# Signature change
content = content.replace(
    'const DesignCard = ({ design, workflow, compared, compareFull, onCompare, onSelect, onUnselect, onRemove }: { design: DesignHistoryDto; workflow: WorkflowDesignHistoryDto; compared: boolean; compareFull: boolean; onCompare: () => void; onSelect: () => void; onUnselect: () => void; onRemove: () => void }) => {',
    'const DesignCard = ({ design, workflow, compared, compareFull, onCompare, onSubmit, onRemove }: { design: DesignHistoryDto; workflow: WorkflowDesignHistoryDto; compared: boolean; compareFull: boolean; onCompare: () => void; onSubmit: () => void; onRemove: () => void }) => {'
)

# Remove isPreferred ring and Selected label
content = content.replace(
    'className={`rounded-2xl bg-surface bg-background border p-4 ${design.isPreferred ? \'border-emerald-500 ring-1 ring-emerald-500\' : compared ? \'border-indigo-500 ring-1 ring-indigo-500\' : \'border-border\'}`}',
    'className={`rounded-xl bg-surface bg-background border p-3 ${compared ? \'border-indigo-500 ring-1 ring-indigo-500\' : \'border-border\'}`}'
)
content = re.sub(r'\{design\.isPreferred && <span.*?Selected</span>\}', '', content)

# Reduce dl margins
content = content.replace('my-3', 'my-2')
content = content.replace('mb-3', 'mb-2')

# Replace the actions div
actions_old = r'<div className="flex flex-wrap gap-2">.*?</div>'
actions_new = """<div className="flex flex-wrap gap-2"><Link to={`/dashboard/workflows/${workflow.workflowId}?design=${design.designId}`} className="px-3 py-1.5 rounded-lg border text-xs font-semibold">View Design</Link><Link to={`/dashboard/construction/${design.designId}/readiness`} className="px-3 py-1.5 rounded-lg bg-indigo-100 text-indigo-700 text-xs font-semibold hover:bg-indigo-200">Readiness Planner</Link>{workflow.designs.length > 1 && !managementLocked && <button onClick={onCompare} disabled={!compared && compareFull} className={`px-3 py-1.5 rounded-lg border text-xs font-semibold disabled:opacity-40 ${compared ? 'bg-indigo-50 text-indigo-700' : ''}`}>{compared ? 'Remove Compare' : 'Compare'}</button>}{approved?<Link to={`/dashboard/construction?design=${design.designId}`} className="px-3 py-1.5 rounded-lg bg-indigo-600 text-text-primary text-xs font-semibold">Find Constructor</Link>:!managementLocked?<button onClick={onSubmit} className="px-3 py-1.5 rounded-lg bg-emerald-600 text-text-primary text-xs font-semibold hover:bg-emerald-700">Send to Architect</button>:null}{!managementLocked && <button onClick={onRemove} className="p-1.5 rounded-lg border text-red-600" aria-label={`Delete Version ${design.version}`}><Trash2 size={15}/></button>}</div>"""
content = re.sub(actions_old, actions_new, content, count=1)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

