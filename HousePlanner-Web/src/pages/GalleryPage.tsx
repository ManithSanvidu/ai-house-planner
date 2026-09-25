import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Box, Sparkles, X, Sun, Moon, ArrowRight, Menu } from 'lucide-react';

const galleryItems = [
  { id: 1, src: "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?q=80&w=800&auto=format&fit=crop", prompt: "A modern luxury mansion with large floor-to-ceiling windows, warm interior lighting, dark exterior accents, situated in a forest setting at twilight." },
  { id: 2, src: "https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?q=80&w=800&auto=format&fit=crop", prompt: "Minimalist open concept living room and kitchen, featuring white marble countertops, light oak hardwood floors, and matte black fixtures." },
  { id: 3, src: "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?q=80&w=800&auto=format&fit=crop", prompt: "Suburban contemporary home exterior with a two-car garage, white stucco walls, natural wood paneling, and a neatly manicured front lawn." },
  { id: 4, src: "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?q=80&w=800&auto=format&fit=crop", prompt: "Luxurious master bathroom with a freestanding white soaking tub, dark grey slate tiles, double vanity, and soft natural sunlight." },
  { id: 5, src: "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=800&auto=format&fit=crop", prompt: "Cozy backyard patio with a modern wooden pergola, outdoor seating area, fire pit, and warm string lights overhead." },
  { id: 6, src: "https://images.unsplash.com/photo-1556912173-3bb406ef7e77?q=80&w=800&auto=format&fit=crop", prompt: "Rustic modern kitchen featuring exposed brick walls, industrial pendant lighting, stainless steel appliances, and a large wooden dining table." }
];

const GalleryPage: React.FC = () => {
  const [isDark, setIsDark] = useState(() => {
    return localStorage.getItem('theme') === 'dark' || window.matchMedia('(prefers-color-scheme: dark)').matches;
  });
  const [selectedImg, setSelectedImg] = useState<typeof galleryItems[0] | null>(null);
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
    <div className="font-sans selection:bg-gray-900 dark:selection:bg-white selection:text-white dark:selection:text-gray-900 min-h-screen relative overflow-x-hidden bg-[#fcfcfd] dark:bg-gray-950 transition-colors duration-300 pb-32">
      
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
            <Link to="/features" className="hover:text-gray-900 dark:hover:text-white transition-colors">FEATURES</Link>
            <Link to="/gallery" className="text-gray-900 dark:text-white hover:text-blue-600 dark:hover:text-blue-400 transition-colors">GALLERY</Link>
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

      {/* HERO */}
      <section className="pt-40 pb-12 relative z-10 px-6">
        <div className="max-w-[1400px] mx-auto text-center">
          <h1 className="text-4xl sm:text-6xl font-medium text-gray-900 dark:text-white tracking-tight mb-6 font-serif">
            Design Gallery
          </h1>
          <p className="text-lg text-gray-500 dark:text-gray-400 max-w-xl mx-auto font-light leading-relaxed">
            Click on any image to reveal the exact text prompt used to generate these stunning architectural spaces with our AI.
          </p>
        </div>
      </section>

      {/* MASONRY GRID */}
      <section className="max-w-[1400px] mx-auto px-6 relative z-10">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {galleryItems.map((item, index) => (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: index * 0.1 }}
              key={item.id}
              className="relative group cursor-pointer rounded-2xl overflow-hidden aspect-square bg-gray-100 dark:bg-gray-800"
              onClick={() => setSelectedImg(item)}
            >
              <img src={item.src} alt="AI Generation" className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105" />
              <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-colors duration-300 flex items-center justify-center">
                <span className="text-white opacity-0 group-hover:opacity-100 font-bold tracking-widest text-sm transition-opacity duration-300">
                  VIEW PROMPT
                </span>
              </div>
            </motion.div>
          ))}
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
              className="bg-white dark:bg-gray-900 rounded-3xl overflow-hidden max-w-4xl w-full grid md:grid-cols-2 shadow-2xl"
              onClick={e => e.stopPropagation()}
            >
              <div className="h-[300px] md:h-[500px]">
                <img src={selectedImg.src} alt="Selected" className="w-full h-full object-cover" />
              </div>
              <div className="p-8 md:p-12 flex flex-col justify-center relative">
                <button onClick={() => setSelectedImg(null)} className="absolute top-6 right-6 text-gray-400 hover:text-gray-900 dark:hover:text-white">
                  <X size={24} />
                </button>
                <div className="flex items-center gap-2 mb-4">
                  <Sparkles size={16} className="text-yellow-600" />
                  <span className="text-[10px] font-bold tracking-[0.2em] text-gray-500 dark:text-gray-400">AI PROMPT USED</span>
                </div>
                <p className="text-xl md:text-2xl text-gray-900 dark:text-white font-serif leading-relaxed mb-8">
                  "{selectedImg.prompt}"
                </p>
                <Link to="/login" className="bg-gray-900 dark:bg-white text-white dark:text-gray-900 px-6 py-4 text-xs font-bold tracking-[0.15em] hover:bg-black dark:hover:bg-gray-200 transition-colors text-center">
                  TRY THIS PROMPT
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
        className="fixed bottom-6 left-6 right-6 md:left-1/2 md:-translate-x-1/2 md:right-auto md:w-[600px] z-40 bg-gray-900/90 dark:bg-white/90 backdrop-blur-xl rounded-2xl shadow-2xl p-4 border border-gray-700/50 dark:border-gray-200/50 flex flex-col sm:flex-row items-center justify-between gap-4"
      >
        <div className="text-center sm:text-left">
          <p className="text-white dark:text-gray-900 font-bold">Want to generate your own?</p>
          <p className="text-gray-400 dark:text-gray-600 text-sm">Join thousands of others today.</p>
        </div>
        <Link to="/login" className="bg-white dark:bg-gray-900 text-gray-900 dark:text-white px-6 py-3 rounded-xl text-xs font-bold tracking-[0.1em] hover:scale-105 transition-transform flex items-center gap-2 whitespace-nowrap">
          SIGN UP NOW <ArrowRight size={14} />
        </Link>
      </motion.div>

    </div>
  );
};

export default GalleryPage;
