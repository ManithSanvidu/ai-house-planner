import re

path = "/Users/kushancs/Desktop/ai-house-system/HousePlanner-Web/src/pages/MyDesignsPage.tsx"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# Fix DesignCard action row
old_card_actions = r'<div className="flex flex-wrap gap-2"><Link to={`/dashboard/workflows/\$\{workflow\.workflowId\}\?design=\$\{design\.designId\}`} className="px-3 py-2 rounded-lg border text-xs font-semibold">Preview</Link><Link to={`/dashboard/construction/\$\{design\.designId\}/readiness`} className="px-3 py-2 rounded-lg bg-indigo-100 text-indigo-700 text-xs font-semibold hover:bg-indigo-200">Readiness Planner</Link>.*?</div>'
new_card_actions = """<div className="flex flex-wrap gap-2"><Link to={`/dashboard/workflows/${workflow.workflowId}?design=${design.designId}`} className="px-3 py-1.5 rounded-lg border text-xs font-semibold">View Design</Link><Link to={`/dashboard/construction/${design.designId}/readiness`} className="px-3 py-1.5 rounded-lg bg-indigo-100 text-indigo-700 text-xs font-semibold hover:bg-indigo-200">Readiness Planner</Link>{workflow.designs.length > 1 && !managementLocked && <button onClick={onCompare} disabled={!compared && compareFull} className={`px-3 py-1.5 rounded-lg border text-xs font-semibold disabled:opacity-40 ${compared ? 'bg-indigo-50 text-indigo-700' : ''}`}>{compared ? 'Remove Compare' : 'Compare'}</button>}{approved?<Link to={`/dashboard/construction?design=${design.designId}`} className="px-3 py-1.5 rounded-lg bg-indigo-600 text-text-primary text-xs font-semibold">Find Constructor</Link>:!managementLocked?<button onClick={onSubmit} className="px-3 py-1.5 rounded-lg bg-emerald-600 text-text-primary text-xs font-semibold hover:bg-emerald-700">Send to Architect</button>:null}{!managementLocked && <button onClick={onRemove} className="p-1.5 rounded-lg border text-red-600" aria-label={`Delete Version ${design.version}`}><Trash2 size={15}/></button>}</div>"""

content = re.sub(old_card_actions, new_card_actions, content)

# Fix CompareModal using `select`
# Find CompareModal declaration:
# const CompareModal = ({ comparison, onClose, onSelect }: { comparison: NonNullable<Comparison>; onClose: () => void; onSelect: (id: string) => void }) =>
content = content.replace(
    "const CompareModal = ({ comparison, onClose, onSelect }: { comparison: NonNullable<Comparison>; onClose: () => void; onSelect: (id: string) => void }) =>",
    "const CompareModal = ({ comparison, onClose }: { comparison: NonNullable<Comparison>; onClose: () => void }) =>"
)
# And the button: <button onClick={() => onSelect(design.designId)} className="w-full mt-5 py-2.5 rounded-xl bg-indigo-600 text-text-primary font-bold">Select Version {design.version}</button>
content = re.sub(r'<button onClick=\{.*?onSelect.*?Select Version.*?button>', '', content)

# Fix where CompareModal is called in MyDesignsPage
content = content.replace(
    "{comparison && <CompareModal comparison={comparison} onClose={() => setComparison(null)} onSelect={async designId => { await select(comparison.workflowId, designId); setComparison(null); }}/>}",
    "{comparison && <CompareModal comparison={comparison} onClose={() => setComparison(null)} />}"
)

# Also fix the import of CheckCircle2 which is no longer used
content = content.replace("CheckCircle2, ", "")

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

