import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Box, ArrowRight, Sparkles, Cpu, Calculator, HardHat, Menu, X, Sun, Moon } from 'lucide-react';

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
    <div className="font-sans selection:bg-gray-900 dark:selection:bg-white selection:text-white dark:selection:text-gray-900 min-h-screen relative overflow-x-hidden bg-[#fcfcfd] dark:bg-gray-950 transition-colors duration-300">
      
      {/* NAVIGATION */}
      <nav className="fixed top-0 w-full z-50 transition-all border-b border-gray-200 dark:border-gray-800 bg-white/90 dark:bg-gray-950/90 backdrop-blur-md">
        <div className="max-w-[1600px] mx-auto px-8 h-24 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-3 hover:opacity-80 transition-opacity">
            <div className="w-8 h-8 relative flex items-center justify-center">
              <Box className="absolute text-gray-900 dark:text-white transition-colors" size={24} strokeWidth={1.5} />
              <Sparkles className="absolute text-yellow-600 -top-1 -right-1" size={12} />
            </div>
            <span className="text-sm font-bold text-gray-900 dark:text-white tracking-[0.2em]">HOMEPLANNER<span className="text-gray-400">AI</span></span>
          </Link>
          
          <div className="hidden lg:flex items-center gap-10 text-[11px] font-semibold tracking-[0.15em] text-gray-500 dark:text-gray-400">
            <Link to="/" className="hover:text-gray-900 dark:hover:text-white transition-colors">HOME</Link>
            <Link to="/features" className="text-gray-900 dark:text-white hover:text-blue-600 dark:hover:text-blue-400 transition-colors">FEATURES</Link>
            <Link to="/gallery" className="hover:text-gray-900 dark:hover:text-white transition-colors">GALLERY</Link>
          </div>

          <div className="flex items-center gap-4 sm:gap-6">
            <button onClick={() => setIsDark(!isDark)} className="text-gray-400 hover:text-gray-900 dark:hover:text-white transition-colors">
              {isDark ? <Sun size={20} /> : <Moon size={20} />}
            </button>
            <Link to="/login" className="hidden sm:block text-[11px] font-bold tracking-[0.15em] text-gray-900 dark:text-white hover:text-gray-600 dark:hover:text-gray-300 transition-colors">
              SIGN IN
            </Link>
            <Link to="/login" className="hidden sm:flex bg-gray-900 dark:bg-white hover:bg-black dark:hover:bg-gray-200 text-white dark:text-gray-900 px-7 py-3.5 rounded-none text-[11px] font-bold tracking-[0.15em] transition-all">
              START DESIGNING
            </Link>
            <button className="lg:hidden text-gray-900 dark:text-white" onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}>
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
            className="fixed inset-0 z-40 bg-white/95 dark:bg-gray-950/95 backdrop-blur-xl flex flex-col items-center justify-center gap-8 lg:hidden"
          >
            <Link to="/" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-gray-900 dark:text-white">HOME</Link>
            <Link to="/features" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-gray-900 dark:text-white">FEATURES</Link>
            <Link to="/gallery" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-gray-900 dark:text-white">GALLERY</Link>
            <Link to="/login" onClick={() => setIsMobileMenuOpen(false)} className="text-2xl font-semibold tracking-widest text-gray-500">SIGN IN</Link>
            <Link to="/login" onClick={() => setIsMobileMenuOpen(false)} className="mt-8 bg-gray-900 dark:bg-white text-white dark:text-gray-900 px-8 py-4 text-sm font-bold tracking-[0.15em]">
              START DESIGNING
            </Link>
          </motion.div>
        )}
      </AnimatePresence>

      {/* HERO SECTION */}
      <section className="pt-40 pb-20 relative z-10 px-6">
        <div className="max-w-[1400px] mx-auto text-center">
          <p className="text-[10px] font-bold tracking-[0.2em] text-gray-500 dark:text-gray-400 mb-6 uppercase">Platform Capabilities</p>
          <h1 className="text-4xl sm:text-6xl font-medium text-gray-900 dark:text-white tracking-tight mb-8 font-serif">
            How HomePlanner AI Works
          </h1>
          <p className="text-lg text-gray-500 dark:text-gray-400 max-w-2xl mx-auto font-light leading-relaxed">
            From initial conception to construction management, discover how our intelligent platform streamlines every phase of your project.
          </p>
        </div>
      </section>

      {/* FEATURES LIST */}
      <section className="py-20 relative z-10">
        <div className="max-w-[1200px] mx-auto px-6 flex flex-col gap-32">
          
          <div className="grid md:grid-cols-2 gap-12 items-center">
            <motion.div initial={{ opacity: 0, x: -30 }} whileInView={{ opacity: 1, x: 0 }} viewport={{ once: true }} className="order-2 md:order-1">
              <div className="w-12 h-12 bg-blue-100 dark:bg-blue-900/30 rounded-2xl flex items-center justify-center mb-6 text-blue-600 dark:text-blue-400">
                <Cpu size={24} />
              </div>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">AI Floor Plan Generation</h2>
              <p className="text-gray-500 dark:text-gray-400 leading-relaxed mb-6">
                Describe your ideal layout, and our architectural AI instantly generates accurate, mathematically sound floor plans. It respects standard building codes, optimizes space, and provides a structural foundation that architects can immediately work with.
              </p>
              <ul className="space-y-3 text-gray-900 dark:text-gray-300 font-medium text-sm">
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-blue-500 rounded-full"></div> Text-to-Floorplan generation</li>
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-blue-500 rounded-full"></div> Automated space optimization</li>
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-blue-500 rounded-full"></div> Instantly converts to 3D models</li>
              </ul>
            </motion.div>
            <div className="order-1 md:order-2 h-[400px] bg-gray-100 dark:bg-gray-800 rounded-3xl overflow-hidden shadow-xl">
              <img src="https://images.unsplash.com/photo-1600607686527-6fb886090705?q=80&w=800&auto=format&fit=crop" alt="Floor plan" className="w-full h-full object-cover" />
            </div>
          </div>

          <div className="grid md:grid-cols-2 gap-12 items-center">
            <div className="h-[400px] bg-gray-100 dark:bg-gray-800 rounded-3xl overflow-hidden shadow-xl">
              <img src="https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?q=80&w=800&auto=format&fit=crop" alt="Cost calculation" className="w-full h-full object-cover" />
            </div>
            <motion.div initial={{ opacity: 0, x: 30 }} whileInView={{ opacity: 1, x: 0 }} viewport={{ once: true }}>
              <div className="w-12 h-12 bg-emerald-100 dark:bg-emerald-900/30 rounded-2xl flex items-center justify-center mb-6 text-emerald-600 dark:text-emerald-400">
                <Calculator size={24} />
              </div>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">Real-Time Cost Estimation</h2>
              <p className="text-gray-500 dark:text-gray-400 leading-relaxed mb-6">
                As your design changes, so does your budget. Our intelligent pricing engine analyzes the materials, square footage, and structural complexity to provide highly accurate cost estimates instantly, keeping you in control of your finances.
              </p>
              <ul className="space-y-3 text-gray-900 dark:text-gray-300 font-medium text-sm">
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-emerald-500 rounded-full"></div> Dynamic material pricing</li>
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-emerald-500 rounded-full"></div> Labor cost integration</li>
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-emerald-500 rounded-full"></div> Exportable budget reports</li>
              </ul>
            </motion.div>
          </div>

          <div className="grid md:grid-cols-2 gap-12 items-center">
            <motion.div initial={{ opacity: 0, x: -30 }} whileInView={{ opacity: 1, x: 0 }} viewport={{ once: true }} className="order-2 md:order-1">
              <div className="w-12 h-12 bg-amber-100 dark:bg-amber-900/30 rounded-2xl flex items-center justify-center mb-6 text-amber-600 dark:text-amber-400">
                <HardHat size={24} />
              </div>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">Daily Construction Logs</h2>
              <p className="text-gray-500 dark:text-gray-400 leading-relaxed mb-6">
                A seamless handover to the build team. Contractors use our integrated management dashboard to post daily progress photos, log materials used, and track timeline milestones. Clients watch their AI dream turn into reality, step-by-step.
              </p>
              <ul className="space-y-3 text-gray-900 dark:text-gray-300 font-medium text-sm">
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-amber-500 rounded-full"></div> Client-Contractor transparency</li>
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-amber-500 rounded-full"></div> Photo progress tracking</li>
                <li className="flex items-center gap-3"><div className="w-1.5 h-1.5 bg-amber-500 rounded-full"></div> Milestone approvals</li>
              </ul>
            </motion.div>
            <div className="order-1 md:order-2 h-[400px] bg-gray-100 dark:bg-gray-800 rounded-3xl overflow-hidden shadow-xl">
              <img src="https://images.unsplash.com/photo-1503387762-592deb58ef4e?q=80&w=800&auto=format&fit=crop" alt="Construction" className="w-full h-full object-cover" />
            </div>
          </div>

        </div>
      </section>

      {/* FOOTER CTA */}
      <section className="py-24 bg-gray-900 text-white mt-20">
        <div className="max-w-[1400px] mx-auto px-6 text-center flex flex-col items-center">
          <h2 className="text-4xl font-medium mb-6 font-serif">Ready to experience the future of housing?</h2>
          <Link to="/login" className="bg-white hover:bg-gray-100 text-gray-900 px-10 py-5 text-xs font-bold tracking-[0.15em] transition-all flex items-center gap-3 mt-4">
            START DESIGNING NOW <ArrowRight size={16} />
          </Link>
        </div>
      </section>
    </div>
  );
};

export default FeaturesPage;
