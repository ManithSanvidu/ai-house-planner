import React, { useRef, useState, Suspense, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Canvas, useFrame } from '@react-three/fiber';
import { OrbitControls, Html, useGLTF } from '@react-three/drei';
import { motion, useScroll, useTransform, AnimatePresence } from 'framer-motion';
import { ArrowRight, Sparkles, Box, Moon, Sun, Menu, X, MessageSquare, Map, ClipboardCheck, Home, PenTool, HardHat, CheckCircle2, ShieldCheck, Activity } from 'lucide-react';
import * as THREE from 'three';
import apiClient from '../services/apiClient';
// ---------------------------------------------------------
// 3D Components
// ---------------------------------------------------------

const AnimatedPart = ({ start, end, yOffset = 0, scaleBase = 1, children }: any) => {
 const ref = useRef<THREE.Group>(null);
 useFrame(({ clock }) => {
  // Run animation once and stop when reaching 'end'
  const t = clock.elapsedTime;
  if (t < start) {
   if (ref.current) ref.current.scale.setScalar(0.0001);
   return;
  }
  const progress = Math.max(0, Math.min(1, (t - start) / (end - start)));
  const ease = 1 - Math.pow(1 - progress, 4); // easeOutQuart
  if (ref.current) {
   ref.current.scale.setScalar(Math.max(0.0001, ease * scaleBase));
   if (yOffset !== 0) {
    ref.current.position.y = yOffset * (1 - ease);
   }
  }
 });
 return <group ref={ref}>{children}</group>;
};

