import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { motion } from 'framer-motion';
import { Box, Lock, Mail, ArrowRight, Sparkles } from 'lucide-react';
import { loginAsync } from '../features/auth/authSlice';
import type { AppDispatch } from '../store';
import useAuth from '../features/auth/useAuth';
import { roleHomePath } from '../utils/roleNavigation';

const LoginPage: React.FC = () => {
 const [isHovered, setIsHovered] = useState(false);
 const [email, setEmail] = useState('');
 const [password, setPassword] = useState('');
 const [error, setError] = useState('');
 const navigate = useNavigate();
 const dispatch = useDispatch<AppDispatch>();
 const { isAuthenticated, user } = useAuth();

 useEffect(() => {
  if (isAuthenticated && user) {
   navigate(roleHomePath(user.role));
  }
 }, [isAuthenticated, user, navigate]);

 const handleLogin = async (e: React.FormEvent) => {
  e.preventDefault();
  setError('');

  const result = await dispatch(loginAsync({email,password}));
  if (loginAsync.fulfilled.match(result)) {
   navigate(roleHomePath(result.payload.user.role));
  } else {
   const errorMsg = (result.payload as string) || 'Invalid email or password.';
   if (errorMsg === 'registration_required' || errorMsg.includes('registration_required')) {
    // Supabase identity exists, but its application profile was not created yet.
    navigate('/register');
   } else {
    setError(errorMsg);
   }
  }
 };


 return (
  <div className="min-h-screen bg-background flex items-center justify-center p-6 relative overflow-hidden transition-colors">
   
   {/* Decorative Background Elements */}
   <div className="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none">
    <div className="absolute -top-[10%] -right-[5%] w-[40%] h-[40%] rounded-full bg-blue-100/50 dark:bg-blue-900/20 blur-3xl"></div>
    <div className="absolute top-[60%] -left-[10%] w-[30%] h-[30%] rounded-full bg-indigo-100/40 dark:bg-indigo-900/20 blur-3xl"></div>
   </div>

   <Link to="/" className="absolute top-8 left-8 flex items-center gap-3 hover:opacity-80 transition-opacity z-10">
    <div className="w-8 h-8 relative flex items-center justify-center">
     <Box className="absolute text-text-primary transition-colors" size={24} strokeWidth={1.5} />
     <Sparkles className="absolute text-yellow-600 -top-1 -right-1" size={12} />
    </div>
    <span className="text-sm font-bold text-text-primary tracking-[0.2em] transition-colors">HOMEPLANNER<span className="text-text-secondary">AI</span></span>
   </Link>

   <motion.div 
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.5 }}
    className="w-full max-w-md relative z-10"
   >
    <div className="bg-surface rounded-3xl p-8 sm:p-10 custom-shadow-xl border border-gray-100 dark:border-border-strong relative overflow-hidden">
     
     <div className="text-center mb-10">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Welcome Back</h1>
      <p className="text-sm text-text-secondary">Sign in to access your HousePlanner workspace.</p>
     </div>

     <form onSubmit={handleLogin} className="space-y-6">
      
      {error && (
       <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl text-sm text-red-600 dark:text-red-400 text-center">
        {error}
       </div>
      )}

      <div className="space-y-2">
       <label className="text-sm font-medium text-text-secondary ml-1">Email Address</label>
       <div className="relative">
        <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
         <Mail className="h-5 w-5 text-text-secondary" />
        </div>
        <input 
         type="email" 
         value={email}
         onChange={(e) => setEmail(e.target.value)}
         required
         className="block w-full pl-11 pr-4 py-3.5 bg-surface-elevated border border-border rounded-xl text-text-primary focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all outline-none"
         placeholder="name@example.com"
        />
       </div>
      </div>

      <div className="space-y-2">
       <div className="flex items-center justify-between ml-1">
        <label className="text-sm font-medium text-text-secondary">Password</label>
        <a href="#" className="text-xs font-medium text-blue-600 hover:text-blue-700 dark:text-blue-400">Forgot password?</a>
       </div>
       <div className="relative">
        <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
         <Lock className="h-5 w-5 text-text-secondary" />
        </div>
        <input 
         type="password" 
         value={password}
         onChange={(e) => setPassword(e.target.value)}
         required
         className="block w-full pl-11 pr-4 py-3.5 bg-surface-elevated border border-border rounded-xl text-text-primary focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all outline-none"
         placeholder="••••••••"
        />
       </div>
      </div>

      <button 
       type="submit"
       onMouseEnter={() => setIsHovered(true)}
       onMouseLeave={() => setIsHovered(false)}
       className="w-full bg-gray-900 dark:bg-white hover:bg-black dark:hover:bg-gray-200 text-white dark:text-gray-900 font-bold py-4 rounded-xl flex items-center justify-center gap-2 transition-all custom-shadow-md group mt-8"
      >
       Sign In
       <motion.div
        animate={{ x: isHovered ? 4 : 0 }}
        transition={{ duration: 0.2 }}
       >
        <ArrowRight size={18} />
       </motion.div>
      </button>
     </form>


     <div className="mt-8 text-center text-sm text-text-muted">
      Don't have an account?{' '}
      <Link to="/register" className="font-semibold text-blue-600 hover:text-blue-700 dark:text-blue-400">Create one</Link>
     </div>
    </div>
   </motion.div>
  </div>
 );
};

export default LoginPage;
