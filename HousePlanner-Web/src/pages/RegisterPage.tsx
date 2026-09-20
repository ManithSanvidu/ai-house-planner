import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { motion, AnimatePresence } from 'framer-motion';
import { Box, Lock, Mail, User, ArrowRight, Sparkles, CheckCircle2, Building2, HardHat } from 'lucide-react';
import { registerAsync } from '../features/auth/authSlice';
import type { AppDispatch } from '../store';
import type { PublicRegistrableRole } from '../types/auth.types';
import useAuth from '../features/auth/useAuth';
import { auth } from '../services/firebase';
import apiClient from '../services/apiClient';
import { verifySessionAsync } from '../features/auth/authSlice';

interface RoleOption {
  id: PublicRegistrableRole;
  label: string;
  description: string;
  icon: React.ReactNode;
  gradient: string;
  accentColor: string;
}

const ROLE_OPTIONS: RoleOption[] = [
  {
    id: 'Customer',
    label: 'Customer',
    description: 'Design and manage your house project from start to finish.',
    icon: <HardHat size={28} strokeWidth={1.5} />,
    gradient: 'from-blue-500/10 to-indigo-500/10',
    accentColor: 'border-blue-500 ring-blue-500/30 bg-blue-50 dark:bg-blue-900/20',
  },
  {
    id: 'Architect',
    label: 'Architect',
    description: 'Review, validate, and manage customer design submissions.',
    icon: <Building2 size={28} strokeWidth={1.5} />,
    gradient: 'from-violet-500/10 to-purple-500/10',
    accentColor: 'border-violet-500 ring-violet-500/30 bg-violet-50 dark:bg-violet-900/20',
  },
  {
    id: 'Constructor',
    label: 'Constructor',
    description: 'Manage assigned construction projects and project progress.',
    icon: <HardHat size={28} strokeWidth={1.5} />, // HardHat fits Constructor well
    gradient: 'from-green-500/10 to-emerald-500/10',
    accentColor: 'border-green-500 ring-green-500/30 bg-green-50 dark:bg-green-900/20',
  },
];

