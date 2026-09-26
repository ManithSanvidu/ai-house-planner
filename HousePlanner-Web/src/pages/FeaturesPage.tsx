import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Box, ArrowRight, Sparkles, Cpu, HardHat, Menu, X, Sun, Moon, Map, ClipboardCheck, ShieldCheck, PenTool, CheckCircle2 } from 'lucide-react';

const FeaturesPage: React.FC = () => {
 const [isDark, setIsDark] = useState(() => {
  return localStorage.getItem('theme') === 'dark' || window.matchMedia('(prefers-color-scheme: dark)').matches;
 });
 const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

 useEffect(() => {
  if (isDark) {
   document.documentElement.classList.add('dark');
   localStorage.setItem('theme', 'dark');
  } else {
   document.documentElement.classList.remove('dark');
   localStorage.setItem('theme', 'light');
  }
 }, [isDark]);

 return (
  <div className="font-sans selection:bg-gray-900 dark:selection:bg-surface selection:text-text-primary dark:selection:text-gray-900 min-h-screen relative overflow-x-hidden bg-background transition-colors duration-300">
   
   {/* NAVIGATION */}
   <nav className="fixed top-0 w-full z-50 transition-all border-b border-border dark:border-border-strong bg-surface/90 bg-background/90 backdrop-blur-md">
    <div className="max-w-[1600px] mx-auto px-8 h-24 flex items-center justify-between">
     <Link to="/" className="flex items-center gap-3 hover:opacity-80 transition-opacity">
      <div className="w-8 h-8 relative flex items-center justify-center">
       <Box className="absolute text-gray-900 dark:text-text-primary transition-colors" size={24} strokeWidth={1.5} />
       <Sparkles className="absolute text-yellow-600 -top-1 -right-1" size={12} />
      </div>
      <span className="text-sm font-bold text-gray-900 dark:text-text-primary tracking-[0.2em]">HOMEPLANNER<span className="text-text-secondary">AI</span></span>
     </Link>
     
     <div className="hidden lg:flex items-center gap-10 text-[11px] font-semibold tracking-[0.15em] text-text-muted text-text-secondary">
      <Link to="/" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">HOME</Link>
      <Link to="/features" className="text-gray-900 dark:text-text-primary hover:text-blue-600 dark:hover:text-blue-400 transition-colors">FEATURES</Link>
      <Link to="/gallery" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">GALLERY</Link>
     </div>

     <div className="flex items-center gap-4 sm:gap-6">
      <button onClick={() => setIsDark(!isDark)} className="text-text-secondary hover:text-gray-900 dark:hover:text-text-primary transition-colors">
       {isDark ? <Sun size={20} /> : <Moon size={20} />}
      </button>
      <Link to="/login" className="hidden sm:block text-[11px] font-bold tracking-[0.15em] text-gray-900 dark:text-text-primary hover:text-text-secondary dark:hover:text-gray-300 transition-colors">
       SIGN IN
      </Link>
      <Link to="/login" className="hidden sm:flex bg-gray-900 dark:bg-surface hover:bg-black dark:hover:bg-gray-200 text-text-primary dark:text-gray-900 px-7 py-3.5 rounded-none text-[11px] font-bold tracking-[0.15em] transition-all">
       START DESIGNING
      </Link>
      <button className="lg:hidden text-gray-900 dark:text-text-primary" onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}>
       {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
      </button>
     </div>
    </div>
   </nav>

   <AnimatePresence>
    {isMobileMenuOpen && (
     <motion.div
      initial={{ opacity: 0, y: -20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="fixed inset-0 z-40 bg-surface/95 bg-background/95 backdrop-blur-xl flex flex-col items-center justify-center gap-8 lg:hidden"
     >
      <Link to="/" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-gray-900 dark:text-text-primary">HOME</Link>
      <Link to="/features" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-gray-900 dark:text-text-primary">FEATURES</Link>
      <Link to="/gallery" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-gray-900 dark:text-text-primary">GALLERY</Link>
      <Link to="/login" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-text-muted">SIGN IN</Link>
      <Link to="/login" onClick={() => setIsMobileMenuOpen(false)} className="mt-8 bg-gray-900 dark:bg-surface text-text-primary dark:text-gray-900 px-8 py-4 text-sm font-bold tracking-[0.15em]">
       START DESIGNING
      </Link>
     </motion.div>
    )}
   </AnimatePresence>

   {/* HERO SECTION */}
   <section className="pt-40 pb-20 relative z-10 px-6">
    <div className="max-w-[1400px] mx-auto text-center">
     <p className="text-[10px] font-bold tracking-[0.2em] text-text-muted text-text-secondary mb-6 uppercase">Platform Capabilities</p>
     <h1 className="text-4xl sm:text-6xl font-medium text-gray-900 dark:text-text-primary tracking-tight mb-8 font-serif">
      From Land to Construction
     </h1>
     <p className="text-lg text-text-muted text-text-secondary max-w-2xl mx-auto font-light leading-relaxed">
      HomePlannerAI helps you turn land details and home requirements into a validated conceptual design, architect review workflow, and construction planning experience.
     </p>
    </div>
   </section>

   {/* CAPABILITY CARDS */}
   <section className="py-12 relative z-10">
    <div className="max-w-[1400px] mx-auto px-6">
     <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
      
      <motion.div initial={{ opacity: 0, y: 20 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} className="bg-surface border border-gray-100 dark:border-border-strong p-8 rounded-3xl group hover:border-blue-500/50 transition-colors">
       <div className="w-12 h-12 bg-blue-50 dark:bg-blue-900/20 rounded-2xl flex items-center justify-center mb-6 text-blue-600 dark:text-blue-400 group-hover:scale-110 transition-transform">
        <Map size={24} />
       </div>
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Land Analysis</h3>
       <p className="text-sm text-text-muted text-text-secondary mb-6 leading-relaxed">
        Capture land size, dimensions, terrain, road orientation, setbacks, and other site details to understand the usable building envelope.
       </p>
       <ul className="space-y-2 text-xs font-semibold text-text-secondary dark:text-gray-300">
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-blue-500" /> Plot dimensions</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-blue-500" /> Terrain context</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-blue-500" /> Buildable area</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-blue-500" /> Site constraints</li>
       </ul>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 20 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: "-50px" }} transition={{ delay: 0.1 }} className="bg-surface border border-gray-100 dark:border-border-strong p-8 rounded-3xl group hover:border-indigo-500/50 transition-colors">
       <div className="w-12 h-12 bg-indigo-50 dark:bg-indigo-900/20 rounded-2xl flex items-center justify-center mb-6 text-indigo-600 dark:text-indigo-400 group-hover:scale-110 transition-transform">
        <ClipboardCheck size={24} />
       </div>
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Smart Requirement Validation</h3>
       <p className="text-sm text-text-muted text-text-secondary mb-6 leading-relaxed">
        Check whether requested bedrooms, bathrooms, floors, parking, and other features are reasonable and supported before design generation begins.
       </p>
       <ul className="space-y-2 text-xs font-semibold text-text-secondary dark:text-gray-300">
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-indigo-500" /> Requirement sanity checks</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-indigo-500" /> Land-size feasibility</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-indigo-500" /> Feature compatibility</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-indigo-500" /> Clear validation feedback</li>
       </ul>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 20 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: "-50px" }} transition={{ delay: 0.2 }} className="bg-surface border border-gray-100 dark:border-border-strong p-8 rounded-3xl group hover:border-purple-500/50 transition-colors">
       <div className="w-12 h-12 bg-purple-50 dark:bg-purple-900/20 rounded-2xl flex items-center justify-center mb-6 text-purple-600 dark:text-purple-400 group-hover:scale-110 transition-transform">
        <Sparkles size={24} />
       </div>
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">AI-Assisted House Design</h3>
       <p className="text-sm text-text-muted text-text-secondary mb-6 leading-relaxed">
        AI helps select and adapt compatible validated base plans based on your land and home requirements.
       </p>
       <ul className="space-y-2 text-xs font-semibold text-text-secondary dark:text-gray-300">
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-purple-500" /> Compatible plan matching</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-purple-500" /> AI plan selection</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-purple-500" /> Safe layout adaptation</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-purple-500" /> Multiple design options</li>
       </ul>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 20 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: "-50px" }} transition={{ delay: 0.3 }} className="bg-surface border border-gray-100 dark:border-border-strong p-8 rounded-3xl group hover:border-emerald-500/50 transition-colors">
       <div className="w-12 h-12 bg-emerald-50 dark:bg-emerald-900/20 rounded-2xl flex items-center justify-center mb-6 text-emerald-600 dark:text-emerald-400 group-hover:scale-110 transition-transform">
        <ShieldCheck size={24} />
       </div>
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Plan Quality Validation</h3>
       <p className="text-sm text-text-muted text-text-secondary mb-6 leading-relaxed">
        Generated concepts are checked for geometry, circulation, zoning, room connectivity, stair alignment, and other planning-quality rules.
       </p>
       <ul className="space-y-2 text-xs font-semibold text-text-secondary dark:text-gray-300">
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-emerald-500" /> Geometry validation</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-emerald-500" /> Circulation checks</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-emerald-500" /> Room adjacency</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-emerald-500" /> Multi-floor consistency</li>
       </ul>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 20 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: "-50px" }} transition={{ delay: 0.4 }} className="bg-surface border border-gray-100 dark:border-border-strong p-8 rounded-3xl group hover:border-amber-500/50 transition-colors">
       <div className="w-12 h-12 bg-amber-50 dark:bg-amber-900/20 rounded-2xl flex items-center justify-center mb-6 text-amber-600 dark:text-amber-400 group-hover:scale-110 transition-transform">
        <PenTool size={24} />
       </div>
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Architect Review</h3>
       <p className="text-sm text-text-muted text-text-secondary mb-6 leading-relaxed">
        Send a selected design for architect review and keep approval history connected to the project.
       </p>
       <ul className="space-y-2 text-xs font-semibold text-text-secondary dark:text-gray-300">
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-amber-500" /> Design review workflow</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-amber-500" /> Approval status</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-amber-500" /> Revision support</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-amber-500" /> Version history</li>
       </ul>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 20 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: "-50px" }} transition={{ delay: 0.5 }} className="bg-surface border border-gray-100 dark:border-border-strong p-8 rounded-3xl group hover:border-rose-500/50 transition-colors">
       <div className="w-12 h-12 bg-rose-50 dark:bg-rose-900/20 rounded-2xl flex items-center justify-center mb-6 text-rose-600 dark:text-rose-400 group-hover:scale-110 transition-transform">
        <HardHat size={24} />
       </div>
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Construction Tracking</h3>
       <p className="text-sm text-text-muted text-text-secondary mb-6 leading-relaxed">
        Once a design is approved, request a constructor and manage the project through schedules, phases, daily logs, and calendar progress.
       </p>
       <ul className="space-y-2 text-xs font-semibold text-text-secondary dark:text-gray-300">
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-rose-500" /> Constructor requests</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-rose-500" /> Phase schedule</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-rose-500" /> Daily site logs</li>
        <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-rose-500" /> Progress calendar</li>
       </ul>
      </motion.div>

     </div>
    </div>
   </section>

   {/* HOW IT WORKS */}
   <section className="py-24 relative z-10">
    <div className="max-w-[1400px] mx-auto px-6">
     <div className="text-center mb-16">
      <h2 className="text-3xl md:text-4xl font-serif text-gray-900 dark:text-text-primary">How the workflow works</h2>
     </div>
     
     <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-6 lg:gap-4 relative">
      {/* Timeline connection line (desktop only) */}
      <div className="hidden lg:block absolute top-12 left-[10%] right-[10%] h-0.5 bg-gray-200 bg-surface-elevated z-0"></div>
      
      {[
       { num: "01", title: "Enter Land Details", icon: <Map size={20} /> },
       { num: "02", title: "Add House Requirements", icon: <ClipboardCheck size={20} /> },
       { num: "03", title: "Validate Feasibility", icon: <ShieldCheck size={20} /> },
       { num: "04", title: "Generate Design Options", icon: <Cpu size={20} /> },
       { num: "05", title: "Send to Architect", icon: <PenTool size={20} /> },
       { num: "06", title: "Start Construction Tracking", icon: <HardHat size={20} /> },
      ].map((step, i) => (
       <div key={i} className="relative z-10 flex flex-col items-center text-center group">
        <div className="w-24 h-24 bg-surface border-4 border-background rounded-full flex items-center justify-center shadow-lg mb-4 text-text-secondary group-hover:text-blue-500 group-hover:border-blue-50 transition-colors">
         {step.icon}
        </div>
        <span className="text-[10px] font-bold tracking-[0.2em] text-blue-600 dark:text-blue-400 mb-2">STEP {step.num}</span>
        <h4 className="text-sm font-semibold text-gray-900 dark:text-text-primary max-w-[120px]">{step.title}</h4>
       </div>
      ))}
     </div>
    </div>
   </section>

   {/* VALIDATION / TRUST */}
   <section className="py-24 relative z-10 bg-gray-50 bg-surface/50">
    <div className="max-w-[1000px] mx-auto px-6 text-center">
     <h2 className="text-3xl md:text-4xl font-serif text-gray-900 dark:text-text-primary mb-6">
      AI Suggestions, Deterministic Validation
     </h2>
     <p className="text-lg text-text-secondary mb-10 leading-relaxed max-w-3xl mx-auto">
      HomePlannerAI uses AI for intelligent plan selection and design intent, while deterministic validation checks geometry, spatial constraints, and supported planning rules before a concept is accepted.
     </p>
     
     <div className="flex flex-col sm:flex-row justify-center gap-6 sm:gap-12 mb-16">
      <div className="flex items-center justify-center gap-3 text-gray-800 dark:text-gray-200 font-medium">
       <Cpu className="text-blue-500" size={20} />
       <span>AI-assisted decisions</span>
      </div>
      <div className="flex items-center justify-center gap-3 text-gray-800 dark:text-gray-200 font-medium">
       <ShieldCheck className="text-emerald-500" size={20} />
       <span>Rule-based validation</span>
      </div>
      <div className="flex items-center justify-center gap-3 text-gray-800 dark:text-gray-200 font-medium">
       <PenTool className="text-amber-500" size={20} />
       <span>Human architect review</span>
      </div>
     </div>
     
     <p className="text-xs text-text-secondary text-text-muted max-w-2xl mx-auto italic">
      HomePlannerAI provides conceptual planning assistance and does not replace licensed architectural, structural, or regulatory approval.
     </p>
    </div>
   </section>

   {/* FOOTER CTA */}
   <section className="py-24 bg-gray-900 text-text-primary mt-12 relative overflow-hidden">
    <div className="absolute inset-0 opacity-10 bg-[url('https://images.unsplash.com/photo-1600607686527-6fb886090705?q=80&w=2000&auto=format&fit=crop')] bg-cover bg-center"></div>
    <div className="max-w-[1400px] mx-auto px-6 text-center flex flex-col items-center relative z-10">
     <h2 className="text-4xl font-medium mb-10 font-serif">Ready to plan your home?</h2>
     <div className="flex flex-col sm:flex-row gap-4">
      <Link to="/login" className="bg-surface hover:bg-gray-100 text-gray-900 px-10 py-5 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center gap-3 rounded-none">
       START NEW PROJECT <ArrowRight size={16} />
      </Link>
      <Link to="/gallery" className="bg-transparent border border-gray-600 hover:border-white text-text-primary px-10 py-5 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center rounded-none">
       BROWSE PLANS
      </Link>
     </div>
    </div>
   </section>
  </div>
 );
};

export default FeaturesPage;
