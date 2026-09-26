import React, { useState } from 'react';
import Navbar from './Navbar';
import Sidebar from './Sidebar';

interface PageContainerProps {
 children: React.ReactNode;
}

export const PageContainer: React.FC<PageContainerProps> = ({ children }) => {
 const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

 return (
  <div className="min-h-screen flex flex-col bg-background transition-colors duration-300">
   <Navbar onMenuClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)} />
   <div className="flex flex-1 relative">
    <Sidebar isOpen={isMobileMenuOpen} onClose={() => setIsMobileMenuOpen(false)} />
    <main className="flex-1 p-4 sm:p-8 overflow-y-auto">
     <div className="max-w-6xl mx-auto space-y-6">
      {children}
     </div>
    </main>
   </div>
  </div>
 );
};

export default PageContainer;
