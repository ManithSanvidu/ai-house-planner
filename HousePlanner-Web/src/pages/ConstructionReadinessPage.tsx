import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import apiClient from '../services/apiClient';
import { customerConstructionService } from '../services/customerConstructionService';
import { workflowService } from '../services/workflowService';
import { constructorWorkflowService } from '../services/constructorWorkflowService';
import useAuth from '../features/auth/useAuth';
import { motion, AnimatePresence } from 'framer-motion';
import { Package, List, AlertTriangle, Zap, CheckCircle, BarChart3, Truck } from 'lucide-react';

interface Material {
  id: string;
  materialName: string;
  requiredQuantity: number;
  availableQuantity: number;
  orderedQuantity: number;
  unit: string;
  status: string;
}

interface ProcurementItem {
  item: string;
  action: string;
  urgency: string;
  deadline: string;
  message?: string;
}

interface ProcurementPriority {
  item: string;
  priority: string;
  status: string;
}

interface RiskItem {
  issue: string;
  chain: string[];
  impact: string;
}

export const ConstructionReadinessPage: React.FC = () => {
  const { projectId: urlProjectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [projectId, setProjectId] = useState<string | null>(urlProjectId || null);
  const [availableProjects, setAvailableProjects] = useState<{ id: string, name: string }[]>([]);
  const [materials, setMaterials] = useState<Material[]>([]);
  const [readinessPlan, setReadinessPlan] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [activeTab, setActiveTab] = useState('overview');
  const [error, setError] = useState<string | null>(null);

  const [newMaterial, setNewMaterial] = useState({
    materialName: '',
    requiredQuantity: 0,
    availableQuantity: 0,
    orderedQuantity: 0,
    unit: '',
    supplier: '',
  });

  useEffect(() => {
    const controller = new AbortController();
    const initializeProject = async () => {
      try {
        const projectsList: { id: string, name: string }[] = [];

        await Promise.allSettled([
          (async () => {
            const overview = await customerConstructionService.overview();
            if (overview.activeProjects) {
              overview.activeProjects.forEach((p: any) => {
                projectsList.push({ id: p.id, name: `Project ${p.id.substring(0, 8)}` });
              });
            }
          })(),
          (async () => {
            const designs = await workflowService.getMyDesigns({ signal: controller.signal });
            designs.forEach((w: any) => {
              w.designs.forEach((d: any) => {
                projectsList.push({ id: d.designId, name: `Project ${d.designId.substring(0, 8)}` });
              });
            });
          })(),
          (async () => {
            const cProjects = await constructorWorkflowService.getProjects();
            cProjects.forEach((p: any) => {
              if (!projectsList.some(x => x.id === p.id)) {
                projectsList.push({ id: p.id, name: `Project ${p.id.substring(0, 8)}` });
              }
            });
          })()
        ]);

        setAvailableProjects(projectsList);
        setError(null);

        if (!urlProjectId) {
          if (projectsList.length > 0) {
            setProjectId(projectsList[0].id);
          } else {
            setError("No active construction projects or designs found.");
          }
        }
      } catch (e: any) {
        if (e.name !== 'CanceledError' && e.name !== 'AbortError') {
          setError("Failed to load projects.");
        }
      }
    };
    initializeProject();
    return () => { controller.abort(); };
  }, [urlProjectId]);

  const fetchMaterials = async () => {
    if (!projectId) return;
    try {
      const res = await apiClient.get(`/readiness/${projectId}/materials`);
      setMaterials(res.data);
    } catch (e) {
      console.error(e);
    }
  };

  const generatePlan = async () => {
    if (isGenerating) return;
    if (!projectId) return;
    setIsGenerating(true);
    setLoading(true);
    try {
      const res = await apiClient.post(`/readiness/${projectId}/plan`);
      setReadinessPlan(res.data);
      setActiveTab('optimizer');
    } catch (e: any) {
      console.error(e);
      let errorMessage = e.message || "Failed to generate plan. Please try again.";
      if (e.response?.data) {
        errorMessage = typeof e.response.data === 'object' ? JSON.stringify(e.response.data) : e.response.data;
      }
      alert(`Error: ${errorMessage}`);
    } finally {
      setIsGenerating(false);
      setLoading(false);
    }
  };

  const addMaterial = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!projectId) return;
    try {
      await apiClient.post(`/readiness/${projectId}/materials`, newMaterial);
      fetchMaterials();
      setNewMaterial({ materialName: '', requiredQuantity: 0, availableQuantity: 0, orderedQuantity: 0, unit: '', supplier: '' });
    } catch (e: any) {
      console.error(e);
      alert("Failed to add material: " + (e.response?.data?.message || e.response?.data || e.message || "Unknown error"));
    }
  };

  const autoGenerateMaterials = async () => {
    if (!projectId) return;
    setLoading(true);
    try {
      await apiClient.post(`/readiness/${projectId}/materials/generate`);
      fetchMaterials();
    } catch (e) {
      console.error(e);
      alert("Failed to auto-generate materials.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (projectId) {
      fetchMaterials();
    }
  }, [projectId, user?.role]);

  if (error) {
    return (
      <div className="p-4 md:p-6 max-w-7xl mx-auto text-center py-4 md:py-20">
        <h2 className="text-xl font-bold text-text-secondary">{error}</h2>
        <button onClick={() => navigate('/dashboard')} className="mt-4 text-blue-600 hover:underline">Return to Dashboard</button>
      </div>
    );
  }

  const readinessPercent = readinessPlan?.final_recommendation?.status === 'Ready' ? 100 : (readinessPlan ? 68 : 0);

  return (
    <div className="min-h-[calc(100vh-65px)] bg-background transition-colors duration-300 p-4 sm:p-8">
      <div className="max-w-[1400px] mx-auto space-y-8">
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex flex-col lg:flex-row lg:items-end justify-between gap-4 md:gap-6 bg-surface p-4 md:p-8 rounded-3xl border border-border dark:border-border-strong shadow-sm transition-colors"
        >
          <div>
            <h1 className="text-2xl md:text-3xl font-bold text-text-primary tracking-tight mb-2 transition-colors">
              Construction Readiness
            </h1>
            <p className="text-text-secondary transition-colors font-medium">AI-powered material & task planning</p>
          </div>
          {availableProjects.length > 0 && (
            <div className="w-full lg:w-96">
              <label className="block text-xs font-bold text-text-secondary mb-2 transition-colors uppercase tracking-wider">Select Project/Design</label>
              <select
                value={projectId || ''}
                onChange={(e) => { setProjectId(e.target.value); setReadinessPlan(null); }}
                className="block w-full py-3.5 px-4 border border-border dark:border-border-strong bg-surface-elevated text-text-primary rounded-xl shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors sm:text-sm font-semibold"
              >
                {availableProjects.map(p => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </div>
          )}
        </motion.div>

        <div className="bg-surface rounded-3xl shadow-sm border border-border dark:border-border-strong overflow-hidden mb-6 transition-colors">
          <div className="flex flex-wrap p-3 gap-2 border-b border-border dark:border-border-strong bg-surface-elevated transition-colors">
            {[
              { id: 'overview', icon: <BarChart3 size={18} />, label: 'Overview' },
              { id: 'materials', icon: <Package size={18} />, label: 'Materials' },
              { id: 'procurement', icon: <Truck size={18} />, label: 'Procurement' },
              { id: 'optimizer', icon: <Zap size={18} />, label: 'Optimizer' }
            ].map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 px-4 md:px-6 py-3 font-bold text-sm rounded-xl transition-all ${activeTab === tab.id
                  ? 'bg-surface shadow-sm text-blue-600 dark:text-blue-400 border border-border-strong'
                  : 'text-text-secondary hover:text-text-primary dark:hover:text-text-primary hover:bg-gray-100 dark:hover:bg-gray-800 border border-transparent'
                  }`}
              >
                {tab.icon}
                {tab.label}
              </button>
            ))}
          </div>

          <div className="p-4 md:p-6 md:p-10">
            <AnimatePresence mode="wait">
              {activeTab === 'overview' && (
                <motion.div key="overview" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }} className="space-y-8">
                  <div className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-900/10 dark:to-indigo-900/10 border border-blue-100 dark:border-blue-900/30 rounded-3xl p-4 md:p-8 md:p-10 flex flex-col md:flex-row items-center justify-between gap-4 md:gap-8 transition-colors shadow-sm">
                    <div className="w-full">
                      <h3 className="text-xl font-bold text-blue-950 dark:text-blue-100 mb-2">Project Readiness</h3>
                      <p className="text-blue-800/70 dark:text-blue-300/70 mb-6 font-medium">Overall readiness based on materials, schedule, and risks.</p>
                      <div className="w-full max-w-lg h-3.5 bg-blue-200/50 dark:bg-blue-900/40 rounded-full overflow-hidden shadow-inner">
                        <motion.div
                          initial={{ width: 0 }}
                          animate={{ width: `${readinessPercent}%` }}
                          transition={{ duration: 1, ease: "easeOut" }}
                          className="h-full bg-blue-600 dark:bg-blue-500 rounded-full"
                        ></motion.div>
                      </div>
                    </div>
                    <div className="text-2xl md:text-6xl font-black text-blue-600 dark:text-blue-400 drop-shadow-sm">{readinessPercent}%</div>
                  </div>

                  {readinessPlan?.risks?.length > 0 && (
                    <div className="bg-red-50 dark:bg-red-900/10 border border-red-100 dark:border-red-900/30 rounded-3xl p-4 md:p-8 transition-colors shadow-sm">
                      <h3 className="text-lg font-bold text-red-800 dark:text-red-400 flex items-center gap-2 mb-5">
                        <AlertTriangle size={20} /> Urgent Risks
                      </h3>
                      <div className="space-y-6">
                        {readinessPlan.risks.map((risk: any, i: number) => (
                          <div key={i} className="flex flex-col">
                            <span className="font-bold text-red-800 dark:text-red-300">{risk.issue}</span>
                            {risk.chain && risk.chain.length > 0 && (
                              <div className="ml-4 mt-2 text-sm text-red-700/80 dark:text-red-400/80 font-medium flex flex-col gap-1">
                                {risk.chain.map((step: string, j: number) => (
                                  <div key={j} className="flex flex-col">
                                    <span className="flex items-center gap-2">
                                      <span className="text-red-500/50">↓</span> {step}
                                    </span>
                                  </div>
                                ))}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  <div className="flex justify-end pt-4">
                    <motion.button
                      whileHover={{ scale: 1.02 }}
                      whileTap={{ scale: 0.98 }}
                      onClick={generatePlan}
                      disabled={loading}
                      className="px-4 md:px-8 py-4 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white font-bold rounded-xl shadow-md hover:shadow-lg transition-all disabled:opacity-50 flex items-center gap-3 text-sm tracking-wide"
                    >
                      <Zap size={18} />
                      {loading ? 'Analyzing Project Data...' : 'Generate Acceleration Plan'}
                    </motion.button>
                  </div>
                </motion.div>
              )}

              {activeTab === 'materials' && (
                <motion.div key="materials" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }} className="space-y-8">
                  <div>
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
                      <div className="flex items-center gap-3">
                        <div className="w-12 h-12 bg-indigo-50 dark:bg-indigo-900/20 rounded-2xl flex items-center justify-center text-indigo-600 dark:text-indigo-400 shadow-sm">
                          <Package size={24} />
                        </div>
                        <h3 className="text-2xl font-bold text-text-primary">Materials Inventory</h3>
                      </div>

                      {user?.role === 'Constructor' && (
                        <motion.button
                          whileHover={{ scale: 1.02 }}
                          whileTap={{ scale: 0.98 }}
                          onClick={autoGenerateMaterials}
                          disabled={loading}
                          className="px-5 py-2.5 bg-gradient-to-r from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 text-white font-bold rounded-xl shadow-sm hover:shadow-md transition-all disabled:opacity-50 flex items-center gap-2 text-sm"
                        >
                          <Zap size={16} />
                          {loading ? <span key="btn-gen-materials-loading">Generating...</span> : <span key="btn-gen-materials">AI Generate Materials</span>}
                        </motion.button>
                      )}
                    </div>

                    <div className="overflow-hidden border border-border dark:border-border-strong rounded-3xl shadow-sm">
                      <div className="overflow-x-auto">
                        <table className="min-w-full divide-y divide-border dark:divide-border-strong text-left">
                          <thead className="bg-surface-elevated">
                            <tr>
                              <th className="px-4 md:px-6 py-5 text-xs font-bold text-text-secondary uppercase tracking-wider">Material</th>
                              <th className="px-4 md:px-6 py-5 text-xs font-bold text-text-secondary uppercase tracking-wider">Required</th>
                              <th className="px-4 md:px-6 py-5 text-xs font-bold text-text-secondary uppercase tracking-wider">Available</th>
                              <th className="px-4 md:px-6 py-5 text-xs font-bold text-text-secondary uppercase tracking-wider">Ordered</th>
                              <th className="px-4 md:px-6 py-5 text-xs font-bold text-text-secondary uppercase tracking-wider">Status</th>
                            </tr>
                          </thead>
                          <tbody className="bg-surface divide-y divide-border dark:divide-border-strong">
                            {materials.map(m => {
                              const shortage = m.requiredQuantity - m.availableQuantity - m.orderedQuantity;
                              return (
                                <tr key={m.id} className="hover:bg-surface-elevated dark:hover:bg-gray-800/50 transition-colors">
                                  <td className="px-4 md:px-6 py-5 whitespace-nowrap text-sm font-bold text-text-primary">{m.materialName}</td>
                                  <td className="px-4 md:px-6 py-5 whitespace-nowrap text-sm font-semibold text-text-secondary">{m.requiredQuantity} {m.unit}</td>
                                  <td className="px-4 md:px-6 py-5 whitespace-nowrap text-sm font-semibold text-text-secondary">{m.availableQuantity} {m.unit}</td>
                                  <td className="px-4 md:px-6 py-5 whitespace-nowrap text-sm font-semibold text-text-secondary">{m.orderedQuantity || 0} {m.unit}</td>
                                  <td className="px-4 md:px-6 py-5 whitespace-nowrap text-sm">
                                    {shortage > 0
                                      ? <span className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-400 rounded-full border border-red-100 dark:border-red-900/30 shadow-sm"><AlertTriangle size={12} /> Shortage</span>
                                      : <span className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold bg-emerald-50 dark:bg-emerald-900/20 text-emerald-700 dark:text-emerald-400 rounded-full border border-emerald-100 dark:border-emerald-900/30 shadow-sm"><CheckCircle size={12} /> Ready</span>}
                                  </td>
                                </tr>
                              );
                            })}
                            {materials.length === 0 && (
                              <tr>
                                <td colSpan={5} className="px-4 md:px-6 py-4 md:py-12 text-center text-text-secondary text-sm font-semibold border-t border-border dark:border-border-strong">No materials found for this project.</td>
                              </tr>
                            )}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  </div>

                  {user?.role === 'Constructor' && (
                    <div className="bg-surface-elevated p-4 md:p-8 rounded-3xl border border-border dark:border-border-strong mt-8 transition-colors shadow-sm">
                      <h4 className="text-lg font-bold text-text-primary mb-6">Add Material</h4>
                      <form onSubmit={addMaterial} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-6 gap-5">
                        <input type="text" placeholder="Material Name" required value={newMaterial.materialName} onChange={e => setNewMaterial({ ...newMaterial, materialName: e.target.value })} className="lg:col-span-2 border border-border dark:border-border-strong bg-surface p-3.5 rounded-xl focus:ring-2 focus:ring-blue-500 text-text-primary transition-colors text-sm font-semibold shadow-sm" />
                        <input type="number" placeholder="Required" required value={newMaterial.requiredQuantity || ''} onChange={e => setNewMaterial({ ...newMaterial, requiredQuantity: Number(e.target.value) })} className="border border-border dark:border-border-strong bg-surface p-3.5 rounded-xl focus:ring-2 focus:ring-blue-500 text-text-primary transition-colors text-sm font-semibold shadow-sm" />
                        <input type="number" placeholder="Available" required value={newMaterial.availableQuantity || ''} onChange={e => setNewMaterial({ ...newMaterial, availableQuantity: Number(e.target.value) })} className="border border-border dark:border-border-strong bg-surface p-3.5 rounded-xl focus:ring-2 focus:ring-blue-500 text-text-primary transition-colors text-sm font-semibold shadow-sm" />
                        <input type="number" placeholder="Ordered" value={newMaterial.orderedQuantity || ''} onChange={e => setNewMaterial({ ...newMaterial, orderedQuantity: Number(e.target.value) })} className="border border-border dark:border-border-strong bg-surface p-3.5 rounded-xl focus:ring-2 focus:ring-blue-500 text-text-primary transition-colors text-sm font-semibold shadow-sm" />
                        <div className="flex gap-3 lg:col-span-1">
                          <input type="text" placeholder="Unit" required value={newMaterial.unit} onChange={e => setNewMaterial({ ...newMaterial, unit: e.target.value })} className="w-1/2 border border-border dark:border-border-strong bg-surface p-3.5 rounded-xl focus:ring-2 focus:ring-blue-500 text-text-primary transition-colors text-sm font-semibold shadow-sm" />
                          <motion.button whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }} type="submit" className="w-1/2 bg-gray-900 dark:bg-white text-white dark:text-text-primary rounded-xl font-bold shadow-sm hover:opacity-90 transition-colors">Add</motion.button>
                        </div>
                      </form>
                    </div>
                  )}
                </motion.div>
              )}

              {activeTab === 'procurement' && (
                <motion.div key="procurement" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }} className="space-y-10">
                  {/* Procurement Priorities */}
                  <div className="space-y-6">
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 bg-blue-50 dark:bg-blue-900/20 rounded-2xl flex items-center justify-center text-blue-600 dark:text-blue-400 shadow-sm">
                        <List size={24} />
                      </div>
                      <h3 className="text-2xl font-bold text-text-primary">Procurement Priority</h3>
                    </div>
                    {readinessPlan?.procurement_priorities ? (
                      <div className="bg-surface border border-border dark:border-border-strong rounded-3xl shadow-sm overflow-hidden transition-colors">
                        <table className="min-w-full divide-y divide-border dark:divide-border-strong text-left">
                          <tbody className="divide-y divide-border dark:divide-border-strong bg-surface">
                            {readinessPlan.procurement_priorities.map((item: ProcurementPriority, idx: number) => (
                              <tr key={idx} className="hover:bg-surface-elevated/50 dark:hover:bg-surface-elevated transition-colors">
                                <td className="px-4 md:px-6 py-4 whitespace-nowrap">
                                  <div className="flex items-center gap-3">
                                    {item.priority === 'Critical' && <span className="text-red-500">🔴</span>}
                                    {item.priority === 'Upcoming' && <span className="text-orange-500">🟠</span>}
                                    {item.priority === 'Ready' && <span className="text-emerald-500">🟢</span>}
                                    {item.priority === 'Later' && <span className="text-gray-400">⚪</span>}
                                    <span className="font-bold text-text-primary">{item.item}</span>
                                  </div>
                                </td>
                                <td className="px-4 md:px-6 py-4 whitespace-nowrap text-sm font-semibold text-text-secondary">{item.status}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    ) : (
                      <p className="text-text-secondary font-medium">Run the AI planner to generate procurement priorities.</p>
                    )}
                  </div>

                  {/* Procurement Actions */}
                  <div className="space-y-6">
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 bg-emerald-50 dark:bg-emerald-900/20 rounded-2xl flex items-center justify-center text-emerald-600 dark:text-emerald-400 shadow-sm">
                        <Truck size={24} />
                      </div>
                      <h3 className="text-2xl font-bold text-text-primary">Procurement Plan</h3>
                    </div>

                    {readinessPlan?.procurement_plan ? (
                      <div className="grid sm:grid-cols-2 gap-5">
                        {readinessPlan.procurement_plan.map((item: ProcurementItem, idx: number) => (
                          <div key={idx} className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 md:p-6 bg-surface border border-border dark:border-border-strong rounded-3xl shadow-sm transition-colors hover:border-emerald-200 dark:hover:border-emerald-900/50 group">
                            <div>
                              <h4 className="font-bold text-text-primary mb-1.5 text-lg group-hover:text-emerald-600 dark:group-hover:text-emerald-400 transition-colors">{item.item}</h4>
                              {item.message ? (
                                <p className="text-sm font-semibold text-red-600 dark:text-red-400">{item.message}</p>
                              ) : (
                                <p className="text-sm font-semibold text-text-secondary">{item.action}</p>
                              )}
                            </div>
                            <div className="flex sm:flex-col items-center sm:items-end justify-between sm:justify-center gap-2">
                              <span className={`px-3 py-1.5 rounded-full text-xs font-bold border shadow-sm ${item.urgency === 'Critical' ? 'bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-400 border-red-100 dark:border-red-900/30' : 'bg-orange-50 dark:bg-orange-900/20 text-orange-700 dark:text-orange-400 border-orange-100 dark:border-orange-900/30'}`}>
                                {item.urgency} Urgency
                              </span>
                              <span className="text-xs font-bold text-text-secondary mt-1">{item.deadline}</span>
                            </div>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div className="text-center py-4 md:py-20 bg-surface-elevated rounded-3xl border border-dashed border-border dark:border-border-strong transition-colors">
                        <List className="mx-auto text-gray-400 dark:text-gray-600 mb-5" size={40} />
                        <p className="text-text-secondary font-semibold mb-6">No procurement plan generated yet.</p>
                        <div className="flex flex-col items-center">
                          <motion.button disabled={isGenerating} whileHover={!isGenerating ? { scale: 1.02 } : {}} whileTap={!isGenerating ? { scale: 0.98 } : {}} onClick={generatePlan} className={`px-4 md:px-8 py-3.5 bg-surface border border-border dark:border-border-strong text-text-primary font-bold rounded-xl shadow-sm transition-colors text-sm tracking-wide ${isGenerating ? 'opacity-50 cursor-not-allowed' : ''}`}>
                            {isGenerating ? <span key="btn-gen-plan-loading">Generating...</span> : <span key="btn-gen-plan">Generate Construction Plan</span>}
                          </motion.button>
                          <p className="text-xs text-text-secondary mt-2">AI construction planning uses resources. Generate only when required.</p>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Risk Agent */}
                  <div className="space-y-6">
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 bg-red-50 dark:bg-red-900/20 rounded-2xl flex items-center justify-center text-red-600 dark:text-red-400 shadow-sm">
                        <AlertTriangle size={24} />
                      </div>
                      <h3 className="text-2xl font-bold text-text-primary">Risk Agent Analysis</h3>
                    </div>
                    {readinessPlan?.risks && readinessPlan.risks.length > 0 ? (
                      <div className="grid sm:grid-cols-2 gap-5">
                        {readinessPlan.risks.map((risk: RiskItem, idx: number) => (
                          <div key={idx} className="bg-red-50/50 dark:bg-red-900/10 p-4 md:p-6 rounded-3xl border border-red-100 dark:border-red-900/30 shadow-sm transition-colors">
                            <h4 className="font-bold text-red-900 dark:text-red-300 mb-4">{risk.issue}</h4>
                            <div className="flex flex-col gap-2 relative pl-4 border-l-2 border-red-200 dark:border-red-900/50 ml-2">
                              {risk.chain.map((step, i) => (
                                <div key={i} className="relative">
                                  <div className="absolute -left-[21px] top-1.5 w-2 h-2 rounded-full bg-red-400 dark:bg-red-500"></div>
                                  <p className="text-sm font-semibold text-red-800 dark:text-red-200">{step}</p>
                                </div>
                              ))}
                            </div>
                          </div>
                        ))}
                      </div>
                    ) : readinessPlan ? (
                      <div className="p-4 md:p-6 bg-emerald-50 dark:bg-emerald-900/20 text-emerald-800 dark:text-emerald-300 font-bold rounded-2xl border border-emerald-100 dark:border-emerald-900/30">
                        <CheckCircle className="inline mr-2" size={20} />
                        No immediate schedule risks identified.
                      </div>
                    ) : null}
                  </div>
                </motion.div>
              )}

              {activeTab === 'optimizer' && (
                <motion.div key="optimizer" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }} className="space-y-6">
                  <div className="flex items-center gap-3 mb-6">
                    <div className="w-12 h-12 bg-purple-50 dark:bg-purple-900/20 rounded-2xl flex items-center justify-center text-purple-600 dark:text-purple-400 shadow-sm">
                      <Zap size={24} />
                    </div>
                    <h3 className="text-2xl font-bold text-text-primary">AI Acceleration Plan</h3>
                  </div>

                  {readinessPlan?.acceleration_opportunities ? (
                    <div className="bg-gradient-to-br from-purple-50 to-indigo-50 dark:from-purple-950/40 dark:to-indigo-950/40 p-4 md:p-8 md:p-10 rounded-3xl border border-purple-100 dark:border-purple-900/30 transition-colors shadow-sm">
                      {typeof readinessPlan.acceleration_opportunities === 'object' && !Array.isArray(readinessPlan.acceleration_opportunities) && (
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 mb-8 bg-white/50 dark:bg-surface/50 p-4 md:p-6 rounded-2xl border border-purple-100/50 dark:border-purple-900/30">
                          <div>
                            <div className="text-sm font-bold text-purple-700/80 dark:text-purple-400/80 mb-1">Current estimated completion</div>
                            <div className="text-2xl font-black text-text-primary dark:text-white">{readinessPlan.acceleration_opportunities.current_estimated_completion}</div>
                          </div>
                          <div>
                            <div className="text-sm font-bold text-purple-700/80 dark:text-purple-400/80 mb-1">Potential optimized completion</div>
                            <div className="text-2xl font-black text-text-primary dark:text-white">{readinessPlan.acceleration_opportunities.potential_optimized_completion}</div>
                          </div>
                          <div>
                            <div className="text-sm font-bold text-emerald-700/80 dark:text-emerald-400/80 mb-1">Potential schedule improvement</div>
                            <div className="text-2xl font-black text-emerald-600 dark:text-emerald-400">{readinessPlan.acceleration_opportunities.potential_schedule_improvement}</div>
                          </div>
                        </div>
                      )}

                      <h4 className="text-purple-900 dark:text-purple-300 font-bold mb-6 text-xl">Recommended Actions</h4>
                      <ul className="space-y-4">
                        {(Array.isArray(readinessPlan.acceleration_opportunities)
                          ? readinessPlan.acceleration_opportunities
                          : (readinessPlan.acceleration_opportunities.actions || [])
                        ).map((opp: string, idx: number) => (
                          <li key={idx} className="flex gap-4 text-purple-950 dark:text-purple-100 bg-white/70 dark:bg-surface/70 p-5 rounded-xl shadow-sm border border-purple-100/50 dark:border-purple-900/30 backdrop-blur-md transition-colors font-medium">
                            <span className="flex-shrink-0 w-8 h-8 rounded-full bg-purple-200 dark:bg-purple-900/50 text-purple-800 dark:text-purple-300 flex items-center justify-center text-sm font-bold shadow-inner">{idx + 1}</span>
                            <div className="pt-1.5 leading-relaxed">{opp}</div>
                          </li>
                        ))}
                      </ul>
                      <p className="mt-8 text-sm text-purple-700/80 dark:text-purple-400/80 font-semibold italic flex items-center gap-2">
                        <Zap size={14} />
                        Note: These are AI-generated suggestions. Actual sequencing should be verified by the constructor.
                      </p>
                    </div>
                  ) : (
                    <div className="text-center py-4 md:py-20 bg-surface-elevated rounded-3xl border border-dashed border-border dark:border-border-strong transition-colors">
                      <Zap className="mx-auto text-gray-400 dark:text-gray-600 mb-5" size={40} />
                      <p className="text-text-secondary font-semibold mb-6">Run the AI optimizer to find acceleration opportunities.</p>
                      <motion.button
                        whileHover={{ scale: 1.02 }}
                        whileTap={{ scale: 0.98 }}
                        onClick={generatePlan}
                        disabled={loading}
                        className="px-4 md:px-8 py-3.5 bg-surface border border-border dark:border-border-strong text-text-primary font-bold rounded-xl shadow-sm transition-colors disabled:opacity-50 text-sm tracking-wide"
                      >
                        {loading ? 'Analyzing...' : 'Generate Acceleration Plan'}
                      </motion.button>
                    </div>
                  )}
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ConstructionReadinessPage;
