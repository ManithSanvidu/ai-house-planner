import { useEffect, useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { preDesignedPlanService, type PreDesignedPlanSummary } from '../services/preDesignedPlanService';
import { Search, Plus, Edit2, Power, PowerOff, Image as ImageIcon, Loader2 } from 'lucide-react';
import { getImageUrl } from '../utils/imageUtils';

export default function AdminPlansPage() {
  const [plans, setPlans] = useState<PreDesignedPlanSummary[]>([]);
  const [status, setStatus] = useState('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    preDesignedPlanService.adminList(status)
      .then(data => {
        setPlans(data);
        setError('');
      })
      .catch(() => setError('Could not load plans.'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status]);

  const toggle = async (p: PreDesignedPlanSummary) => {
    if (!p.isActive && !confirm(`Reactivate ${p.name}?`)) return;
    if (p.isActive && !confirm(`Deactivate ${p.name}? Existing designs will remain available.`)) return;
    try {
      await preDesignedPlanService.setStatus(p.id, !p.isActive);
      load();
    } catch (err) {
      setError('Failed to update status.');
    }
  };

  const filteredPlans = useMemo(() => {
    if (!searchQuery.trim()) return plans;
    const q = searchQuery.toLowerCase();
    return plans.filter(p =>
      p.name.toLowerCase().includes(q) ||
      p.designCode.toLowerCase().includes(q)
    );
  }, [plans, searchQuery]);

  return (
    <main className="p-4 md:p-6 md:p-10 max-w-7xl mx-auto text-zinc-900 dark:text-text-primary">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
        <div>
          <p className="text-indigo-500 font-semibold text-sm mb-1 uppercase tracking-wider">Admin</p>
          <h1 className="text-2xl md:text-3xl font-bold">Plan Catalogue</h1>
          <p className="text-sm text-text-secondary mt-1">Manage the house plans available in the system.</p>
        </div>
        <Link
          to="/dashboard/admin/plans/new"
          className="bg-indigo-600 hover:bg-indigo-700 transition-colors text-white px-5 py-2.5 rounded-xl font-medium flex items-center gap-2 shadow-sm whitespace-nowrap"
        >
          <Plus size={18} /> Add Plan
        </Link>
      </div>

      {error && (
        <div className="bg-red-50 text-red-600 p-4 rounded-xl mb-6 border border-red-100">
          <p role="alert">{error}</p>
        </div>
      )}

      <div className="flex flex-col md:flex-row gap-4 mb-6">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
          <input
            type="text"
            placeholder="Search plans by name or code..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl bg-surface border border-gray-200 dark:border-gray-800 focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm"
            aria-label="Search plans"
          />
        </div>
        <select
          value={status}
          onChange={e => setStatus(e.target.value)}
          className="px-4 py-2.5 rounded-xl bg-surface border border-gray-200 dark:border-gray-800 focus:outline-none focus:ring-2 focus:ring-indigo-500 min-w-[160px] text-sm"
          aria-label="Filter by status"
        >
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </div>

      <div className="bg-surface border border-gray-200 dark:border-gray-800 rounded-2xl overflow-hidden shadow-sm">
        {loading ? (
          <div className="flex flex-col items-center justify-center py-4 md:py-20 text-text-secondary">
            <Loader2 className="animate-spin mb-4" size={32} />
            <p>Loading catalogue...</p>
          </div>
        ) : filteredPlans.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-4 md:py-20 text-text-secondary text-center px-4">
            <div className="bg-gray-100 dark:bg-gray-800/50 p-4 rounded-full mb-4">
              <Search size={32} className="text-gray-400" />
            </div>
            <h3 className="text-lg font-semibold text-text-primary mb-1">No plans found</h3>
            <p className="text-sm">Try changing your search query or status filter.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 dark:bg-gray-900/50 text-text-secondary border-b border-gray-200 dark:border-gray-800">
                <tr>
                  <th className="px-4 md:px-6 py-4 font-medium">Plan</th>
                  <th className="px-4 md:px-6 py-4 font-medium">Configuration</th>
                  <th className="px-4 md:px-6 py-4 font-medium">Status</th>
                  <th className="px-4 md:px-6 py-4 font-medium text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-800">
                {filteredPlans.map(p => (
                  <tr key={p.id} className={`hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors ${!p.isActive ? 'opacity-75 bg-gray-50/50 dark:bg-gray-900/20' : ''}`}>
                    <td className="px-4 md:px-6 py-4">
                      <div className="flex items-center gap-4">
                        <div className="w-16 h-16 rounded-lg bg-gray-100 dark:bg-gray-800 flex-shrink-0 flex items-center justify-center border border-gray-200 dark:border-gray-700 overflow-hidden">
                          {p.thumbnailUrl ? (
                            <img src={getImageUrl(p.thumbnailUrl)} alt={p.name} className="w-full h-full object-cover" />
                          ) : (
                            <ImageIcon className="text-gray-400" size={24} />
                          )}
                        </div>
                        <div>
                          <Link to={`/dashboard/admin/plans/${p.id}/edit`} className="font-bold text-base text-text-primary hover:text-indigo-600 transition-colors">
                            {p.name}
                          </Link>
                          <div className="text-xs text-text-secondary font-mono mt-1">{p.designCode}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 md:px-6 py-4">
                      <div className="flex flex-col gap-1.5 text-xs font-medium text-gray-600 dark:text-gray-300">
                        <span className="bg-gray-100 dark:bg-gray-800 px-2.5 py-1 rounded-md w-max">{p.bedrooms} {p.bedrooms === 1 ? 'Bedroom' : 'Bedrooms'}</span>
                        <span className="bg-gray-100 dark:bg-gray-800 px-2.5 py-1 rounded-md w-max">{p.bathrooms} {p.bathrooms === 1 ? 'Bathroom' : 'Bathrooms'}</span>
                        <span className="bg-gray-100 dark:bg-gray-800 px-2.5 py-1 rounded-md w-max">{p.floorCount} {p.floorCount === 1 ? 'Floor' : 'Floors'}</span>
                      </div>
                    </td>
                    <td className="px-4 md:px-6 py-4">
                      {p.isActive ? (
                        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800/50">
                          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                          Active
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-400 border border-gray-200 dark:border-gray-700">
                          <span className="w-1.5 h-1.5 rounded-full bg-gray-400"></span>
                          Inactive
                        </span>
                      )}
                    </td>
                    <td className="px-4 md:px-6 py-4">
                      <div className="flex items-center justify-end gap-3">
                        <Link
                          to={`/dashboard/admin/plans/${p.id}/edit`}
                          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-indigo-600 hover:bg-indigo-50 dark:text-indigo-400 dark:hover:bg-indigo-900/30 rounded-lg transition-colors"
                          aria-label={`Edit ${p.name}`}
                        >
                          <Edit2 size={16} /> Edit
                        </Link>
                        <button
                          onClick={() => toggle(p)}
                          className={`flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-lg transition-colors ${
                            p.isActive
                              ? 'text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/30'
                              : 'text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-900/30'
                          }`}
                          aria-label={p.isActive ? `Deactivate ${p.name}` : `Reactivate ${p.name}`}
                        >
                          {p.isActive ? <><PowerOff size={16} /> Deactivate</> : <><Power size={16} /> Reactivate</>}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </main>
  );
}
