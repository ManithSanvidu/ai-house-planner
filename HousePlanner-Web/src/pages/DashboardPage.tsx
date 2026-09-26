import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useNavigate } from 'react-router-dom';
import { Plus, Sparkles, Library, FolderKanban, Shield, UserCog, CheckCircle, HardHat, Clock, MessageSquare, ArrowRight, LayoutTemplate } from 'lucide-react';
import useAuth from '../features/auth/useAuth';
import { customerConstructionService, type ApprovedDesign, type CustomerConstruction } from '../services/customerConstructionService';

const DashboardPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [approvedDesigns, setApprovedDesigns] = useState<ApprovedDesign[]>([]);
  const [construction, setConstruction] = useState<CustomerConstruction | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Role-aware redirect
  useEffect(() => {
    if (!user) return;
    if (user.role === 'Architect') navigate('/architect/dashboard', { replace: true });
    else if (user.role === 'Constructor') navigate('/constructor/dashboard', { replace: true });
  }, [user, navigate]);

  useEffect(() => {
    if (user?.role === 'Customer') {
      setIsLoading(true);
      Promise.all([
        customerConstructionService.approvedDesigns(),
        customerConstructionService.overview()
      ]).then(([d, c]) => {
        setApprovedDesigns(d);
        setConstruction(c);
      }).catch(() => {
        // Handle error gracefully
      }).finally(() => {
        setIsLoading(false);
      });
    } else {
      setIsLoading(false);
    }
  }, [user]);

  if (isLoading) {
    return (
      <div className="min-h-[calc(100vh-65px)] flex items-center justify-center p-6 bg-[#fcfcfd] dark:bg-gray-950 transition-colors duration-300">
        <div className="w-8 h-8 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
      </div>
    );
  }

  // Admin View
  if (user?.role === 'Admin') {
    return (
      <div className="min-h-[calc(100vh-65px)] flex items-center justify-center p-6 bg-[#fcfcfd] dark:bg-gray-950 transition-colors duration-300">
        <motion.div 
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 p-10 rounded-[2rem] max-w-2xl w-full text-center shadow-sm transition-colors"
        >
          <div className="mx-auto w-16 h-16 bg-blue-50 dark:bg-blue-900/20 rounded-2xl flex items-center justify-center mb-6 transition-colors">
            <Shield className="text-blue-600 dark:text-blue-400" size={28} />
          </div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-3 tracking-tight transition-colors">Admin Dashboard</h1>
          <p className="text-gray-500 dark:text-gray-400 mb-10 transition-colors">Manage the validated house-plan library and staff accounts.</p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link to="/dashboard/admin/plans" className="flex-1">
              <motion.button whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }} className="w-full flex items-center justify-center gap-3 px-6 py-4 bg-gray-900 dark:bg-white text-white dark:text-gray-900 rounded-xl font-semibold shadow-sm transition-colors">
                <Library size={18} /><span>Manage Plans</span>
              </motion.button>
            </Link>
            <Link to="/dashboard/admin/staff" className="flex-1">
              <motion.button whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }} className="w-full flex items-center justify-center gap-3 px-6 py-4 border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-900 dark:text-white rounded-xl font-semibold transition-colors">
                <UserCog size={18} /><span>Manage Staff</span>
              </motion.button>
            </Link>
          </div>
        </motion.div>
      </div>
    );
  }

  // Customer Dashboard
  const activeProject = construction?.activeProjects?.[0];
  const pendingRequests = construction?.pendingRequests?.length || 0;
  const activeCount = construction?.activeProjects?.length || 0;
  const approvedCount = approvedDesigns.length;

  const getNextStep = () => {
    if (activeCount > 0) return {
      msg: "Construction is underway. View progress and daily updates.",
      btn: "View Construction",
      link: "/dashboard/construction",
      icon: <HardHat size={16} />
    };
    if (pendingRequests > 0) return {
      msg: "Your construction request is pending approval from the constructor.",
      btn: "Track Request",
      link: "/dashboard/construction",
      icon: <Clock size={16} />
    };
    if (approvedCount > 0) return {
      msg: "Your design is approved. You can now find a constructor.",
      btn: "Find Constructor",
      link: "/dashboard/construction",
      icon: <CheckCircle size={16} />
    };
    return {
      msg: "Start by creating your first home project.",
      btn: "Start New Project",
      link: "/dashboard/new-project",
      icon: <Plus size={16} />
    };
  };

  const nextStep = getNextStep();

  return (
    <div className="min-h-[calc(100vh-65px)] bg-[#fcfcfd] dark:bg-gray-950 transition-colors duration-300 p-4 sm:p-8">
      <div className="max-w-[1400px] mx-auto space-y-8">
        
        {/* HEADER */}
        <motion.div 
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex flex-col lg:flex-row lg:items-end justify-between gap-6 bg-white dark:bg-gray-900 p-8 rounded-3xl border border-gray-200 dark:border-gray-800 shadow-sm transition-colors"
        >
          <div>
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white tracking-tight mb-2 transition-colors">
              Welcome back, {user?.fullName?.split(' ')[0] || 'there'}
            </h1>
            <p className="text-gray-500 dark:text-gray-400 transition-colors">
              Here’s an overview of your home planning journey.
            </p>
          </div>
          
          <div className="flex flex-wrap gap-3">
            <button className="flex items-center gap-2 px-5 py-3 border border-gray-200 dark:border-gray-700 rounded-xl font-semibold text-xs tracking-wider text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
              <MessageSquare size={16} />
              ASK AI ARCHITECT
            </button>
            <Link to="/dashboard/plans" className="flex items-center gap-2 px-5 py-3 border border-gray-200 dark:border-gray-700 rounded-xl font-semibold text-xs tracking-wider text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
              <Library size={16} />
              BROWSE PLANS
            </Link>
            <Link to="/dashboard/new-project" className="flex items-center gap-2 px-5 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-xl font-semibold text-xs tracking-wider transition-colors shadow-sm shadow-blue-900/20">
              <Plus size={16} />
              START NEW PROJECT
            </Link>
          </div>
        </motion.div>

        {/* SUMMARY CARDS */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }} className="bg-white dark:bg-gray-900 p-6 rounded-2xl border border-gray-200 dark:border-gray-800 flex items-center gap-4 shadow-sm group transition-colors">
            <div className="w-12 h-12 bg-emerald-50 dark:bg-emerald-900/20 rounded-xl flex items-center justify-center text-emerald-600 dark:text-emerald-400 group-hover:scale-110 transition-transform">
              <CheckCircle size={24} />
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400 mb-1 transition-colors">Approved Designs</p>
              <h3 className="text-2xl font-bold text-gray-900 dark:text-white transition-colors">{approvedCount}</h3>
            </div>
          </motion.div>

          <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }} className="bg-white dark:bg-gray-900 p-6 rounded-2xl border border-gray-200 dark:border-gray-800 flex items-center gap-4 shadow-sm group transition-colors">
            <div className="w-12 h-12 bg-indigo-50 dark:bg-indigo-900/20 rounded-xl flex items-center justify-center text-indigo-600 dark:text-indigo-400 group-hover:scale-110 transition-transform">
              <HardHat size={24} />
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400 mb-1 transition-colors">Active Construction</p>
              <h3 className="text-2xl font-bold text-gray-900 dark:text-white transition-colors">{activeCount}</h3>
            </div>
          </motion.div>

          <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 }} className="bg-white dark:bg-gray-900 p-6 rounded-2xl border border-gray-200 dark:border-gray-800 flex items-center gap-4 shadow-sm group sm:col-span-2 lg:col-span-1 transition-colors">
            <div className="w-12 h-12 bg-amber-50 dark:bg-amber-900/20 rounded-xl flex items-center justify-center text-amber-600 dark:text-amber-400 group-hover:scale-110 transition-transform">
              <Clock size={24} />
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400 mb-1 transition-colors">Pending Requests</p>
              <h3 className="text-2xl font-bold text-gray-900 dark:text-white transition-colors">{pendingRequests}</h3>
            </div>
          </motion.div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* MAIN CONTENT AREA */}
          <div className="lg:col-span-2 space-y-8">
            
            {/* CURRENT PROJECT */}
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.4 }}>
              <h2 className="text-lg font-bold text-gray-900 dark:text-white mb-4 transition-colors">Current Project</h2>
              {activeProject ? (
                <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-3xl p-8 shadow-sm transition-colors">
                  <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 mb-8">
                    <div>
                      <div className="flex items-center gap-3 mb-2">
                        <h3 className="text-2xl font-bold text-gray-900 dark:text-white transition-colors">
                          {approvedDesigns.find(d => d.designId === activeProject.houseDesignId)?.title || `Design v${activeProject.designVersion}`}
                        </h3>
                        <span className="px-2.5 py-1 text-[10px] uppercase font-bold tracking-wider bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400 rounded-full transition-colors">
                          In Progress
                        </span>
                      </div>
                      <p className="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2 transition-colors">
                        <Shield size={14} className="text-emerald-500" /> Architect Approved
                      </p>
                    </div>
                    
                    <div className="flex flex-col sm:flex-row gap-3">
                      <Link to={`/dashboard/workflows/${approvedDesigns.find(d => d.designId === activeProject.houseDesignId)?.workflowId}?design=${activeProject.houseDesignId}`} className="px-5 py-2.5 border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-900 dark:text-white rounded-xl text-xs font-bold tracking-wider transition-colors text-center">
                        VIEW DESIGN
                      </Link>
                      <Link to={`/dashboard/construction`} className="px-5 py-2.5 bg-gray-900 dark:bg-white hover:bg-black dark:hover:bg-gray-200 text-white dark:text-gray-900 rounded-xl text-xs font-bold tracking-wider transition-colors text-center">
                        VIEW CONSTRUCTION
                      </Link>
                    </div>
                  </div>
                  
                  <div className="grid sm:grid-cols-2 gap-4 border-t border-gray-100 dark:border-gray-800 pt-6 transition-colors">
                    <div>
                      <p className="text-xs text-gray-400 uppercase tracking-wider font-bold mb-1 transition-colors">Constructor</p>
                      <p className="font-medium text-gray-900 dark:text-white transition-colors">{activeProject.constructorName}</p>
                    </div>
                    <div>
                      <p className="text-xs text-gray-400 uppercase tracking-wider font-bold mb-1 transition-colors">Current Phase</p>
                      <p className="font-medium text-gray-900 dark:text-white transition-colors">{activeProject.currentPhase || 'Site Preparation'}</p>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 border-dashed rounded-3xl p-12 text-center flex flex-col items-center justify-center transition-colors">
                  <div className="w-16 h-16 bg-gray-50 dark:bg-gray-800 rounded-full flex items-center justify-center mb-4 transition-colors">
                    <FolderKanban className="text-gray-400" size={24} />
                  </div>
                  <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-2 transition-colors">No active project yet</h3>
                  <p className="text-gray-500 dark:text-gray-400 text-sm mb-6 max-w-sm transition-colors">
                    Start by creating a new home project or requesting construction for an approved design.
                  </p>
                </div>
              )}
            </motion.div>

            {/* APPROVED DESIGNS */}
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.5 }}>
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold text-gray-900 dark:text-white transition-colors">Approved Designs</h2>
                <Link to="/dashboard/designs" className="text-sm font-semibold text-blue-600 dark:text-blue-400 hover:underline transition-colors">View all</Link>
              </div>
              
              <div className="grid sm:grid-cols-2 gap-4">
                {!approvedDesigns.length ? (
                  <div className="sm:col-span-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-8 text-center transition-colors">
                    <p className="text-gray-500 dark:text-gray-400 text-sm transition-colors">No designs have been approved yet.</p>
                  </div>
                ) : (
                  approvedDesigns.slice(0, 4).map(d => {
                    const isConstructing = construction?.activeProjects.some(p => p.houseDesignId === d.designId) || construction?.pendingRequests.some(p => p.houseDesignId === d.designId);
                    
                    return (
                      <div key={d.designId} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-5 shadow-sm hover:border-blue-200 dark:hover:border-blue-900/50 transition-colors flex flex-col h-full">
                        <div className="flex items-start justify-between mb-4">
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 bg-gray-50 dark:bg-gray-800 rounded-lg flex items-center justify-center transition-colors">
                              <LayoutTemplate className="text-gray-400 dark:text-gray-500" size={20} />
                            </div>
                            <div>
                              <h4 className="font-bold text-gray-900 dark:text-white truncate max-w-[150px] transition-colors">{d.title}</h4>
                              <span className="text-[10px] uppercase font-bold tracking-wider text-emerald-600 dark:text-emerald-400 flex items-center gap-1 transition-colors">
                                <Shield size={10} /> Approved
                              </span>
                            </div>
                          </div>
                        </div>
                        
                        <div className="flex gap-4 text-xs font-semibold text-gray-500 dark:text-gray-400 mb-6 bg-gray-50 dark:bg-gray-800/50 p-3 rounded-xl border border-gray-100 dark:border-gray-800 transition-colors">
                          <div><span className="text-gray-900 dark:text-white transition-colors">{d.bedrooms}</span> Beds</div>
                          <div><span className="text-gray-900 dark:text-white transition-colors">{d.bathrooms}</span> Baths</div>
                          <div><span className="text-gray-900 dark:text-white transition-colors">{d.floorCount}</span> Flrs</div>
                        </div>
                        
                        <div className="mt-auto flex gap-2">
                          <Link to={`/dashboard/workflows/${d.workflowId}?design=${d.designId}`} className="flex-1 py-2 text-center border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-900 dark:text-white rounded-lg text-xs font-bold transition-colors">
                            View
                          </Link>
                          {isConstructing ? (
                            <Link to={`/dashboard/construction`} className="flex-[2] py-2 text-center bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-900 dark:text-white rounded-lg text-xs font-bold transition-colors">
                              In Construction
                            </Link>
                          ) : (
                            <Link to={`/dashboard/construction?design=${d.designId}`} className="flex-[2] py-2 text-center bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 text-blue-700 dark:text-blue-400 rounded-lg text-xs font-bold transition-colors">
                              Find Constructor
                            </Link>
                          )}
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </motion.div>
          </div>

          {/* SIDEBAR */}
          <div className="space-y-6">
            <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.6 }} className="bg-gradient-to-br from-indigo-50 to-purple-50 dark:from-indigo-950/30 dark:to-purple-950/30 border border-indigo-100 dark:border-indigo-900/50 rounded-3xl p-6 shadow-sm transition-colors">
              <div className="w-10 h-10 bg-white dark:bg-gray-900 rounded-full flex items-center justify-center mb-4 shadow-sm text-indigo-600 dark:text-indigo-400 transition-colors">
                <Sparkles size={18} />
              </div>
              <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-2 transition-colors">Next Step</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-6 leading-relaxed transition-colors">
                {nextStep.msg}
              </p>
              <Link to={nextStep.link} className="w-full flex items-center justify-center gap-2 py-3 bg-gray-900 dark:bg-white text-white dark:text-gray-900 rounded-xl text-xs font-bold tracking-wider hover:opacity-90 transition-all shadow-sm">
                {nextStep.icon}
                {nextStep.btn}
              </Link>
            </motion.div>

            <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.7 }} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-3xl p-6 shadow-sm transition-colors">
              <h3 className="font-bold text-gray-900 dark:text-white mb-4 transition-colors">Quick Links</h3>
              <ul className="space-y-3">
                <li><Link to="/dashboard/designs" className="text-sm text-gray-600 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 flex items-center justify-between group transition-colors">My Designs <ArrowRight size={14} className="opacity-0 group-hover:opacity-100 transition-opacity" /></Link></li>
                <li><Link to="/dashboard/plans" className="text-sm text-gray-600 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 flex items-center justify-between group transition-colors">Plan Catalogue <ArrowRight size={14} className="opacity-0 group-hover:opacity-100 transition-opacity" /></Link></li>
                <li><Link to="/settings" className="text-sm text-gray-600 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 flex items-center justify-between group transition-colors">Account Settings <ArrowRight size={14} className="opacity-0 group-hover:opacity-100 transition-opacity" /></Link></li>
              </ul>
            </motion.div>
          </div>
          
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
