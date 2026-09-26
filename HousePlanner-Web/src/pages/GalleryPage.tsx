import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Box, Sparkles, X, Sun, Moon, ArrowRight, Menu } from 'lucide-react';
import { preDesignedPlanService, type PreDesignedPlanSummary } from '../services/preDesignedPlanService';
import { getCustomerPlanName } from '../utils/presentation';

const GalleryPage: React.FC = () => {
 const [isDark, setIsDark] = useState(() => {
  return localStorage.getItem('theme') === 'dark' || window.matchMedia('(prefers-color-scheme: dark)').matches;
 });
 const [plans, setPlans] = useState<PreDesignedPlanSummary[]>([]);
 const [selectedImg, setSelectedImg] = useState<PreDesignedPlanSummary | null>(null);
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

 useEffect(() => {
  preDesignedPlanService.list({}).then(data => {
   // Filter out plans that might not have images, although we'll assume they all do based on designCode
   setPlans(data.filter(p => p.isActive));
  }).catch(err => console.error("Failed to load plans for gallery", err));
 }, []);

 return (
  <div className="font-sans selection:bg-gray-900 dark:selection:bg-surface selection:text-text-primary dark:selection:text-gray-900 min-h-screen relative overflow-x-hidden bg-background transition-colors duration-300 pb-32">
   
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
      <Link to="/features" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">FEATURES</Link>
      <Link to="/gallery" className="text-gray-900 dark:text-text-primary hover:text-blue-600 dark:hover:text-blue-400 transition-colors">GALLERY</Link>
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

   {/* HERO */}
   <section className="pt-40 pb-12 relative z-10 px-6">
    <div className="max-w-[1400px] mx-auto text-center">
     <h1 className="text-4xl sm:text-6xl font-medium text-gray-900 dark:text-text-primary tracking-tight mb-6 font-serif">
      Design Gallery
     </h1>
     <p className="text-lg text-text-muted text-text-secondary max-w-xl mx-auto font-light leading-relaxed">
      Click on any design to reveal its structural parameters and buildable area requirements, intelligently generated by our AI.
     </p>
    </div>
   </section>

   {/* MASONRY GRID */}
   <section className="max-w-[1400px] mx-auto px-6 relative z-10">
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
     {plans.map((item, index) => {
      const imgSrc = `/designs/${item.designCode}-F1.svg.png`;
      return (
      <motion.div
       initial={{ opacity: 0, y: 20 }}
       animate={{ opacity: 1, y: 0 }}
       transition={{ delay: index * 0.05 }}
       key={item.id}
       className="relative group cursor-pointer rounded-2xl overflow-hidden aspect-square bg-surface-elevated border border-gray-100 dark:border-border-strong"
       onClick={() => setSelectedImg(item)}
      >
       <div className="w-full h-full p-8 flex items-center justify-center">
        <img src={imgSrc} alt={item.name} className="w-full h-full object-contain transition-transform duration-700 group-hover:scale-105" />
       </div>
       <div className="absolute inset-0 bg-black/0 group-hover:bg-black/60 transition-colors duration-300 flex flex-col items-center justify-center p-6 text-center">
        <span className="text-text-primary opacity-0 group-hover:opacity-100 font-bold tracking-widest text-lg transition-opacity duration-300 mb-2">
         {getCustomerPlanName(item)}
        </span>
        <span className="text-gray-300 opacity-0 group-hover:opacity-100 text-sm transition-opacity duration-300 delay-75">
         VIEW DETAILS
        </span>
       </div>
      </motion.div>
     )})}
    </div>
   </section>

   {/* MODAL */}
   <AnimatePresence>
    {selectedImg && (
     <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm"
      onClick={() => setSelectedImg(null)}
     >
      <motion.div 
       initial={{ scale: 0.95, opacity: 0 }}
       animate={{ scale: 1, opacity: 1 }}
       exit={{ scale: 0.95, opacity: 0 }}
       className="bg-surface rounded-3xl overflow-hidden max-w-4xl w-full grid md:grid-cols-2 shadow-2xl relative"
       onClick={e => e.stopPropagation()}
      >
       <div className="h-[300px] md:h-[500px] p-8 bg-gray-50 bg-surface-elevated flex items-center justify-center">
        <img src={`/designs/${selectedImg.designCode}-F1.svg.png`} alt={selectedImg.name} className="w-full h-full object-contain" />
       </div>
       <div className="p-8 md:p-12 flex flex-col justify-center relative">
        <button onClick={() => setSelectedImg(null)} className="absolute top-6 right-6 text-text-secondary hover:text-gray-900 dark:hover:text-text-primary transition-colors">
         <X size={24} />
        </button>
        <div className="flex items-center gap-2 mb-4">
         <Sparkles size={16} className="text-indigo-600 dark:text-indigo-400" />
         <span className="text-[10px] font-bold tracking-[0.2em] text-text-muted text-text-secondary">DESIGN PARAMETERS</span>
        </div>
        <h2 className="text-2xl md:text-3xl text-gray-900 dark:text-text-primary font-serif leading-relaxed mb-6">
         {getCustomerPlanName(selectedImg)}
        </h2>
        
        <div className="grid grid-cols-2 gap-y-4 gap-x-6 mb-8 text-sm text-text-secondary dark:text-gray-300">
         <div className="flex flex-col">
          <span className="text-[10px] text-text-secondary font-bold uppercase tracking-wider mb-1">Configuration</span>
          <span>{selectedImg.bedrooms} Bed, {selectedImg.bathrooms} Bath</span>
         </div>
         <div className="flex flex-col">
          <span className="text-[10px] text-text-secondary font-bold uppercase tracking-wider mb-1">Floors</span>
          <span>{selectedImg.floorCount} Story</span>
         </div>
         <div className="flex flex-col">
          <span className="text-[10px] text-text-secondary font-bold uppercase tracking-wider mb-1">Area</span>
          <span>{selectedImg.totalBuiltUpAreaSqft.toLocaleString()} sq ft</span>
         </div>
         <div className="flex flex-col">
          <span className="text-[10px] text-text-secondary font-bold uppercase tracking-wider mb-1">Min. Land</span>
          <span>{selectedImg.minimumLandSizePerches} Perches</span>
         </div>
         <div className="flex flex-col col-span-2">
          <span className="text-[10px] text-text-secondary font-bold uppercase tracking-wider mb-1">Suitable Terrain</span>
          <span className="capitalize">{selectedImg.suitableTerrain}</span>
         </div>
        </div>

        <Link to={`/dashboard/plans/${selectedImg.id}`} className="bg-indigo-600 text-text-primary px-6 py-4 rounded-xl text-xs font-bold tracking-[0.1em] hover:bg-indigo-700 transition-colors text-center w-full shadow-md">
         VIEW FULL PLAN
        </Link>
       </div>
      </motion.div>
     </motion.div>
    )}
   </AnimatePresence>

   {/* STICKY BOTTOM BANNER */}
   <motion.div 
    initial={{ y: 100 }}
    animate={{ y: 0 }}
    transition={{ delay: 1, type: 'spring' }}
    className="fixed bottom-6 left-6 right-6 md:left-1/2 md:-translate-x-1/2 md:right-auto md:w-[600px] z-40 bg-surface/90 dark:bg-surface/90 backdrop-blur-xl rounded-2xl shadow-2xl p-4 border border-gray-700/50 dark:border-border/50 flex flex-col sm:flex-row items-center justify-between gap-4"
   >
    <div className="text-center sm:text-left">
     <p className="text-text-primary dark:text-gray-900 font-bold">Want to generate your own?</p>
     <p className="text-text-secondary dark:text-text-secondary text-sm">Join thousands of others today.</p>
    </div>
    <Link to="/login" className="bg-surface text-gray-900 dark:text-text-primary px-6 py-3 rounded-xl text-xs font-bold tracking-[0.1em] hover:scale-105 transition-transform flex items-center gap-2 whitespace-nowrap">
     SIGN UP NOW <ArrowRight size={14} />
    </Link>
   </motion.div>

  </div>
 );
};

export default GalleryPage;
