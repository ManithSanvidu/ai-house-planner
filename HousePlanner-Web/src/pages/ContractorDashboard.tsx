import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { HardHat, Clock } from 'lucide-react';
import projectService from '../features/projects/projectService';
// import Card from '../components/common/Card';

const ContractorDashboard: React.FC = () => {
  const navigate = useNavigate();
  const [projects, setProjects] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchProjects = async () => {
      try {
        const data = await projectService.getProjects();
        setProjects(data);
      } catch (err: any) {
        setError(err.message || 'Failed to load projects');
      } finally {
        setLoading(false);
      }
    };
    fetchProjects();
  }, []);

  if (loading) {
    return (
      <div className="flex justify-center items-center h-full min-h-[calc(100vh-65px)] bg-gray-50">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-[calc(100vh-65px)] bg-gray-50 p-6 flex justify-center">
        <div className="text-red-500 bg-red-50 p-4 rounded-lg">{error}</div>
      </div>
    );
  }

  return (
    <div className="min-h-[calc(100vh-65px)] bg-gradient-to-br from-zinc-50 to-zinc-100 p-6">
      <div className="max-w-6xl mx-auto space-y-6">
        <div className="flex items-center gap-4 mb-8">
          <div className="p-3 bg-indigo-100 rounded-xl text-indigo-600">
            <HardHat size={28} />
          </div>
          <div>
            <h1 className="text-3xl font-bold text-zinc-900">Contractor Dashboard</h1>
            <p className="text-zinc-500">Manage your construction projects and phases.</p>
          </div>
        </div>

        {projects.length === 0 ? (
          <div className="text-center p-12 bg-white rounded-2xl shadow-sm border border-zinc-200">
            <p className="text-zinc-500">No projects available at the moment.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {projects.map((project) => (
              <motion.div
                key={project.id}
                whileHover={{ y: -4 }}
                className="bg-white p-6 rounded-2xl shadow-sm border border-zinc-200 cursor-pointer hover:shadow-md transition-all"
                onClick={() => navigate(`/project-tracking?projectId=${project.id}`)}
              >
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="font-bold text-zinc-900">Project {project.id.slice(0, 8).toUpperCase()}</h3>
                    <p className="text-xs text-zinc-500 font-mono mt-1">{project.id}</p>
                  </div>
                  <span className={`px-2 py-1 rounded text-xs font-bold ${project.status === 'completed' ? 'bg-green-100 text-green-700' : project.status === 'in_progress' ? 'bg-blue-100 text-blue-700' : 'bg-yellow-100 text-yellow-700'}`}>
                    {project.status.replace('_', ' ').toUpperCase()}
                  </span>
                </div>

                <div className="flex items-center gap-2 text-sm text-zinc-600 mb-2">
                  <Clock size={16} />
                  <span>Created: {new Date(project.createdAt).toLocaleDateString()}</span>
                </div>

                <div className="mt-4 pt-4 border-t border-zinc-100">
                  <button className="text-indigo-600 text-sm font-semibold hover:text-indigo-700 flex items-center gap-1">
                    Manage Project &rarr;
                  </button>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default ContractorDashboard;