const ProceduralHouse = () => {
 const groupRef = useRef<THREE.Group>(null);
 const mats = React.useMemo(() => ({
  grass: new THREE.MeshStandardMaterial({ color: '#3f6212', roughness: 1 }),
  asphalt: new THREE.MeshStandardMaterial({ color: '#3f3f46', roughness: 0.9 }),
  concrete: new THREE.MeshStandardMaterial({ color: '#d4d4d8', roughness: 0.9 }),
  wall: new THREE.MeshStandardMaterial({ color: '#f4f4f5', roughness: 0.9 }),
  accentWall: new THREE.MeshStandardMaterial({ color: '#52525b', roughness: 0.8 }),
  wood: new THREE.MeshStandardMaterial({ color: '#78350f', roughness: 0.7 }),
  glass: new THREE.MeshStandardMaterial({ color: '#0f172a', roughness: 0.1, metalness: 0.9, transparent: true, opacity: 0.7, depthWrite: false }),
  frame: new THREE.MeshStandardMaterial({ color: '#171717', roughness: 0.5 }),
  roof: new THREE.MeshStandardMaterial({ color: '#1c1917', roughness: 0.8 }),
  soil: new THREE.MeshStandardMaterial({ color: '#292524', roughness: 1 }),
  leaves: new THREE.MeshStandardMaterial({ color: '#15803d', roughness: 0.9 }),
  metal: new THREE.MeshStandardMaterial({ color: '#52525b', roughness: 0.4, metalness: 0.8 }),
  lightBulb: new THREE.MeshStandardMaterial({ color: '#fef08a', emissive: '#fef08a', emissiveIntensity: 2 })
 }), []);

 return (
  <group ref={groupRef} position={[2, -1, 0]} scale={0.7}>
   
   {/* TIME 0-1: Landscaping & Boundary */}
   <AnimatedPart start={0} end={1.5} yOffset={-1}>
    <mesh position={[0, -0.05, 0]} receiveShadow material={mats.grass}>
     <boxGeometry args={[26, 0.1, 26]} />
    </mesh>
    <group>
     <mesh position={[0, 1, -13]} castShadow receiveShadow material={mats.concrete}><boxGeometry args={[26, 2, 0.4]} /></mesh>
     <mesh position={[-13, 1, 0]} castShadow receiveShadow material={mats.concrete}><boxGeometry args={[0.4, 2, 26]} /></mesh>
     <mesh position={[13, 1, 0]} castShadow receiveShadow material={mats.concrete}><boxGeometry args={[0.4, 2, 26]} /></mesh>
     <mesh position={[-8.5, 1, 13]} castShadow receiveShadow material={mats.concrete}><boxGeometry args={[9, 2, 0.4]} /></mesh>
     <mesh position={[8.5, 1, 13]} castShadow receiveShadow material={mats.concrete}><boxGeometry args={[9, 2, 0.4]} /></mesh>
    </group>
   </AnimatedPart>

   {/* TIME 1-2.5: Driveway & Pathway */}
   <AnimatedPart start={1} end={2.5}>
    <mesh position={[-2.5, 0.02, 8]} receiveShadow material={mats.asphalt}><boxGeometry args={[5, 0.05, 10]} /></mesh>
    <mesh position={[2, 0.02, 8]} receiveShadow material={mats.concrete}><boxGeometry args={[2, 0.05, 10]} /></mesh>
   </AnimatedPart>

   {/* TIME 2-3.5: Foundation & Steps */}
   <AnimatedPart start={2} end={3.5} yOffset={-1}>
    <mesh position={[0, 0.2, 0]} castShadow receiveShadow material={mats.concrete}>
     <boxGeometry args={[12, 0.4, 8]} />
    </mesh>
    <mesh position={[2, 0.15, 4.5]} castShadow receiveShadow material={mats.concrete}><boxGeometry args={[3, 0.3, 1]} /></mesh>
    <mesh position={[2, 0.3, 4.2]} castShadow receiveShadow material={mats.concrete}><boxGeometry args={[3, 0.2, 0.6]} /></mesh>
   </AnimatedPart>

   {/* TIME 3-4.5: Ground Floor Structure */}
   <AnimatedPart start={3} end={4.5} yOffset={2}>
    <mesh position={[2.9, 1.9, 0]} castShadow receiveShadow material={mats.wall}>
     <boxGeometry args={[6.0, 3, 7.8]} />
    </mesh>
    <mesh position={[-3, 1.9, 0]} castShadow receiveShadow material={mats.accentWall}>
     <boxGeometry args={[5.8, 3, 7.8]} />
    </mesh>
    <mesh position={[0.6, 1.9, 4.1]} castShadow receiveShadow material={mats.frame}><boxGeometry args={[0.3, 3, 0.3]} /></mesh>
    <mesh position={[3.4, 1.9, 4.1]} castShadow receiveShadow material={mats.frame}><boxGeometry args={[0.3, 3, 0.3]} /></mesh>
   </AnimatedPart>

   {/* TIME 4-5.5: Ground Floor Details (Doors/Windows) */}
   <AnimatedPart start={4} end={5.5}>
    <mesh position={[-3, 1.5, 3.96]} material={mats.frame}><boxGeometry args={[4.2, 2.4, 0.1]} /></mesh>
    <mesh position={[2, 1.6, 3.96]} material={mats.wood}><boxGeometry args={[1.4, 2.4, 0.1]} /></mesh>
    <mesh position={[1.4, 1.5, 4.02]} material={mats.metal}><cylinderGeometry args={[0.03, 0.03, 0.4]} /></mesh> 
    <mesh position={[4.5, 1.8, 3.96]} material={mats.glass}><boxGeometry args={[2, 2, 0.1]} /></mesh>
    <mesh position={[4.5, 1.8, 3.96]} material={mats.frame}><boxGeometry args={[2.2, 2.2, 0.15]} /></mesh>
   </AnimatedPart>

   {/* TIME 5-6.5: First Floor Slab */}
   <AnimatedPart start={5} end={6.5} yOffset={2}>
    <mesh position={[0, 3.55, 0]} castShadow receiveShadow material={mats.concrete}>
     <boxGeometry args={[12.4, 0.3, 8.4]} />
    </mesh>
    <mesh position={[2, 4.05, 4.1]} material={mats.glass}><boxGeometry args={[4, 1, 0.05]} /></mesh>
    <mesh position={[2, 4.55, 4.1]} material={mats.metal}><boxGeometry args={[4, 0.05, 0.1]} /></mesh>
   </AnimatedPart>

   {/* TIME 6-7.5: First Floor Walls & Windows */}
   <AnimatedPart start={6} end={7.5} yOffset={2}>
    <mesh position={[0, 5.1, -0.5]} castShadow receiveShadow material={mats.wall}>
     <boxGeometry args={[11.8, 2.8, 6.8]} />
    </mesh>
    <mesh position={[-3, 5.1, 2.96]} castShadow receiveShadow material={mats.wood}>
     <boxGeometry args={[5, 2.8, 0.1]} />
    </mesh>
    <mesh position={[2, 5.1, 2.96]} material={mats.glass}><boxGeometry args={[4, 2.2, 0.1]} /></mesh>
    <mesh position={[2, 5.1, 2.96]} material={mats.frame}><boxGeometry args={[4.2, 2.4, 0.15]} /></mesh>
   </AnimatedPart>

   {/* TIME 7-8.5: Roof & Eaves */}
   <AnimatedPart start={7} end={8.5} yOffset={3}>
    <mesh position={[0, 6.7, -0.5]} castShadow receiveShadow material={mats.roof}>
     <boxGeometry args={[12.8, 0.4, 7.8]} />
    </mesh>
   </AnimatedPart>

   {/* TIME 8-10: Landscaping Details & Gates */}
   <AnimatedPart start={8} end={10} yOffset={1}>
    <mesh position={[-2.5, 1, 13]} material={mats.metal}><boxGeometry args={[4.8, 1.8, 0.1]} /></mesh>
    <mesh position={[2, 1, 13]} material={mats.metal}><boxGeometry args={[1.5, 1.8, 0.1]} /></mesh>
    
    <mesh position={[8, 0.1, 8]} material={mats.soil}><boxGeometry args={[4, 0.2, 4]} /></mesh>
    <mesh position={[-8, 0.1, -8]} material={mats.soil}><boxGeometry args={[4, 0.2, 4]} /></mesh>

    <group position={[8, 0, 8]}>
     <mesh position={[0, 1, 0]} castShadow material={mats.wood}><cylinderGeometry args={[0.2, 0.3, 2]} /></mesh>
     <mesh position={[0, 2.5, 0]} castShadow material={mats.leaves}><icosahedronGeometry args={[1.5, 1]} /></mesh>
    </group>
    <group position={[-8, 0, -8]}>
     <mesh position={[0, 1, 0]} castShadow material={mats.wood}><cylinderGeometry args={[0.2, 0.3, 2]} /></mesh>
     <mesh position={[0, 3, 0]} castShadow material={mats.leaves}><icosahedronGeometry args={[1.8, 1]} /></mesh>
    </group>
    
    <mesh position={[4, 0.5, 4.5]} castShadow material={mats.leaves}><sphereGeometry args={[0.6]} /></mesh>
    <mesh position={[5, 0.4, 4.5]} castShadow material={mats.leaves}><sphereGeometry args={[0.4]} /></mesh>
   </AnimatedPart>


  </group>
 );
};

export const ExternalHouseModel = ({ url }: { url: string }) => {
 const { scene } = useGLTF(url);
 const groupRef = useRef<THREE.Group>(null);
 
 useEffect(() => {
  scene.traverse((child) => {
   if (child instanceof THREE.Mesh) {
    child.castShadow = true;
    child.receiveShadow = true;
    if (child.material) {
     if (child.material.name.toLowerCase().includes('glass')) {
      child.material.transparent = true;
      child.material.opacity = 0.4;
      child.material.roughness = 0;
      child.material.metalness = 1;
      child.material.envMapIntensity = 2;
     } else if (child.material.name.toLowerCase().includes('concrete') || child.material.name.toLowerCase().includes('wall')) {
      child.material.roughness = 0.9;
      child.material.metalness = 0.1;
     }
    }
   }
  });
 }, [scene]);

 return <primitive ref={groupRef} object={scene} position={[0, -1, 0]} scale={1} />;
};