const RegisterPage: React.FC = () => {
  const [step, setStep] = useState<1 | 2>(1);
  const [selectedRole, setSelectedRole] = useState<PublicRegistrableRole | null>(null);
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isHovered, setIsHovered] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    if (isAuthenticated) navigate('/dashboard');
  }, [isAuthenticated, navigate]);

  const handleRoleSelect = (role: PublicRegistrableRole) => {
    setSelectedRole(role);
  };

  const isGoogleOnboarding = !!auth.currentUser;

  const handleContinue = async () => {
    if (!selectedRole) {
      setError('Please select an account type to continue.');
      return;
    }
    setError('');

    if (isGoogleOnboarding) {
      // Direct registration for Google Auth users (already have Firebase identity)
      try {
        await apiClient.post('/auth/register', {
          requestedRole: selectedRole,
          fullName: auth.currentUser?.displayName || '',
        });
        // Now that the backend profile exists, verify session to update Redux state
        const result = await dispatch(verifySessionAsync());
        if (verifySessionAsync.fulfilled.match(result)) {
          navigate('/dashboard');
        } else {
          setError('Failed to load session after registration.');
        }
      } catch (err: any) {
        setError(err.response?.data?.error || err.message || 'Registration failed.');
      }
    } else {
      setStep(2);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!selectedRole) {
      setError('Account type is missing. Please go back and select one.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    if (password.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }

    const result = await dispatch(registerAsync({ email, password, fullName, requestedRole: selectedRole }));
    if (registerAsync.fulfilled.match(result)) {
      navigate('/dashboard');
    } else {
      setError((result.payload as string) || 'Registration failed. Please try again.');
    }
  };

  const selectedRoleOption = ROLE_OPTIONS.find(r => r.id === selectedRole);

  return (
    <div className="min-h-screen bg-[#fcfcfd] dark:bg-gray-950 flex items-center justify-center p-6 relative overflow-hidden transition-colors">
      
      {/* Decorative Background */}
      <div className="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none">
        <div className="absolute -top-[10%] -right-[5%] w-[40%] h-[40%] rounded-full bg-violet-100/50 dark:bg-violet-900/20 blur-3xl" />
        <div className="absolute top-[60%] -left-[10%] w-[30%] h-[30%] rounded-full bg-blue-100/40 dark:bg-blue-900/20 blur-3xl" />
      </div>

      {/* Logo */}
      <Link to="/" className="absolute top-8 left-8 flex items-center gap-3 hover:opacity-80 transition-opacity z-10">
        <div className="w-8 h-8 relative flex items-center justify-center">
          <Box className="absolute text-gray-900 dark:text-white transition-colors" size={24} strokeWidth={1.5} />
          <Sparkles className="absolute text-yellow-600 -top-1 -right-1" size={12} />
        </div>
        <span className="text-sm font-bold text-gray-900 dark:text-white tracking-[0.2em] transition-colors">
          HOMEPLANNER<span className="text-gray-400">AI</span>
        </span>
      </Link>

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
        className="w-full max-w-md relative z-10"
      >
        <div className="bg-white dark:bg-gray-900 rounded-3xl p-8 sm:p-10 custom-shadow-xl border border-gray-100 dark:border-gray-800 relative overflow-hidden">

          {/* Step indicator */}
          <div className="flex items-center gap-2 mb-8">
            {[1, 2].map((s) => (
              <React.Fragment key={s}>
                <div className={`flex items-center justify-center w-7 h-7 rounded-full text-xs font-bold transition-all ${
                  s === step
                    ? 'bg-gray-900 dark:bg-white text-white dark:text-gray-900'
                    : s < step
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-100 dark:bg-gray-800 text-gray-400'
                }`}>
                  {s < step ? <CheckCircle2 size={14} /> : s}
                </div>
                {s < 2 && <div className={`flex-1 h-0.5 rounded-full transition-all ${s < step ? 'bg-blue-600' : 'bg-gray-100 dark:bg-gray-800'}`} />}
              </React.Fragment>
            ))}
          </div>

          <AnimatePresence mode="wait">
            {step === 1 && (
              <motion.div
                key="step1"
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: 20 }}
                transition={{ duration: 0.25 }}
              >
                <div className="mb-8">
                  <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
                    {isGoogleOnboarding ? 'Complete your account' : 'Create an account'}
                  </h1>
                  <p className="text-sm text-gray-500 dark:text-gray-400">
                    {isGoogleOnboarding ? "Choose how you'll use HousePlanner." : 'Choose the account type that describes your role.'}
                  </p>
                </div>

                {error && (
                  <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl text-sm text-red-600 dark:text-red-400 text-center">
                    {error}
                  </div>
                )}

                <div className="space-y-3 mb-8" role="radiogroup" aria-label="Account type">
                  {ROLE_OPTIONS.map((option) => {
                    const isSelected = selectedRole === option.id;
                    return (
                      <button
                        key={option.id}
                        id={`role-option-${option.id.toLowerCase()}`}
                        role="radio"
                        aria-checked={isSelected}
                        onClick={() => handleRoleSelect(option.id)}
                        className={`w-full text-left p-4 rounded-2xl border-2 transition-all duration-200 flex items-center gap-4 group ${
                          isSelected
                            ? `${option.accentColor} border-2 ring-4`
                            : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600 bg-white dark:bg-gray-800/50'
                        }`}
                      >
                        <div className={`p-2.5 rounded-xl transition-all ${
                          isSelected
                            ? 'bg-white dark:bg-gray-900 text-gray-900 dark:text-white shadow-sm'
                            : 'bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400'
                        }`}>
                          {option.icon}
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className={`font-semibold text-sm transition-colors ${
                            isSelected ? 'text-gray-900 dark:text-white' : 'text-gray-700 dark:text-gray-200'
                          }`}>
                            {option.label}
                          </p>
                          <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5 leading-relaxed">
                            {option.description}
                          </p>
                        </div>
                        {isSelected && <CheckCircle2 className="text-blue-600 shrink-0" size={20} />}
                      </button>
                    );
                  })}
                </div>

                <button
                  onClick={handleContinue}
                  onMouseEnter={() => setIsHovered(true)}
                  onMouseLeave={() => setIsHovered(false)}
                  disabled={!selectedRole}
                  className="w-full bg-gray-900 dark:bg-white hover:bg-black dark:hover:bg-gray-200 disabled:bg-gray-200 dark:disabled:bg-gray-700 disabled:text-gray-400 dark:disabled:text-gray-500 text-white dark:text-gray-900 font-bold py-4 rounded-xl flex items-center justify-center gap-2 transition-all custom-shadow-md"
                >
                  {isGoogleOnboarding ? 'Continue' : `Continue as ${selectedRole ?? '...'}`}
                  <motion.div animate={{ x: isHovered && selectedRole ? 4 : 0 }} transition={{ duration: 0.2 }}>
                    <ArrowRight size={18} />
                  </motion.div>
                </button>

                <div className="mt-6 text-center text-sm text-gray-500">
                  Already have an account?{' '}
                  <Link to="/login" className="font-semibold text-blue-600 hover:text-blue-700 dark:text-blue-400">
                    Sign in
                  </Link>
                </div>
              </motion.div>
            )}

            {step === 2 && (
              <motion.div
                key="step2"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{ duration: 0.25 }}
              >
                <div className="mb-8">
                  <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">Your details</h1>
                  <div className="flex items-center gap-2">
                    <p className="text-sm text-gray-500 dark:text-gray-400">Registering as</p>
                    <span className={`inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full ${
                      selectedRole === 'Architect'
                        ? 'bg-violet-100 dark:bg-violet-900/40 text-violet-700 dark:text-violet-300'
                        : 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300'
                    }`}>
                    {selectedRoleOption?.id === 'Architect' ? <Building2 size={12} /> : <HardHat size={12} />}
                      {selectedRole}
                    </span>
                    <button
                      onClick={() => { setStep(1); setError(''); }}
                      className="text-xs text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 underline ml-auto"
                    >
                      Change
                    </button>
                  </div>
                </div>

                <form onSubmit={handleRegister} className="space-y-5">
                  {error && (
                    <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl text-sm text-red-600 dark:text-red-400 text-center">
                      {error}
                    </div>
                  )}

                  {/* Full Name */}
                  <div className="space-y-1.5">
                    <label htmlFor="reg-fullname" className="text-sm font-medium text-gray-700 dark:text-gray-300 ml-1">
                      Full Name
                    </label>
                    <div className="relative">
                      <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                        <User className="h-5 w-5 text-gray-400" />
                      </div>
                      <input
                        id="reg-fullname"
                        type="text"
                        value={fullName}
                        onChange={(e) => setFullName(e.target.value)}
                        required
                        autoComplete="name"
                        className="block w-full pl-11 pr-4 py-3.5 bg-gray-50 dark:bg-gray-950 border border-gray-200 dark:border-gray-800 rounded-xl text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all outline-none"
                        placeholder="Kasun Perera"
                      />
                    </div>
                  </div>

                  {/* Email */}
                  <div className="space-y-1.5">
                    <label htmlFor="reg-email" className="text-sm font-medium text-gray-700 dark:text-gray-300 ml-1">
                      Email Address
                    </label>
                    <div className="relative">
                      <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                        <Mail className="h-5 w-5 text-gray-400" />
                      </div>
                      <input
                        id="reg-email"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        autoComplete="email"
                        className="block w-full pl-11 pr-4 py-3.5 bg-gray-50 dark:bg-gray-950 border border-gray-200 dark:border-gray-800 rounded-xl text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all outline-none"
                        placeholder="name@example.com"
                      />
                    </div>
                  </div>

                  {/* Password */}
                  <div className="space-y-1.5">
                    <label htmlFor="reg-password" className="text-sm font-medium text-gray-700 dark:text-gray-300 ml-1">
                      Password
                    </label>
                    <div className="relative">
                      <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                        <Lock className="h-5 w-5 text-gray-400" />
                      </div>
                      <input
                        id="reg-password"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        autoComplete="new-password"
                        minLength={6}
                        className="block w-full pl-11 pr-4 py-3.5 bg-gray-50 dark:bg-gray-950 border border-gray-200 dark:border-gray-800 rounded-xl text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all outline-none"
                        placeholder="At least 6 characters"
                      />
                    </div>
                  </div>

                  {/* Confirm Password */}
                  <div className="space-y-1.5">
                    <label htmlFor="reg-confirm-password" className="text-sm font-medium text-gray-700 dark:text-gray-300 ml-1">
                      Confirm Password
                    </label>
                    <div className="relative">
                      <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                        <Lock className="h-5 w-5 text-gray-400" />
                      </div>
                      <input
                        id="reg-confirm-password"
                        type="password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        required
                        autoComplete="new-password"
                        className="block w-full pl-11 pr-4 py-3.5 bg-gray-50 dark:bg-gray-950 border border-gray-200 dark:border-gray-800 rounded-xl text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all outline-none"
                        placeholder="••••••••"
                      />
                    </div>
                  </div>

                  <button
                    id="register-submit"
                    type="submit"
                    onMouseEnter={() => setIsHovered(true)}
                    onMouseLeave={() => setIsHovered(false)}
                    className="w-full bg-gray-900 dark:bg-white hover:bg-black dark:hover:bg-gray-200 text-white dark:text-gray-900 font-bold py-4 rounded-xl flex items-center justify-center gap-2 transition-all custom-shadow-md group mt-2"
                  >
                    Create Account
                    <motion.div animate={{ x: isHovered ? 4 : 0 }} transition={{ duration: 0.2 }}>
                      <ArrowRight size={18} />
                    </motion.div>
                  </button>
                </form>

                <div className="mt-6 text-center text-sm text-gray-500">
                  Already have an account?{' '}
                  <Link to="/login" className="font-semibold text-blue-600 hover:text-blue-700 dark:text-blue-400">
                    Sign in
                  </Link>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </motion.div>
    </div>
  );
};

export default RegisterPage;
