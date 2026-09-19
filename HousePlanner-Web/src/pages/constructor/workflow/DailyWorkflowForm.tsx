import React, { useState } from 'react';
import { constructorWorkflowService } from '../../../services/constructorWorkflowService';
import type { ConstructionPhase } from '../../../services/constructorWorkflowService';

interface DailyWorkflowFormProps {
  projectId: string;
  phases: ConstructionPhase[];
  onSuccess: () => void;
}

export const DailyWorkflowForm: React.FC<DailyWorkflowFormProps> = ({ projectId, phases, onSuccess }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const [formData, setFormData] = useState({
    date: new Date().toISOString().split('T')[0],
    constructionPhaseId: '',
    completedWork: '',
    progressPercentage: 0,
    challenges: '',
    issues: '',
    resolution: '',
    tomorrowPlan: '',
    additionalNotes: '',
    status: 'In Progress'
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: name === 'progressPercentage' ? parseInt(value) : value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.completedWork) {
      setError('Completed work description is required.');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      await constructorWorkflowService.createLog({
        projectId,
        date: new Date(formData.date).toISOString(),
        constructionPhaseId: formData.constructionPhaseId || undefined,
        completedWork: formData.completedWork,
        progressPercentage: formData.progressPercentage,
        challenges: formData.challenges,
        issues: formData.issues,
        resolution: formData.resolution,
        tomorrowPlan: formData.tomorrowPlan,
        additionalNotes: formData.additionalNotes,
        status: formData.status,
        dayNumber: 0 // Service will auto-calculate
      });
      onSuccess();
    } catch (err: any) {
      setError(err.response?.data || 'Failed to submit log.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && (
        <div className="rounded-xl bg-red-50 p-4 text-sm text-red-800 dark:bg-red-900/30 dark:text-red-200">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Date</label>
          <input
            type="date"
            name="date"
            required
            value={formData.date}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Phase</label>
          <select
            name="constructionPhaseId"
            value={formData.constructionPhaseId}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          >
            <option value="">-- General Project Work --</option>
            {phases.map(p => (
              <option key={p.id} value={p.id}>{p.phaseName}</option>
            ))}
          </select>
        </div>

        <div className="md:col-span-2">
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Work Completed Today *</label>
          <textarea
            name="completedWork"
            required
            rows={3}
            value={formData.completedWork}
            onChange={handleChange}
            placeholder="Describe the tasks completed today..."
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div className="md:col-span-2">
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Progress Percentage ({formData.progressPercentage}%)
          </label>
          <input
            type="range"
            name="progressPercentage"
            min="0"
            max="100"
            value={formData.progressPercentage}
            onChange={handleChange}
            className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer dark:bg-gray-700 accent-indigo-600"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Challenges / Issues</label>
          <textarea
            name="challenges"
            rows={2}
            value={formData.challenges}
            onChange={handleChange}
            placeholder="Any blockers or challenges?"
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Resolutions</label>
          <textarea
            name="resolution"
            rows={2}
            value={formData.resolution}
            onChange={handleChange}
            placeholder="How were issues resolved?"
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div className="md:col-span-2">
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Plan for Tomorrow</label>
          <textarea
            name="tomorrowPlan"
            rows={2}
            value={formData.tomorrowPlan}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Log Status</label>
          <select
            name="status"
            value={formData.status}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          >
            <option value="Planned">Planned</option>
            <option value="In Progress">In Progress</option>
            <option value="Completed">Completed</option>
          </select>
        </div>
      </div>

      <div className="flex justify-end pt-4">
        <button
          type="submit"
          disabled={loading}
          className="inline-flex justify-center rounded-xl bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50"
        >
          {loading ? 'Saving...' : 'Submit Log'}
        </button>
      </div>
    </form>
  );
};

export default DailyWorkflowForm;