const HouseScene = ({ isDark }: { isDark: boolean }) => {
 useFrame((state) => {
  state.camera.position.lerp(new THREE.Vector3(12, 4, 15), 0.02);
  state.camera.lookAt(0, 2, 0);
 });

 return (
  <>
   <ambientLight intensity={isDark ? 0.1 : 0.2} color="#f8fafc" />
   <directionalLight 
    position={[15, 25, 10]} 
    intensity={isDark ? 1.0 : 2.5} 
    color={isDark ? "#e2e8f0" : "#fffbeb"} 
    castShadow 
    shadow-mapSize={[2048, 2048]} 
    shadow-camera-far={50}
    shadow-camera-left={-10}
    shadow-camera-right={10}
    shadow-camera-top={10}
    shadow-camera-bottom={-10}
    shadow-bias={-0.0001}
   />
   <directionalLight position={[-15, -10, -15]} color={isDark ? "#1e40af" : "#38bdf8"} intensity={isDark ? 0.3 : 0.5} />
   
   {/* Removed Environment to prevent CORS / Network errors with pmndrs GitHub raw assets */}
   
   {/* Centered the house and moved up further */}
   <group position={[-2, 1.2, 0]}>
    <Suspense fallback={null}>
     <ProceduralHouse />
    </Suspense>
   </group>

   {/* ContactShadows removed to prevent severe Z-fighting glitching with the physical grass floor */}

   {/* Floating Architectural Annotations */}
   {/* Floor Area points left towards the ground floor main volume */}
   <Html position={[3.0, 2.46, 2.77]} center className="pointer-events-none">
    <div className="relative flex items-center gap-4">
     <div className="w-16 h-[1px] bg-surface/50 dark:bg-gray-500/50 hidden md:block"></div>
     <div className="bg-surface/90 bg-surface/90 backdrop-blur-md border border-border/50 border-border/50 px-4 py-2 rounded-lg shadow-xl text-xs font-mono w-max transition-colors">
      <div className="text-text-secondary text-text-muted text-[9px] uppercase tracking-wider mb-1 font-bold">Floor Area</div>
      <div className="text-gray-900 dark:text-text-primary font-bold">2,450 SQ FT</div>
     </div>
    </div>
   </Html>
   {/* Bedrooms points right towards the upper floor slab / balcony base */}
   <Html position={[-6.0, 3.68, 2.07]} center className="pointer-events-none">
    <div className="relative flex items-center gap-4 flex-row-reverse">
     <div className="w-16 h-[1px] bg-gray-900/50 dark:bg-gray-500/50 hidden md:block"></div>
     <div className="bg-surface/90 dark:bg-black/90 backdrop-blur-md border border-gray-700/50 px-4 py-2 rounded-lg shadow-xl text-xs font-mono w-max transition-colors">
      <div className="text-text-secondary text-[9px] uppercase tracking-wider mb-1 font-bold">Bedrooms</div>
      <div className="text-text-primary font-bold">04</div>
     </div>
    </div>
   </Html>
  </>
 );
};


// ---------------------------------------------------------
// Page Component
// ---------------------------------------------------------


