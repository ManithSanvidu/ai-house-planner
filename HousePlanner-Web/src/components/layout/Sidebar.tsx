import React from 'react';
import { NavLink } from 'react-router-dom';
import { 
  LayoutDashboard, 
  PlusSquare, Library, Shield, X
} from 'lucide-react';
import useAuth from '../../features/auth/useAuth';

interface SidebarProps {
  isOpen?: boolean;
  onClose?: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen, onClose }) => {
  const { user } = useAuth();
  
  if (!user) return null;

  // Common links for all users
  const links = [
    { to: '/dashboard', label: 'Overview', icon: LayoutDashboard },
    { to: '/dashboard/new-project', label: 'New Project (Intake)', icon: PlusSquare },
    { to: '/dashboard/plans', label: 'Plan Library', icon: Library },
    ...(user.role === 'Admin' ? [{ to: '/dashboard/admin/plans', label: 'Manage Plans', icon: Shield }] : []),
    ...(user.role === 'Architect' ? [
      { to: '/architect/dashboard', label: 'Architect Dashboard', icon: LayoutDashboard },
      { to: '/architect/requests', label: 'Validation Requests', icon: Library },
    ] : []),
    ...(user.role === 'Constructor' ? [
      { to: '/constructor/dashboard', label: 'Constructor Dashboard', icon: LayoutDashboard },
    ] : []),
  ];

  return (
    <>
      {/* Mobile Overlay */}
      {isOpen && (
        <div 
          className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 md:hidden transition-opacity"
          onClick={onClose}
        />
      )}
      
      {/* Sidebar Content */}
      <aside className={`
        fixed inset-y-0 left-0 z-50 w-64 transform transition-transform duration-300 md:relative md:translate-x-0
        ${isOpen ? 'translate-x-0' : '-translate-x-full'}
        border-r border-zinc-200/50 dark:border-gray-800/50 bg-white dark:bg-gray-950/90 md:bg-white/80 md:dark:bg-gray-950/80 md:backdrop-blur-xl min-h-[calc(100vh-65px)] p-4 flex flex-col gap-6 shadow-2xl md:shadow-[4px_0_24px_-12px_rgba(0,0,0,0.1)] transition-colors
      `}>
        <div>
          <div className="flex items-center justify-between px-3 mb-1">
            <h2 className="text-[10px] font-bold text-zinc-400 dark:text-gray-500 uppercase tracking-wider">
              Navigation
            </h2>
            {onClose && (
              <button onClick={onClose} className="md:hidden p-1 text-zinc-500 hover:text-zinc-800 dark:hover:text-zinc-200">
                <X size={16} />
              </button>
            )}
          </div>
          <nav className="flex flex-col gap-1.5 mt-2">
            {links.map((link) => {
              const Icon = link.icon;
              
              return (
                <NavLink
                  key={link.to}
                  to={link.to}
                  end={link.to === '/dashboard'}
                  className={({ isActive }) =>
                    `flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-300 ${
                      isActive
                        ? 'bg-gradient-to-r from-indigo-50 to-white dark:from-indigo-900/30 dark:to-gray-900 text-indigo-700 dark:text-indigo-400 font-semibold shadow-sm border border-indigo-100/50 dark:border-indigo-800/30'
                        : 'text-zinc-500 dark:text-gray-400 hover:bg-zinc-50/80 dark:hover:bg-gray-900/50 hover:text-zinc-900 dark:hover:text-gray-200 border border-transparent'
                    }`
                  }
                >
                  {({ isActive }) => (
                    <>
                      <Icon size={18} className={isActive ? 'text-indigo-600 dark:text-indigo-400' : 'text-zinc-400 dark:text-gray-500'} />
                      <span>{link.label}</span>
                    </>
                  )}
                </NavLink>
              );
            })}
          </nav>
        </div>

        <div className="mt-auto border-t border-zinc-100 dark:border-gray-800/50 pt-4 px-3 text-center bg-zinc-50/50 dark:bg-gray-900/30 rounded-xl pb-2 transition-colors duration-300">
          <p className="text-[11px] text-zinc-400 dark:text-gray-500 font-medium leading-normal">
            Logged in as <span className="font-bold text-zinc-700 dark:text-gray-300 bg-white dark:bg-gray-800 px-2 py-0.5 rounded shadow-sm border border-zinc-100 dark:border-gray-700 ml-1">{user.role}</span>
          </p>
        </div>
      </aside>
    </>
  );
};

export default Sidebar;
