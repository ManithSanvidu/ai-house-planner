import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { ArrowRight, Box, Lock, Mail, Sparkles, User } from 'lucide-react';
import { googleLoginAsync, registerAsync } from '../features/auth/authSlice';
import type { AppDispatch } from '../store';
import useAuth from '../features/auth/useAuth';
import { roleHomePath } from '../utils/roleNavigation';

const RegisterPage: React.FC = () => {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const { isAuthenticated, user } = useAuth();

  useEffect(() => { if (isAuthenticated && user) navigate(roleHomePath(user.role)); }, [isAuthenticated, user, navigate]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    if (password !== confirmPassword) return setError('Passwords do not match.');
    if (password.length < 6) return setError('Password must be at least 6 characters.');
    const result = await dispatch(registerAsync({ email, password, fullName }));
    if (registerAsync.fulfilled.match(result)) navigate('/dashboard');
    else setError((result.payload as string) || 'Registration failed. Please try again.');
  };

  const continueWithGoogle = async () => {
    setError('');
    const result = await dispatch(googleLoginAsync());
    if (googleLoginAsync.fulfilled.match(result)) navigate(roleHomePath(result.payload.user.role));
    else setError((result.payload as string) || 'Google sign-in failed.');
  };

  const fieldClass = 'block w-full pl-11 pr-4 py-3.5 bg-gray-50 dark:bg-gray-950 border border-gray-200 dark:border-gray-800 rounded-xl text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-600 focus:border-transparent outline-none';
  return <div className="min-h-screen bg-[#fcfcfd] dark:bg-gray-950 flex items-center justify-center p-6 relative overflow-hidden">
    <div className="absolute inset-0 pointer-events-none"><div className="absolute -top-[10%] -right-[5%] w-[40%] h-[40%] rounded-full bg-violet-100/50 dark:bg-violet-900/20 blur-3xl"/><div className="absolute top-[60%] -left-[10%] w-[30%] h-[30%] rounded-full bg-blue-100/40 dark:bg-blue-900/20 blur-3xl"/></div>
    <Link to="/" className="absolute top-8 left-8 flex items-center gap-3 z-10"><div className="w-8 h-8 relative flex items-center justify-center"><Box size={24}/><Sparkles className="absolute text-yellow-600 -top-1 -right-1" size={12}/></div><span className="text-sm font-bold tracking-[0.2em]">HOMEPLANNER<span className="text-gray-400">AI</span></span></Link>
    <main className="w-full max-w-md relative z-10 bg-white dark:bg-gray-900 rounded-3xl p-8 sm:p-10 custom-shadow-xl border border-gray-100 dark:border-gray-800">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Create your HousePlanner account</h1>
      <p className="text-sm text-gray-500 dark:text-gray-400 mt-2 mb-7">Create a customer account to explore plans and manage your home design.</p>
      {error && <div role="alert" className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl text-sm text-red-600 dark:text-red-400 text-center">{error}</div>}
      <form onSubmit={submit} className="space-y-5">
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Full Name<div className="relative mt-1.5"><User className="absolute left-4 top-3.5 text-gray-400" size={20}/><input id="reg-fullname" required autoComplete="name" value={fullName} onChange={e=>setFullName(e.target.value)} className={fieldClass} placeholder="Kasun Perera"/></div></label>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Email Address<div className="relative mt-1.5"><Mail className="absolute left-4 top-3.5 text-gray-400" size={20}/><input id="reg-email" required type="email" autoComplete="email" value={email} onChange={e=>setEmail(e.target.value)} className={fieldClass} placeholder="name@example.com"/></div></label>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Password<div className="relative mt-1.5"><Lock className="absolute left-4 top-3.5 text-gray-400" size={20}/><input id="reg-password" required minLength={6} type="password" autoComplete="new-password" value={password} onChange={e=>setPassword(e.target.value)} className={fieldClass} placeholder="At least 6 characters"/></div></label>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Confirm Password<div className="relative mt-1.5"><Lock className="absolute left-4 top-3.5 text-gray-400" size={20}/><input id="reg-confirm-password" required type="password" autoComplete="new-password" value={confirmPassword} onChange={e=>setConfirmPassword(e.target.value)} className={fieldClass} placeholder="••••••••"/></div></label>
        <button id="register-submit" className="w-full bg-gray-900 dark:bg-white text-white dark:text-gray-900 font-bold py-4 rounded-xl flex items-center justify-center gap-2 focus-visible:ring-4 focus-visible:ring-blue-300">Create Account <ArrowRight size={18}/></button>
      </form>
      <div className="flex items-center gap-3 my-6"><span className="h-px bg-gray-200 dark:bg-gray-700 flex-1"/><span className="text-xs text-gray-400">OR</span><span className="h-px bg-gray-200 dark:bg-gray-700 flex-1"/></div>
      <button type="button" onClick={continueWithGoogle} className="w-full border border-gray-300 dark:border-gray-700 py-3 rounded-xl font-semibold focus-visible:ring-4 focus-visible:ring-blue-200">Continue with Google</button>
      <p className="text-xs text-gray-500 text-center mt-5">Architect and Constructor accounts are created by the HousePlanner administrator.</p>
      <p className="text-sm text-gray-500 text-center mt-5">Already have an account? <Link to="/login" className="font-semibold text-blue-600">Sign in</Link></p>
    </main>
  </div>;
};

export default RegisterPage;