const HomePage: React.FC = () => {
 const { scrollY } = useScroll();
 const navigate = useNavigate();
 
 // Theme Toggle with LocalStorage for persistence
 const [isDark, setIsDark] = useState(() => {
  const savedTheme = localStorage.getItem('theme');
  if (savedTheme) {
   return savedTheme === 'dark';
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
 });

 useEffect(() => {
  if (isDark) {
   document.documentElement.classList.add('dark');
   localStorage.setItem('theme', 'dark');
  } else {
   document.documentElement.classList.remove('dark');
   localStorage.setItem('theme', 'light');
  }
 }, [isDark]);

 const navBackground = useTransform(scrollY, [0, 50], [isDark ? 'rgba(3, 7, 18, 0)' : 'rgba(252, 252, 253, 0)', isDark ? 'rgba(3, 7, 18, 0.9)' : 'rgba(252, 252, 253, 0.9)']);
 const navBackdrop = useTransform(scrollY, [0, 50], ['blur(0px)', 'blur(12px)']);
 const navBorder = useTransform(scrollY, [0, 50], [isDark ? 'rgba(31, 41, 55, 0)' : 'rgba(229, 231, 235, 0)', isDark ? 'rgba(31, 41, 55, 1)' : 'rgba(229, 231, 235, 1)']);
 
 const heroOpacity = useTransform(scrollY, [0, 300], [1, 0]);
 const heroY = useTransform(scrollY, [0, 300], [0, -50]);

 type ChatMessage = {
  role: 'user' | 'assistant';
  content: string;
  action?: any;
  intent?: string;
 };

 const [prompt, setPrompt] = useState('');
 const [isGenerating, setIsGenerating] = useState(false);
 const [chatHistory, setChatHistory] = useState<ChatMessage[]>([]);
 const [isChatOpen, setIsChatOpen] = useState(false);
 const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

 const handleGenerate = async (e: React.FormEvent) => {
  e.preventDefault();
  if (!prompt) return;
  setIsGenerating(true);
  const currentPrompt = prompt;
  setPrompt(''); // clear input immediately
  
  // Add user message to UI history
  setChatHistory(prev => [...prev, { role: 'user', content: currentPrompt }]);
  
  try {
   const historyToSend = chatHistory.map(msg => ({ role: msg.role, content: msg.content }));
   const { data } = await apiClient.post('/assistant/interpret', { 
    message: currentPrompt,
    history: historyToSend 
   });
   if (data.reply) {
    setChatHistory(prev => [...prev, { 
     role: 'assistant', 
     content: data.reply,
     action: data.action,
     intent: data.intent
    }]);
   }
  } catch (err) {
   console.error('Assistant error:', err);
   setChatHistory(prev => [...prev, { role: 'assistant', content: 'Failed to interpret message.' }]);
  } finally {
   setIsGenerating(false);
  }
 };

 
 return (
  <div className={`font-sans selection:bg-gray-900 dark:selection:bg-surface selection:text-text-primary dark:selection:text-gray-900 min-h-screen relative overflow-x-hidden bg-background transition-colors duration-300`}>
   
   {/* 1. NAVIGATION */}
   <motion.nav 
    style={{ backgroundColor: navBackground, backdropFilter: navBackdrop, borderBottomColor: navBorder }}
    className="fixed top-0 w-full z-50 transition-all border-b border-transparent"
   >
    <div className="max-w-[1600px] mx-auto px-8 h-24 flex items-center justify-between">
     <div className="flex items-center gap-3">
      <Link to="/" className="flex items-center gap-3 hover:opacity-80 transition-opacity">
       <div className="w-8 h-8 relative flex items-center justify-center">
        <Box className="absolute text-gray-900 dark:text-text-primary transition-colors" size={24} strokeWidth={1.5} />
        <Sparkles className="absolute text-yellow-600 -top-1 -right-1" size={12} />
       </div>
       <span className="text-sm font-bold text-gray-900 dark:text-text-primary tracking-[0.2em] transition-colors">HOMEPLANNER<span className="text-text-secondary">AI</span></span>
      </Link>
     </div>
     
     <div className="hidden lg:flex items-center gap-10 text-[11px] font-semibold tracking-[0.15em] text-text-muted text-text-secondary">
      <Link to="/" className="text-gray-900 dark:text-text-primary hover:text-blue-600 dark:hover:text-blue-400 transition-colors">HOME</Link>
      <Link to="/features" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">FEATURES</Link>
      <Link to="/gallery" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">GALLERY</Link>
     </div>

     <div className="flex items-center gap-4 sm:gap-6">
      <button onClick={() => setIsDark(!isDark)} className="text-text-secondary hover:text-gray-900 dark:hover:text-text-primary transition-colors">
       {isDark ? <Sun size={20} /> : <Moon size={20} />}
      </button>
      <Link to="/login" className="hidden sm:block text-[11px] font-bold tracking-[0.15em] text-gray-900 dark:text-text-primary hover:text-text-secondary dark:hover:text-gray-300 transition-colors">
       SIGN IN
      </Link>
      <Link 
       to="/login" 
       className="hidden sm:flex bg-gray-900 dark:bg-surface hover:bg-black dark:hover:bg-gray-200 text-text-primary dark:text-gray-900 px-7 py-3.5 rounded-none text-[11px] font-bold tracking-[0.15em] transition-all"
      >
       START DESIGNING
      </Link>
      <button 
       className="lg:hidden text-gray-900 dark:text-text-primary"
       onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
      >
       {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
      </button>
     </div>
    </div>
   </motion.nav>

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

   {/* MAIN HERO */}
   <section className="relative min-h-[100vh] w-full flex flex-col lg:flex-row items-center pt-28 pb-12 lg:pt-0 lg:pb-0">
    
    {/* 3D Canvas on the Right (60% width) */}
    <div className="relative w-full h-[50vh] lg:absolute lg:inset-0 lg:z-0 lg:left-[40%] lg:w-[60%] lg:h-full pointer-events-auto order-2 lg:order-none">
     <Canvas shadows camera={{ position: [20, 10, 20], fov: 35 }} className="w-full h-full cursor-grab active:cursor-grabbing">
      <HouseScene isDark={isDark} />
      <OrbitControls 
       enableZoom={false} 
       enablePan={false} 
       maxPolarAngle={Math.PI / 2 - 0.05} 
       minPolarAngle={Math.PI / 4} 
       minAzimuthAngle={-Math.PI / 4}
       maxAzimuthAngle={Math.PI / 2}
      />
     </Canvas>
     
     {/* Subtle gradient overlay to blend 3D with background on the left */}
     <div className={`absolute inset-0 bg-gradient-to-r ${isDark ? 'from-gray-950 via-gray-950/70' : 'from-[#fcfcfd] via-[#fcfcfd]/70'} to-transparent pointer-events-none w-1/3 hidden lg:block transition-colors duration-300`}></div>
    </div>

    {/* Hero Content on the Left (40% width) */}
    <motion.div 
     style={{ opacity: heroOpacity, y: heroY }}
     className="relative z-10 w-full max-w-[1600px] mx-auto px-6 sm:px-8 pointer-events-none flex flex-col justify-center h-full order-1 lg:order-none mt-10 lg:mt-0"
    >
     <div className="w-full lg:max-w-[45%] pointer-events-auto bg-background/70 bg-background/70 lg:bg-transparent lg:dark:bg-transparent backdrop-blur-md lg:backdrop-blur-none p-6 lg:p-0 rounded-3xl lg:rounded-none">
      <div className="inline-block mb-6">
       <p className="text-[10px] font-bold tracking-[0.2em] text-text-muted text-text-secondary flex items-center gap-2">
        <span className="w-8 h-[1px] bg-gray-300 dark:bg-gray-700 transition-colors"></span>
        AI-ASSISTED HOME PLANNING
       </p>
      </div>
      
      <h1 className="text-4xl sm:text-5xl lg:text-[4.2rem] font-medium text-gray-900 dark:text-text-primary tracking-tight leading-[1.1] lg:leading-[1.05] mb-6 font-serif transition-colors">
       Design Smarter.<br/>
       <span className="relative inline-block text-gray-800 dark:text-gray-300 transition-colors">
        Build with Confidence.
       </span>
      </h1>
      
      <p className="text-base sm:text-lg text-text-muted text-text-secondary mb-10 leading-relaxed font-light lg:pr-10 transition-colors">
       HomePlannerAI helps you validate land and home requirements, explore suitable design options, collaborate with architects, and move into construction planning.
      </p>
      
      <div className="flex flex-col sm:flex-row gap-4 mb-4">
       <Link to="/login" className="bg-gray-900 dark:bg-surface hover:bg-black dark:hover:bg-gray-200 text-text-primary dark:text-gray-900 px-8 py-4 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center gap-3 w-max rounded-xl lg:rounded-none">
        START DESIGNING <ArrowRight size={16} />
       </Link>
       <button onClick={() => setIsChatOpen(true)} className="bg-transparent border border-border-strong border-border hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-900 dark:text-text-primary px-8 py-4 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center gap-3 w-max rounded-xl lg:rounded-none">
        ASK AI ARCHITECT <MessageSquare size={16} />
       </button>
      </div>
      <p className="text-xs text-text-secondary text-text-muted lg:max-w-sm ml-2">
       Ask about land size, bedroom count, layout suitability, and planning ideas.
      </p>
     </div>
    </motion.div>
   </section>

   {/* SECTION 2 - AI ARCHITECT ASSISTANT */}
   <section className="py-24 bg-surface relative z-10 transition-colors duration-300 border-t border-gray-100 dark:border-border-strong">
    <div className="max-w-[1400px] mx-auto px-6 grid lg:grid-cols-2 gap-16 items-center">
     <div>
      <p className="text-[10px] font-bold tracking-[0.2em] text-blue-600 dark:text-blue-400 mb-4 transition-colors">AI ARCHITECT ASSISTANT</p>
      <h2 className="text-3xl sm:text-4xl lg:text-5xl font-medium text-gray-900 dark:text-text-primary tracking-tight font-serif mb-6 transition-colors">
       Talk to the AI Architect Before You Design
      </h2>
      <p className="text-base sm:text-lg text-text-muted text-text-secondary mb-8 leading-relaxed font-light transition-colors">
       Ask planning questions about land size, room counts, layout ideas, or construction-related decisions before starting a full design workflow.
      </p>
      <button onClick={() => setIsChatOpen(true)} className="bg-blue-600 hover:bg-blue-700 text-text-primary px-8 py-4 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center gap-3 w-max rounded-xl shadow-lg shadow-blue-900/20">
       OPEN AI ARCHITECT <MessageSquare size={16} />
      </button>
     </div>
     
     <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div onClick={() => { setPrompt("I have 25 perch land. How many bedrooms are suitable?"); setIsChatOpen(true); }} className="bg-gray-50 bg-surface-elevated/50 p-6 rounded-2xl border border-gray-100 dark:border-border-strong cursor-pointer hover:border-blue-300 dark:hover:border-blue-800 transition-colors group">
       <MessageSquare className="text-text-secondary mb-4 group-hover:text-blue-500 transition-colors" size={20} />
       <p className="text-sm text-gray-700 dark:text-gray-300 font-medium">"I have 25 perch land. How many bedrooms are suitable?"</p>
      </div>
      <div onClick={() => { setPrompt("Is 2 bedrooms and 12 bathrooms realistic?"); setIsChatOpen(true); }} className="bg-gray-50 bg-surface-elevated/50 p-6 rounded-2xl border border-gray-100 dark:border-border-strong cursor-pointer hover:border-blue-300 dark:hover:border-blue-800 transition-colors group">
       <MessageSquare className="text-text-secondary mb-4 group-hover:text-blue-500 transition-colors" size={20} />
       <p className="text-sm text-gray-700 dark:text-gray-300 font-medium">"Is 2 bedrooms and 12 bathrooms realistic?"</p>
      </div>
      <div onClick={() => { setPrompt("What house layout fits a narrow plot?"); setIsChatOpen(true); }} className="bg-gray-50 bg-surface-elevated/50 p-6 rounded-2xl border border-gray-100 dark:border-border-strong cursor-pointer hover:border-blue-300 dark:hover:border-blue-800 transition-colors group">
       <MessageSquare className="text-text-secondary mb-4 group-hover:text-blue-500 transition-colors" size={20} />
       <p className="text-sm text-gray-700 dark:text-gray-300 font-medium">"What house layout fits a narrow plot?"</p>
      </div>
      <div onClick={() => { setPrompt("Can I build a 2-floor home on this site?"); setIsChatOpen(true); }} className="bg-gray-50 bg-surface-elevated/50 p-6 rounded-2xl border border-gray-100 dark:border-border-strong cursor-pointer hover:border-blue-300 dark:hover:border-blue-800 transition-colors group">
       <MessageSquare className="text-text-secondary mb-4 group-hover:text-blue-500 transition-colors" size={20} />
       <p className="text-sm text-gray-700 dark:text-gray-300 font-medium">"Can I build a 2-floor home on this site?"</p>
      </div>
     </div>
    </div>
   </section>

   {/* SECTION 3 - CAPABILITIES GRID */}
   <section className="py-24 bg-background relative z-10 transition-colors duration-300 border-t border-gray-100 dark:border-border-strong">
    <div className="max-w-[1400px] mx-auto px-6">
     <div className="mb-16 text-center">
      <h2 className="text-3xl sm:text-4xl lg:text-5xl font-medium text-gray-900 dark:text-text-primary tracking-tight font-serif transition-colors">
       What HomePlannerAI Helps You Do
      </h2>
     </div>
     
     <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 lg:gap-8">
      <div className="bg-surface border border-border dark:border-border-strong p-8 rounded-3xl hover:border-blue-500/50 dark:hover:border-blue-500/50 transition-colors group">
       <Map className="text-blue-600 dark:text-blue-400 mb-6 group-hover:scale-110 transition-transform" size={32} />
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Land Analysis</h3>
       <p className="text-sm text-text-muted text-text-secondary leading-relaxed">Capture site dimensions, land size, and planning context to understand what is realistically possible.</p>
      </div>
      
      <div className="bg-surface border border-border dark:border-border-strong p-8 rounded-3xl hover:border-indigo-500/50 dark:hover:border-indigo-500/50 transition-colors group">
       <ClipboardCheck className="text-indigo-600 dark:text-indigo-400 mb-6 group-hover:scale-110 transition-transform" size={32} />
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Requirement Validation</h3>
       <p className="text-sm text-text-muted text-text-secondary leading-relaxed">Check whether requested bedrooms, bathrooms, floors, and features are suitable for the land.</p>
      </div>
      
      <div className="bg-surface border border-border dark:border-border-strong p-8 rounded-3xl hover:border-purple-500/50 dark:hover:border-purple-500/50 transition-colors group">
       <Home className="text-purple-600 dark:text-purple-400 mb-6 group-hover:scale-110 transition-transform" size={32} />
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">AI-Assisted Design</h3>
       <p className="text-sm text-text-muted text-text-secondary leading-relaxed">Explore home design options that align with validated land and requirement inputs.</p>
      </div>
      
      <div className="bg-surface border border-border dark:border-border-strong p-8 rounded-3xl hover:border-amber-500/50 dark:hover:border-amber-500/50 transition-colors group">
       <PenTool className="text-amber-600 dark:text-amber-400 mb-6 group-hover:scale-110 transition-transform" size={32} />
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Architect Review</h3>
       <p className="text-sm text-text-muted text-text-secondary leading-relaxed">Send selected designs for review and track approval status through the workflow.</p>
      </div>
      
      <div className="bg-surface border border-border dark:border-border-strong p-8 rounded-3xl hover:border-rose-500/50 dark:hover:border-rose-500/50 transition-colors group">
       <HardHat className="text-rose-600 dark:text-rose-400 mb-6 group-hover:scale-110 transition-transform" size={32} />
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Constructor Workflow</h3>
       <p className="text-sm text-text-muted text-text-secondary leading-relaxed">Request construction support and manage project progress after approval.</p>
      </div>
      
      <div className="bg-surface border border-border dark:border-border-strong p-8 rounded-3xl hover:border-emerald-500/50 dark:hover:border-emerald-500/50 transition-colors group">
       <Activity className="text-emerald-600 dark:text-emerald-400 mb-6 group-hover:scale-110 transition-transform" size={32} />
       <h3 className="text-xl font-bold text-gray-900 dark:text-text-primary mb-3">Progress Tracking</h3>
       <p className="text-sm text-text-muted text-text-secondary leading-relaxed">Follow phases, daily logs, and schedule updates during construction.</p>
      </div>
     </div>
    </div>
   </section>

   {/* SECTION 4 - HOW IT WORKS */}
   <section className="py-24 bg-surface relative z-10 transition-colors duration-300">
    <div className="max-w-[1400px] mx-auto px-6">
     <div className="mb-16 text-center">
      <h2 className="text-3xl sm:text-4xl lg:text-5xl font-medium text-gray-900 dark:text-text-primary tracking-tight font-serif transition-colors">
       How It Works
      </h2>
     </div>
     
     <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-6 relative">
      <div className="hidden lg:block absolute top-12 left-[10%] right-[10%] h-[1px] bg-gray-200 bg-surface-elevated z-0"></div>
      
      {[
       { num: '01', title: 'Enter Land Details', desc: 'Provide land size, dimensions, and site context.' },
       { num: '02', title: 'Add House Requirements', desc: 'Tell us the bedrooms, bathrooms, floors, and features you want.' },
       { num: '03', title: 'Validate Feasibility', desc: 'The system checks whether the request is suitable.' },
       { num: '04', title: 'Explore Design Options', desc: 'View AI-assisted design options matched to your needs.' },
       { num: '05', title: 'Get Architect Approval', desc: 'Send a chosen design for architect review.' },
       { num: '06', title: 'Start Construction Tracking', desc: 'Move into construction requests, phases, and daily tracking.' }
      ].map((step, i) => (
       <div key={i} className="relative z-10 flex flex-col items-center text-center">
        <div className="w-24 h-24 bg-background border-4 border-white dark:border-gray-900 rounded-full flex items-center justify-center shadow-lg mb-6 text-gray-300 dark:text-gray-700 text-3xl font-bold font-serif">
         {step.num}
        </div>
        <h4 className="text-sm font-bold text-gray-900 dark:text-text-primary mb-3 tracking-wide">{step.title}</h4>
        <p className="text-xs text-text-muted text-text-secondary max-w-[150px] leading-relaxed">{step.desc}</p>
       </div>
      ))}
     </div>
    </div>
   </section>

   {/* SECTION 5 - VALIDATION / TRUST */}
   <section className="py-24 bg-gray-50 bg-surface-elevated/50 relative z-10 transition-colors duration-300">
    <div className="max-w-[1000px] mx-auto px-6 text-center">
     <h2 className="text-3xl lg:text-4xl font-medium text-gray-900 dark:text-text-primary tracking-tight font-serif mb-6 transition-colors">
      AI Guidance with Smart Validation
     </h2>
     <p className="text-lg text-text-secondary mb-12 leading-relaxed max-w-3xl mx-auto transition-colors">
      HomePlannerAI does not generate blindly. It helps assess whether a request is suitable for the land and planning context before moving users into design workflows.
     </p>
     
     <div className="flex flex-col sm:flex-row justify-center gap-6 sm:gap-12 mb-16">
      <div className="flex items-center justify-center gap-3 text-gray-900 dark:text-text-primary font-medium bg-surface px-6 py-3 rounded-full border border-border dark:border-border-strong shadow-sm">
       <ShieldCheck className="text-blue-500" size={20} />
       <span className="text-sm">Feasibility-aware guidance</span>
      </div>
      <div className="flex items-center justify-center gap-3 text-gray-900 dark:text-text-primary font-medium bg-surface px-6 py-3 rounded-full border border-border dark:border-border-strong shadow-sm">
       <CheckCircle2 className="text-indigo-500" size={20} />
       <span className="text-sm">Requirement sanity checks</span>
      </div>
      <div className="flex items-center justify-center gap-3 text-gray-900 dark:text-text-primary font-medium bg-surface px-6 py-3 rounded-full border border-border dark:border-border-strong shadow-sm">
       <PenTool className="text-amber-500" size={20} />
       <span className="text-sm">Architect review support</span>
      </div>
     </div>
     
     <p className="text-xs text-text-secondary text-text-muted max-w-2xl mx-auto italic transition-colors">
      HomePlannerAI supports conceptual planning and workflow management. Final architectural, structural, and regulatory decisions must be confirmed by qualified professionals.
     </p>
    </div>
   </section>

   {/* SECTION 6 - CTA */}
   <section className="py-32 bg-gray-900 text-text-primary relative overflow-hidden">
    <div className="absolute inset-0 opacity-10 pointer-events-none bg-[url('https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?q=80&w=2000&auto=format&fit=crop')] bg-cover bg-center mix-blend-overlay"></div>
    <div className="max-w-[1400px] mx-auto px-6 relative z-10 text-center flex flex-col items-center">
     <h2 className="text-4xl lg:text-6xl font-medium tracking-tight mb-12 font-serif">
      Ready to start planning your home?
     </h2>
     <div className="flex flex-col sm:flex-row gap-4">
      <Link to="/login" className="bg-surface hover:bg-gray-100 text-gray-900 px-10 py-5 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center gap-3 w-max">
       START NEW PROJECT <ArrowRight size={16} />
      </Link>
      <Link to="/gallery" className="bg-transparent border border-gray-600 hover:border-white text-text-primary px-10 py-5 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center gap-3 w-max">
       BROWSE PLANS
      </Link>
      <button onClick={() => setIsChatOpen(true)} className="bg-transparent border border-gray-600 hover:border-white text-text-primary px-10 py-5 text-xs font-bold tracking-[0.15em] transition-all flex items-center justify-center gap-3 w-max">
       ASK AI ARCHITECT <MessageSquare size={16} />
      </button>
     </div>
    </div>
   </section>

   {/* FOOTER */}
   <footer className="bg-surface bg-background border-t border-gray-100 dark:border-border-strong pt-20 pb-10 transition-colors duration-300">
    <div className="max-w-[1400px] mx-auto px-6">
     <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-10 mb-16">
      <div className="col-span-2 lg:col-span-2">
       <div className="flex items-center gap-2 mb-6">
        <Box className="text-gray-900 dark:text-text-primary transition-colors" size={20} strokeWidth={2} />
        <span className="text-sm font-bold text-gray-900 dark:text-text-primary tracking-[0.2em] transition-colors">HOMEPLANNER<span className="text-text-secondary">AI</span></span>
       </div>
       <p className="text-text-muted text-text-secondary text-sm leading-relaxed max-w-sm mb-6 transition-colors">
        The most advanced AI architecture platform. Design, visualize, and plan your perfect home with precision and ease.
       </p>
      </div>
      
      <div>
       <h4 className="font-bold text-gray-900 dark:text-text-primary mb-4 text-sm tracking-wider uppercase transition-colors">Product</h4>
       <ul className="space-y-3 text-sm text-text-muted text-text-secondary">
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Features</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">AI Floor Plans</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">3D Visualization</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Pricing</a></li>
       </ul>
      </div>

      <div>
       <h4 className="font-bold text-gray-900 dark:text-text-primary mb-4 text-sm tracking-wider uppercase transition-colors">Resources</h4>
       <ul className="space-y-3 text-sm text-text-muted text-text-secondary">
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Design Gallery</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Architecture Blog</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Help Center</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Community</a></li>
       </ul>
      </div>

      <div>
       <h4 className="font-bold text-gray-900 dark:text-text-primary mb-4 text-sm tracking-wider uppercase transition-colors">Company</h4>
       <ul className="space-y-3 text-sm text-text-muted text-text-secondary">
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">About Us</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Careers</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Contact</a></li>
        <li><a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors">Privacy Policy</a></li>
       </ul>
      </div>
     </div>
     
     <div className="pt-8 border-t border-gray-100 dark:border-border-strong flex flex-col md:flex-row items-center justify-between gap-4 transition-colors">
      <p className="text-xs text-text-secondary">© 2026 HomePlanner AI. Developed by Team Slytherin with ❤️.</p>
      <div className="flex items-center gap-4 text-text-secondary">
       <a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"></path></svg></a>
       <a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 4s-.7 2.1-2 3.4c1.6 10-9.4 17.3-18 11.6 2.2.1 4.4-.6 6-2C3 15.5.5 9.6 3 5c2.2 2.6 5.6 4.1 9 4-.9-4.2 4-6.6 7-3.8 1.1 0 3-1.2 3-1.2z"></path></svg></a>
       <a href="#" className="hover:text-gray-900 dark:hover:text-text-primary transition-colors"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="2" y="2" width="20" height="20" rx="5" ry="5"></rect><path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"></path><line x1="17.5" y1="6.5" x2="17.51" y2="6.5"></line></svg></a>
      </div>
     </div>
    </div>
   </footer>
   
   {/* FLOATING CHAT WIDGET */}
   <AnimatePresence>
    {isChatOpen && (
     <motion.div
      initial={{ opacity: 0, y: 20, scale: 0.95 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, y: 20, scale: 0.95 }}
      transition={{ duration: 0.2 }}
      className="fixed bottom-6 right-6 z-50 w-[420px] max-w-[calc(100vw-2rem)] bg-surface/95 bg-surface/95 backdrop-blur-xl border border-border dark:border-border-strong rounded-2xl shadow-2xl flex flex-col overflow-hidden"
     >
      {/* Header */}
      <div className="flex items-center justify-between px-5 py-4 bg-gray-50/80 bg-surface-elevated/80 border-b border-gray-100 dark:border-border-strong">
       <div className="flex items-center gap-2">
        <Sparkles size={14} className="text-yellow-600" />
        <span className="text-[10px] font-bold tracking-[0.2em] text-gray-700 dark:text-gray-300">AI ARCHITECT</span>
       </div>
       <div className="flex items-center gap-4">
        {chatHistory.length > 0 && (
         <button type="button" onClick={() => setChatHistory([])} className="text-[10px] uppercase font-bold tracking-wider text-text-muted hover:text-gray-900 dark:hover:text-text-primary transition-colors">
          Clear
         </button>
        )}
        <button type="button" onClick={() => setIsChatOpen(false)} className="text-text-secondary hover:text-gray-700 dark:hover:text-gray-200 transition-colors">
         <X size={18} />
        </button>
       </div>
      </div>

      {/* Chat History Area */}
      <div className="px-5 py-4 h-[400px] overflow-y-auto flex flex-col gap-4 scrollbar-thin relative">
       <AnimatePresence>
        {isGenerating && (
         <motion.div 
          initial={{ left: '-100%' }}
          animate={{ left: '200%' }}
          transition={{ duration: 1.5, ease: "linear", repeat: Infinity }}
          className="absolute top-0 bottom-0 w-1/2 bg-gradient-to-r from-transparent via-blue-400/10 dark:via-blue-500/10 to-transparent z-0 pointer-events-none skew-x-12"
         />
        )}
       </AnimatePresence>

       {chatHistory.length === 0 ? (
        <div className="flex-1 flex flex-col items-center justify-center text-center opacity-70 my-8 relative z-10">
         <MessageSquare size={32} className="mb-3 text-text-secondary" />
         <p className="text-sm text-text-secondary">Describe your dream home or ask any architectural question to get started.</p>
        </div>
       ) : (
        chatHistory.map((msg, idx) => (
         <div key={idx} className={`flex flex-col relative z-10 ${msg.role === 'user' ? 'items-end' : 'items-start'}`}>
          <div className={`px-4 py-3 rounded-2xl max-w-[90%] text-sm shadow-sm ${msg.role === 'user' ? 'bg-black dark:bg-surface text-text-primary dark:text-black rounded-br-sm' : 'bg-gray-100 bg-surface-elevated text-black dark:text-text-primary rounded-bl-sm border border-border'}`}>
           <p className="whitespace-pre-wrap leading-relaxed">
            {msg.content}
           </p>
          </div>
          
          {/* Render actions if assistant message */}
          {msg.role === 'assistant' && msg.action && (
           <div className="mt-3 w-full pl-2">
            {msg.action.type === 'CONTINUE_TO_DESIGN' && (
             <div className="p-4 bg-indigo-50 dark:bg-indigo-900/30 rounded-xl border border-indigo-100 dark:border-indigo-800 max-w-[90%]">
              <p className="font-semibold text-indigo-900 dark:text-indigo-200 mb-3 text-sm">
               Your request is feasible. I can start the design setup with these requirements.
              </p>
              <button 
               onClick={() => navigate('/dashboard/new-project', { state: { prefill: msg.action.payload.requirements } })}
               className="bg-indigo-600 hover:bg-indigo-700 text-text-primary px-5 py-2.5 rounded-lg font-bold text-xs tracking-wider transition-colors shadow-sm"
              >
               Continue to Design
              </button>
             </div>
            )}

            {msg.intent === 'DESIGN_REQUEST' && msg.action.type === 'NONE' && msg.action.payload?.feasibility && !msg.action.payload.feasibility.can_proceed && (
             <div className="p-4 bg-amber-50 dark:bg-amber-900/30 rounded-xl border border-amber-200 dark:border-amber-800 max-w-[90%]">
              <p className="font-semibold text-amber-900 dark:text-amber-200 mb-2 text-sm">
               Your request is not supported with the available buildable area or catalogue.
              </p>
              {msg.action.payload.feasibility.suggestions?.length > 0 && (
               <ul className="list-disc list-inside text-amber-800 dark:text-amber-300 text-xs space-y-1.5 mt-2">
                {msg.action.payload.feasibility.suggestions.map((s: string, i: number) => (
                 <li key={i}>{s}</li>
                ))}
               </ul>
              )}
             </div>
            )}
           </div>
          )}
         </div>
        ))
       )}
      </div>

      {/* Input Form */}
      <form onSubmit={handleGenerate} className="flex items-center gap-2 p-3 bg-surface border-t border-gray-100 dark:border-border-strong relative z-10">
       <input
        type="text"
        value={prompt}
        onChange={(e) => setPrompt(e.target.value)}
        placeholder="Message AI Architect..."
        className="flex-1 bg-gray-50 bg-surface-elevated border border-border rounded-xl focus:ring-2 focus:ring-black dark:focus:ring-white focus:border-transparent text-black dark:text-text-primary placeholder-gray-500 dark:placeholder-gray-400 px-4 py-3 outline-none text-sm transition-all"
       />
       <button 
        type="submit"
        disabled={isGenerating || !prompt}
        className="bg-black dark:bg-surface hover:bg-gray-800 dark:hover:bg-gray-200 text-text-primary dark:text-black w-12 h-12 flex-shrink-0 rounded-xl flex items-center justify-center transition-colors disabled:opacity-50 shadow-sm"
       >
        {isGenerating ? <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin"></div> : <ArrowRight size={18} />}
       </button>
      </form>
     </motion.div>
    )}
   </AnimatePresence>

   {/* FLOATING ACTION BUTTON (FAB) when chat is closed */}
   <AnimatePresence>
    {!isChatOpen && (
     <motion.button
      initial={{ scale: 0 }}
      animate={{ scale: 1 }}
      exit={{ scale: 0 }}
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
      onClick={() => setIsChatOpen(true)}
      className="fixed bottom-6 right-6 z-50 w-16 h-16 bg-black dark:bg-surface text-text-primary dark:text-black rounded-full shadow-2xl flex items-center justify-center"
     >
      <MessageSquare size={24} />
     </motion.button>
    )}
   </AnimatePresence>

  </div>
 );
};

export default HomePage;
