import React, { useState, useEffect } from 'react';
import { dailyConstructionLogService } from '../../../services/constructorWorkflowService';
import type { DailyConstructionLogDto, CreateDailyConstructionLogRequest, UpdateDailyConstructionLogRequest } from '../../../services/constructorWorkflowService';
import type { ConstructionPhase } from '../../../services/constructorWorkflowService';

interface DailyLogbookFormProps {
  projectId: string;
  phases: ConstructionPhase[];
  existingLog?: DailyConstructionLogDto;
  onSuccess: () => void;
  onCancel: () => void;
  isReadOnly?: boolean;
}

export const DailyLogbookForm: React.FC<DailyLogbookFormProps> = ({ projectId, phases, existingLog, onSuccess, onCancel, isReadOnly }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState<CreateDailyConstructionLogRequest>({
    logDate: new Date().toISOString().split('T')[0],
    constructionPhaseId: '',
    workCompleted: '',
    progressPercentage: 0,
    challenges: '',
    materialsUsed: '',
    workforceCount: 0,
    weatherCondition: '',
    safetyIssues: '',
    tomorrowPlan: '',
    notes: ''
  });

  useEffect(() => {
    if (existingLog) {
      setFormData({
        logDate: existingLog.logDate.split('T')[0], // Assuming yyyy-MM-dd
        constructionPhaseId: existingLog.constructionPhaseId || '',
        workCompleted: existingLog.workCompleted || '',
        progressPercentage: existingLog.progressPercentage || 0,
        challenges: existingLog.challenges || '',
        materialsUsed: existingLog.materialsUsed || '',
        workforceCount: existingLog.workforceCount || 0,
        weatherCondition: existingLog.weatherCondition || '',
        safetyIssues: existingLog.safetyIssues || '',
        tomorrowPlan: existingLog.tomorrowPlan || '',
        notes: existingLog.notes || ''
      });
    }
  }, [existingLog]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    let finalValue: string | number = value;
    if (type === 'number' || type === 'range') {
      finalValue = value === '' ? 0 : Number(value);
    }
    setFormData(prev => ({ ...prev, [name]: finalValue }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isReadOnly) return;
    
    if (!formData.workCompleted.trim()) {
      setError('Completed work description is required.');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const requestData = {
        ...formData,
        constructionPhaseId: formData.constructionPhaseId || undefined,
        workforceCount: formData.workforceCount ? Number(formData.workforceCount) : undefined
      };

      if (existingLog) {
        await dailyConstructionLogService.updateLog(projectId, existingLog.id, requestData as UpdateDailyConstructionLogRequest);
      } else {
        await dailyConstructionLogService.createLog(projectId, requestData as CreateDailyConstructionLogRequest);
      }
      onSuccess();
    } catch (err: any) {
      if (err.response?.status === 409) {
          setError('Project completed. Daily logbook is read-only.');
      } else {
          setError(err.response?.data?.title || err.response?.data || 'Failed to save log.');
      }
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
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Date *</label>
          <input
            type="date"
            name="logDate"
            required
            disabled={isReadOnly}
            value={formData.logDate}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Phase</label>
          <select
            name="constructionPhaseId"
            disabled={isReadOnly}
            value={formData.constructionPhaseId}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
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
            name="workCompleted"
            required
            rows={3}
            disabled={isReadOnly}
            value={formData.workCompleted}
            onChange={handleChange}
            placeholder="Describe the tasks completed today..."
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
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
            disabled={isReadOnly}
            value={formData.progressPercentage}
            onChange={handleChange}
            className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer disabled:opacity-50 dark:bg-gray-700 accent-indigo-600"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Workers on Site</label>
          <input
            type="number"
            name="workforceCount"
            min="0"
            disabled={isReadOnly}
            value={formData.workforceCount}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Weather / Site Condition</label>
          <input
            type="text"
            name="weatherCondition"
            disabled={isReadOnly}
            value={formData.weatherCondition}
            onChange={handleChange}
            placeholder="e.g. Sunny, 75F"
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Materials / Resources Used</label>
          <textarea
            name="materialsUsed"
            rows={2}
            disabled={isReadOnly}
            value={formData.materialsUsed}
            onChange={handleChange}
            placeholder="Concrete, lumber, etc."
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>
        
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Safety Issues</label>
          <textarea
            name="safetyIssues"
            rows={2}
            disabled={isReadOnly}
            value={formData.safetyIssues}
            onChange={handleChange}
            placeholder="Any safety incidents?"
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Challenges Faced Today</label>
          <textarea
            name="challenges"
            rows={2}
            disabled={isReadOnly}
            value={formData.challenges}
            onChange={handleChange}
            placeholder="Any blockers or challenges?"
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>
        
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Tomorrow's Plan</label>
          <textarea
            name="tomorrowPlan"
            rows={2}
            disabled={isReadOnly}
            value={formData.tomorrowPlan}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>
        
        <div className="md:col-span-2">
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Additional Notes</label>
          <textarea
            name="notes"
            rows={2}
            disabled={isReadOnly}
            value={formData.notes}
            onChange={handleChange}
            className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 disabled:opacity-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
          />
        </div>
      </div>

      {!isReadOnly && (
        <div className="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-gray-700">
          <button
            type="button"
            onClick={onCancel}
            disabled={loading}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-xl hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-600 dark:hover:bg-gray-700"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={loading}
            className="inline-flex justify-center rounded-xl bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50"
          >
            {loading ? 'Saving...' : 'Save Log'}
          </button>
        </div>
      )}
    </form>
  );
};

export default DailyLogbookForm;
