import React, { useState, useMemo, useEffect } from 'react';
import {
  Search,
  Pencil,
  X,
  Save,
  Tag,
  Box,
  PackageCheck,
  AlertCircle,
  TrendingUp,
  CalendarDays,
  DollarSign,
  RefreshCw,
  Loader2,
  Plus,
  Users,
  Building2,
  Info,
  Archive,
  History,
} from 'lucide-react';
import pricingService from '../services/pricingService';
import type { PricingHistoryItem, PricingItem as ApiPricingItem } from '../types/pricing.types';

// ─────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────

interface PricingItem {
  id: number;
  name: string;
  category: string; // machine category: "material" | "labour"
  displayGroup: string; // human grouping: "Foundation", "Structural", etc.
  unit: string;
  unitCost: number;
  flatMultiplier: number;
  hillsideMultiplier: number;
  coastalMultiplier: number;
  provider?: string | null;
  sourceReference?: string | null;
  region: string;
  qualityLevel: 'Basic' | 'Standard' | 'Premium' | 'Luxury';
  isActive: boolean;
  createdAt: string;
  updatedByUserId?: string | null;
  updatedByName?: string | null;
  lastUpdated: string;
}

interface EditFormState {
  unitCost: string;
  flatMultiplier: string;
  hillsideMultiplier: string;
  coastalMultiplier: string;
  reason: string;
}

interface CreateFormState {
  itemName: string;
  category: 'material' | 'labour';
  displayGroup: string;
  unitCost: string;
  flatMultiplier: string;
  hillsideMultiplier: string;
  coastalMultiplier: string;
  sourceReference: string;
  region: string;
  qualityLevel: 'Basic' | 'Standard' | 'Premium' | 'Luxury';
}

interface ValidationErrors {
  itemName?: string;
  category?: string;
  displayGroup?: string;
  unitCost?: string;
  flatMultiplier?: string;
  hillsideMultiplier?: string;
  coastalMultiplier?: string;
}

const DISPLAY_GROUPS = [
  'Foundation',
  'Structural',
  'Roofing',
  'Finishing',
  'MEP',
  'Labour',
  'General',
] as const;

// ─────────────────────────────────────────────
// Adapter: backend → view-model
// ─────────────────────────────────────────────

function toViewModel(api: ApiPricingItem): PricingItem {
  return {
    id: api.id,
    name: api.itemName,
    category: api.category.toLowerCase(),
    displayGroup: api.displayGroup || (api.category.toLowerCase() === 'labour' ? 'Labour' : 'General'),
    unit: api.unit,
    unitCost: api.unitCostLkr,
    flatMultiplier: api.terrainMultiplier.flat,
    hillsideMultiplier: api.terrainMultiplier.hillside,
    coastalMultiplier: api.terrainMultiplier.coastal,
    provider: api.provider,
    sourceReference: api.sourceReference,
    region: api.region,
    qualityLevel: api.qualityLevel,
    isActive: api.isActive,
    createdAt: api.createdAt,
    updatedByUserId: api.updatedByUserId,
    updatedByName: api.updatedByName,
    lastUpdated: api.updatedAt,
  };
}

// ─────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────

const GROUP_COLORS: Record<string, string> = {
  Foundation: 'bg-stone-900/30 text-stone-300 ring-1 ring-stone-700',
  Structural: 'bg-blue-900/30 text-blue-300 ring-1 ring-blue-800',
  Roofing: 'bg-orange-900/30 text-orange-300 ring-1 ring-orange-800',
  Finishing: 'bg-violet-900/30 text-violet-300 ring-1 ring-violet-800',
  MEP: 'bg-emerald-900/30 text-emerald-300 ring-1 ring-emerald-800',
  Labour: 'bg-amber-900/30 text-amber-300 ring-1 ring-amber-800',
  General: 'bg-[#1F2937] text-slate-400 ring-1 ring-slate-200',
};

function getGroupColor(group: string): string {
  return GROUP_COLORS[group] ?? 'bg-[#1F2937] text-slate-400 ring-1 ring-slate-200';
}

function formatRateOrFactor(item: PricingItem): string {
  if (item.category === 'labour') {
    return `${item.unitCost} × material cost (${(item.unitCost * 100).toFixed(0)}%)`;
  }
  return `LKR ${item.unitCost.toLocaleString('en-LK')} / sqft`;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

function getLatestUpdate(items: PricingItem[]): string {
  const sorted = [...items].sort(
    (a, b) => new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime()
  );
  return sorted[0] ? formatDate(sorted[0].lastUpdated) : '-';
}

function parsePositiveFloat(value: string): number | null {
  const n = parseFloat(value);
  return isNaN(n) || n <= 0 ? null : n;
}

function validateEditForm(form: EditFormState): ValidationErrors {
  const errors: ValidationErrors = {};
  if (parsePositiveFloat(form.unitCost) === null)
    errors.unitCost = 'Must be a positive number.';
  if (parsePositiveFloat(form.flatMultiplier) === null)
    errors.flatMultiplier = 'Must be a positive number.';
  if (parsePositiveFloat(form.hillsideMultiplier) === null)
    errors.hillsideMultiplier = 'Must be a positive number.';
  if (parsePositiveFloat(form.coastalMultiplier) === null)
    errors.coastalMultiplier = 'Must be a positive number.';
  return errors;
}

function validateCreateForm(form: CreateFormState): ValidationErrors {
  const errors: ValidationErrors = {};
  if (!form.itemName.trim())
    errors.itemName = 'Item name is required.';
  if (parsePositiveFloat(form.unitCost) === null)
    errors.unitCost = 'Must be a positive number.';
  if (parsePositiveFloat(form.flatMultiplier) === null)
    errors.flatMultiplier = 'Must be a positive number.';
  if (parsePositiveFloat(form.hillsideMultiplier) === null)
    errors.hillsideMultiplier = 'Must be a positive number.';
  if (parsePositiveFloat(form.coastalMultiplier) === null)
    errors.coastalMultiplier = 'Must be a positive number.';
  return errors;
}

// ─────────────────────────────────────────────
// SummaryCard sub-component
// ─────────────────────────────────────────────

interface SummaryCardProps {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub?: string;
}

const SummaryCard: React.FC<SummaryCardProps> = ({ icon, label, value, sub }) => (
  <div className="bg-[#111827] rounded-2xl border border-slate-800 custom-shadow-sm px-6 py-5 flex items-center gap-4">
    <div className="flex-shrink-0 w-10 h-10 rounded-xl bg-[#1F2937] border border-slate-800 flex items-center justify-center text-slate-400">
      {icon}
    </div>
    <div>
      <p className="text-[11px] font-semibold uppercase tracking-widest text-slate-400 mb-0.5">{label}</p>
      <p className="text-2xl font-bold text-white leading-tight">{value}</p>
      {sub && <p className="text-[11px] text-slate-400 mt-0.5">{sub}</p>}
    </div>
  </div>
);

// ─────────────────────────────────────────────
// MultiplierBadge sub-component
// ─────────────────────────────────────────────

interface MultiplierBadgeProps {
  value: number;
}

const MultiplierBadge: React.FC<MultiplierBadgeProps> = ({ value }) => {
  const isBase = value === 1.0;
  const isHigh = value >= 1.2;
  const cls = isBase
    ? 'text-slate-400 bg-[#1F2937] ring-1 ring-slate-100'
    : isHigh
    ? 'text-rose-400 bg-rose-900/30 ring-1 ring-rose-800'
    : 'text-purple-400 bg-purple-900/30 ring-1 ring-purple-700/50';

  return (
    <span className={`inline-block px-2 py-0.5 rounded-md text-xs font-bold font-mono ${cls}`}>
      x{value.toFixed(2)}
    </span>
  );
};

// ─────────────────────────────────────────────
// CreateModal sub-component
// ─────────────────────────────────────────────

interface CreateModalProps {
  onClose: () => void;
  onCreateSuccess: (created: PricingItem) => void;
}

const CreateModal: React.FC<CreateModalProps> = ({ onClose, onCreateSuccess }) => {
  const [form, setForm] = useState<CreateFormState>({
    itemName: '',
    category: 'material',
    displayGroup: 'Structural',
    unitCost: '',
    flatMultiplier: '1.00',
    hillsideMultiplier: '1.25',
    coastalMultiplier: '1.35',
    sourceReference: '',
    region: 'Sri Lanka',
    qualityLevel: 'Standard',
  });
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleCategoryChange = (category: 'material' | 'labour') => {
    setForm((prev) => ({
      ...prev,
      category,
      displayGroup: category === 'labour' ? 'Labour' : prev.displayGroup === 'Labour' ? 'Structural' : prev.displayGroup,
      flatMultiplier: category === 'labour' ? '1.00' : prev.flatMultiplier,
      hillsideMultiplier: category === 'labour' ? '1.00' : prev.hillsideMultiplier,
      coastalMultiplier: category === 'labour' ? '1.00' : prev.coastalMultiplier,
    }));
    setSubmitError(null);
  };

  const handleChange = (field: keyof CreateFormState) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    setForm((prev) => ({ ...prev, [field]: e.target.value }));
    setErrors((prev) => ({ ...prev, [field]: undefined }));
    setSubmitError(null);
  };

  const handleSave = async () => {
    const validationErrors = validateCreateForm(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setSubmitting(true);
    setSubmitError(null);

    try {
      const created = await pricingService.create({
        itemName: form.itemName.trim(),
        category: form.category,
        displayGroup: form.displayGroup.trim(),
        unitCostLkr: parseFloat(form.unitCost),
        terrainMultiplier: {
          flat: parseFloat(form.flatMultiplier),
          hillside: parseFloat(form.hillsideMultiplier),
          coastal: parseFloat(form.coastalMultiplier),
        },
        sourceReference: form.sourceReference.trim() || undefined,
        region: form.region.trim() || 'Sri Lanka',
        qualityLevel: form.qualityLevel,
      });
      onCreateSuccess(toViewModel(created));
    } catch (err: unknown) {
      const resp = (err as { response?: { data?: any; status?: number } })?.response;
      const message = typeof resp?.data === 'string' ? resp.data : resp?.data?.message || 'Failed to create pricing item.';
      setSubmitError(message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/30 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="relative bg-[#111827] rounded-3xl border border-slate-800 custom-shadow-lg w-full max-w-lg mx-4 overflow-hidden max-h-[90vh] flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div className="px-8 pt-8 pb-5 border-b border-slate-800 flex-shrink-0">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[10px] font-bold tracking-[0.18em] text-purple-400 uppercase mb-1">
                New Price Entry
              </p>
              <h2 className="text-xl font-bold text-white leading-snug">Add Pricing Item</h2>
              <p className="text-xs text-slate-400 mt-1">
                Define an agent-compatible rate for the AI Cost Estimator.
              </p>
            </div>
            <button
              onClick={onClose}
              className="flex-shrink-0 w-8 h-8 rounded-lg flex items-center justify-center text-slate-400 hover:text-slate-300 hover:bg-[#1F2937] transition-colors"
              aria-label="Close modal"
            >
              <X size={16} />
            </button>
          </div>
        </div>

        {/* Modal Error Banner */}
        {submitError && (
          <div className="mx-8 mt-4 flex items-start gap-2 rounded-xl bg-red-50 border border-red-100 px-4 py-3 flex-shrink-0">
            <AlertCircle size={14} className="mt-0.5 flex-shrink-0 text-red-500" />
            <p className="text-xs text-red-600 leading-relaxed">{submitError}</p>
          </div>
        )}

        {/* Modal Body */}
        <div className="px-8 py-5 space-y-4 overflow-y-auto">
          {/* Category Selector */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">
              Category (Machine Contract)
            </label>
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={() => handleCategoryChange('material')}
                className={`py-2 px-3 rounded-xl text-xs font-semibold border text-center transition-all ${
                  form.category === 'material'
                    ? 'border-purple-500 bg-purple-900/30/70 text-purple-300 ring-2 ring-indigo-500/20'
                    : 'border-slate-700 bg-[#1F2937] text-slate-400 hover:bg-[#111827]'
                }`}
              >
                Material (per_sqft)
              </button>
              <button
                type="button"
                onClick={() => handleCategoryChange('labour')}
                className={`py-2 px-3 rounded-xl text-xs font-semibold border text-center transition-all ${
                  form.category === 'labour'
                    ? 'border-amber-600 bg-amber-900/30/70 text-amber-300 ring-2 ring-amber-500/20'
                    : 'border-slate-700 bg-[#1F2937] text-slate-400 hover:bg-[#111827]'
                }`}
              >
                Labour (factor)
              </button>
            </div>
          </div>

          {/* Unit Note */}
          <div className="flex items-center gap-2 rounded-xl bg-[#1F2937] border border-slate-700/80 px-3.5 py-2.5">
            <Info size={14} className="text-slate-400 flex-shrink-0" />
            <p className="text-xs text-slate-400">
              {form.category === 'material' ? (
                <>
                  <span className="font-semibold text-slate-100">Unit: LKR per sqft</span> (canonical backend value: <code className="text-purple-400 font-mono">per_sqft</code>)
                </>
              ) : (
                <>
                  <span className="font-semibold text-slate-100">Unit: Labour factor</span> (canonical backend value: <code className="text-amber-600 font-mono">factor</code>)
                </>
              )}
            </p>
          </div>

          {/* Item Name */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1" htmlFor="create-itemName">
              Item Name
            </label>
            <input
              id="create-itemName"
              type="text"
              placeholder={form.category === 'material' ? 'e.g. Substructure Materials' : 'e.g. Standard Construction Labour'}
              value={form.itemName}
              onChange={handleChange('itemName')}
              disabled={submitting}
              className={`w-full bg-[#1F2937] border rounded-xl px-4 py-2 text-sm text-white placeholder-slate-500 outline-none transition-all focus:bg-[#111827] focus:ring-2 focus:ring-purple-500/40 ${
                errors.itemName ? 'border-red-300 ring-1 ring-red-200' : 'border-slate-700 focus:border-purple-500'
              }`}
            />
            {errors.itemName && (
              <p className="mt-1 text-[11px] text-red-500 font-medium">{errors.itemName}</p>
            )}
          </div>

          {/* Display Group */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1" htmlFor="create-displayGroup">
              Display Group (UI Grouping)
            </label>
            <select
              id="create-displayGroup"
              value={form.displayGroup}
              onChange={handleChange('displayGroup')}
              disabled={submitting}
              className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-3.5 py-2 text-sm text-white outline-none transition-all focus:bg-[#111827] focus:border-purple-500 focus:ring-2 focus:ring-purple-500/40"
            >
              {DISPLAY_GROUPS.map((g) => (
                <option key={g} value={g}>
                  {g}
                </option>
              ))}
            </select>
          </div>

          {/* Unit Cost / Labour Factor */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1" htmlFor="create-unitCost">
              {form.category === 'material' ? 'Unit Cost (LKR / sqft)' : 'Labour Rate Factor'}
            </label>
            <input
              id="create-unitCost"
              type="number"
              step={form.category === 'material' ? '1' : '0.01'}
              min="0"
              placeholder={form.category === 'material' ? 'e.g. 12000' : 'e.g. 0.35'}
              value={form.unitCost}
              onChange={handleChange('unitCost')}
              disabled={submitting}
              className={`w-full bg-[#1F2937] border rounded-xl px-4 py-2 text-sm text-white placeholder-slate-500 outline-none transition-all focus:bg-[#111827] focus:ring-2 focus:ring-purple-500/40 ${
                errors.unitCost ? 'border-red-300 ring-1 ring-red-200' : 'border-slate-700 focus:border-purple-500'
              }`}
            />
            <p className="mt-1 text-[11px] text-slate-400">
              {form.category === 'material'
                ? 'Base cost in LKR per square foot on standard flat terrain.'
                : 'Example: 0.35 means labour is calculated as 35% of material cost.'}
            </p>
            {errors.unitCost && (
              <p className="mt-1 text-[11px] text-red-500 font-medium">{errors.unitCost}</p>
            )}
          </div>

          {/* Terrain Multipliers */}
          <div className="pt-1">
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">
              Terrain Multipliers
            </label>
            <div className="grid grid-cols-3 gap-2.5">
              <div>
                <label htmlFor="create-flatMultiplier" className="block text-[11px] text-slate-400 mb-1 font-medium">Flat</label>
                <input
                  id="create-flatMultiplier"
                  type="number"
                  step="0.05"
                  min="0.1"
                  value={form.flatMultiplier}
                  onChange={handleChange('flatMultiplier')}
                  disabled={submitting}
                  className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-3 py-1.5 text-xs text-white outline-none focus:bg-[#111827] focus:border-purple-500"
                />
              </div>
              <div>
                <label htmlFor="create-hillsideMultiplier" className="block text-[11px] text-slate-400 mb-1 font-medium">Hillside</label>
                <input
                  id="create-hillsideMultiplier"
                  type="number"
                  step="0.05"
                  min="0.1"
                  value={form.hillsideMultiplier}
                  onChange={handleChange('hillsideMultiplier')}
                  disabled={submitting}
                  className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-3 py-1.5 text-xs text-white outline-none focus:bg-[#111827] focus:border-purple-500"
                />
              </div>
              <div>
                <label htmlFor="create-coastalMultiplier" className="block text-[11px] text-slate-400 mb-1 font-medium">Coastal</label>
                <input
                  id="create-coastalMultiplier"
                  type="number"
                  step="0.05"
                  min="0.1"
                  value={form.coastalMultiplier}
                  onChange={handleChange('coastalMultiplier')}
                  disabled={submitting}
                  className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-3 py-1.5 text-xs text-white outline-none focus:bg-[#111827] focus:border-purple-500"
                />
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1" htmlFor="create-region">Region</label>
              <input
                id="create-region"
                type="text"
                value={form.region}
                onChange={handleChange('region')}
                disabled={submitting}
                className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-4 py-2 text-sm text-white outline-none focus:bg-[#111827] focus:border-purple-500"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1" htmlFor="create-qualityLevel">Quality Level</label>
              <select
                id="create-qualityLevel"
                value={form.qualityLevel}
                onChange={handleChange('qualityLevel')}
                disabled={submitting}
                className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-4 py-2 text-sm text-white outline-none focus:bg-[#111827] focus:border-purple-500"
              >
                {['Basic', 'Standard', 'Premium', 'Luxury'].map((level) => <option key={level} value={level}>{level}</option>)}
              </select>
            </div>
          </div>

          {/* Source Reference */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1" htmlFor="create-sourceReference">
              Source Reference (Optional)
            </label>
            <input
              id="create-sourceReference"
              type="text"
              placeholder="e.g. Q3 2026 contractor estimate / supplier catalog"
              value={form.sourceReference}
              onChange={handleChange('sourceReference')}
              disabled={submitting}
              className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-4 py-2 text-sm text-white placeholder-slate-500 outline-none focus:bg-[#111827] focus:border-purple-500"
            />
          </div>
        </div>

        {/* Modal Footer */}
        <div className="px-8 py-5 border-t border-slate-800 flex items-center justify-end gap-3 flex-shrink-0 bg-[#1F2937]/50">
          <button
            onClick={onClose}
            disabled={submitting}
            className="px-5 py-2.5 rounded-xl text-sm font-medium text-slate-400 border border-slate-700 bg-[#111827] hover:bg-[#1F2937] transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            id="modal-create-btn"
            onClick={handleSave}
            disabled={submitting}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-purple-600 hover:bg-purple-500 active:scale-[0.98] text-white transition-all custom-shadow-sm disabled:opacity-60"
          >
            {submitting ? <Loader2 size={14} className="animate-spin" /> : <Plus size={14} />}
            {submitting ? 'Creating…' : 'Create Item'}
          </button>
        </div>
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────
// EditModal sub-component
// ─────────────────────────────────────────────

interface EditModalProps {
  item: PricingItem;
  onClose: () => void;
  onSaveSuccess: (updated: PricingItem) => void;
}

const EditModal: React.FC<EditModalProps> = ({ item, onClose, onSaveSuccess }) => {
  const [form, setForm] = useState<EditFormState>({
    unitCost: String(item.unitCost),
    flatMultiplier: String(item.flatMultiplier),
    hillsideMultiplier: String(item.hillsideMultiplier),
    coastalMultiplier: String(item.coastalMultiplier),
    reason: '',
  });
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const handleChange = (field: keyof EditFormState) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm((prev) => ({ ...prev, [field]: e.target.value }));
    setErrors((prev) => ({ ...prev, [field]: undefined }));
    setSaveError(null);
  };

  const handleSave = async () => {
    const validationErrors = validateEditForm(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setSaving(true);
    setSaveError(null);

    try {
      const updated = await pricingService.update(item.id, {
        unitCostLkr: parseFloat(form.unitCost),
        terrainMultiplier: {
          flat: parseFloat(form.flatMultiplier),
          hillside: parseFloat(form.hillsideMultiplier),
          coastal: parseFloat(form.coastalMultiplier),
        },
        reason: form.reason.trim() || undefined,
      });
      onSaveSuccess(toViewModel(updated));
    } catch (err: unknown) {
      const resp = (err as { response?: { data?: any; status?: number } })?.response;
      const message = typeof resp?.data === 'string' ? resp.data : resp?.data?.message || 'Failed to save changes. Please try again.';
      setSaveError(message);
    } finally {
      setSaving(false);
    }
  };

  const isLabour = item.category === 'labour';

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/30 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="relative bg-[#111827] rounded-3xl border border-slate-800 custom-shadow-lg w-full max-w-lg mx-4 overflow-hidden"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div className="px-8 pt-8 pb-6 border-b border-slate-800">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[10px] font-bold tracking-[0.18em] text-purple-400 uppercase mb-1">
                Edit Pricing
              </p>
              <h2 className="text-xl font-bold text-white leading-snug">{item.name}</h2>
              <div className="flex items-center gap-2 mt-2">
                <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold ${
                  isLabour ? 'bg-amber-900/30 text-amber-300 ring-1 ring-amber-700' : 'bg-purple-900/30 text-purple-300 ring-1 ring-purple-700/50'
                }`}>
                  {item.category}
                </span>
                <span className={`inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-medium ${getGroupColor(item.displayGroup)}`}>
                  {item.displayGroup}
                </span>
                <span className="text-xs text-slate-400">({item.unit})</span>
              </div>
            </div>
            <button
              onClick={onClose}
              className="flex-shrink-0 w-8 h-8 rounded-lg flex items-center justify-center text-slate-400 hover:text-slate-300 hover:bg-[#1F2937] transition-colors"
              aria-label="Close modal"
            >
              <X size={16} />
            </button>
          </div>
        </div>

        {/* Save error banner */}
        {saveError && (
          <div className="mx-8 mt-4 flex items-start gap-2 rounded-xl bg-red-50 border border-red-100 px-4 py-3">
            <AlertCircle size={13} className="mt-0.5 flex-shrink-0 text-red-500" />
            <p className="text-[11px] text-red-600 leading-relaxed">{saveError}</p>
          </div>
        )}

        {/* Modal Body */}
        <div className="px-8 py-6 space-y-4">
          <div>
            <label
              className="block text-xs font-semibold text-slate-300 mb-1.5"
              htmlFor="modal-unitCost"
            >
              {isLabour ? 'Labour Rate Factor' : 'Unit Cost (LKR / sqft)'}
            </label>
            <input
              id="modal-unitCost"
              type="number"
              step={isLabour ? '0.01' : '1'}
              min="0"
              value={form.unitCost}
              onChange={handleChange('unitCost')}
              disabled={saving}
              className={`w-full bg-[#1F2937] border rounded-xl px-4 py-2.5 text-sm text-white outline-none transition-all focus:bg-[#111827] focus:ring-2 focus:ring-purple-500/40 ${
                errors.unitCost ? 'border-red-300 ring-1 ring-red-200' : 'border-slate-700 focus:border-purple-500'
              }`}
            />
            <p className="mt-1 text-[11px] text-slate-400">
              {isLabour
                ? 'Example: 0.35 means labour is calculated as 35% of material cost.'
                : 'Base cost in LKR per square foot on standard flat terrain.'}
            </p>
            {errors.unitCost && (
              <p className="mt-1 text-[11px] text-red-500 font-medium">{errors.unitCost}</p>
            )}
          </div>

          <div className="pt-2">
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">
              Terrain Multipliers
            </label>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <span className="block text-[11px] text-slate-400 mb-1 font-medium">Flat Multiplier</span>
                <input
                  id="modal-flatMultiplier"
                  type="number"
                  step="0.05"
                  min="0.1"
                  value={form.flatMultiplier}
                  onChange={handleChange('flatMultiplier')}
                  disabled={saving}
                  className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-3 py-2 text-xs text-white outline-none focus:bg-[#111827] focus:border-purple-500"
                />
              </div>
              <div>
                <span className="block text-[11px] text-slate-400 mb-1 font-medium">Hillside Multiplier</span>
                <input
                  id="modal-hillsideMultiplier"
                  type="number"
                  step="0.05"
                  min="0.1"
                  value={form.hillsideMultiplier}
                  onChange={handleChange('hillsideMultiplier')}
                  disabled={saving}
                  className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-3 py-2 text-xs text-white outline-none focus:bg-[#111827] focus:border-purple-500"
                />
              </div>
              <div>
                <span className="block text-[11px] text-slate-400 mb-1 font-medium">Coastal Multiplier</span>
                <input
                  id="modal-coastalMultiplier"
                  type="number"
                  step="0.05"
                  min="0.1"
                  value={form.coastalMultiplier}
                  onChange={handleChange('coastalMultiplier')}
                  disabled={saving}
                  className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-3 py-2 text-xs text-white outline-none focus:bg-[#111827] focus:border-purple-500"
                />
              </div>
            </div>
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1" htmlFor="modal-reason">Reason for change</label>
            <input
              id="modal-reason"
              type="text"
              value={form.reason}
              onChange={handleChange('reason')}
              disabled={saving}
              placeholder="e.g. September contractor rate review"
              className="w-full bg-[#1F2937] border border-slate-700 rounded-xl px-4 py-2 text-sm text-white outline-none focus:bg-[#111827] focus:border-purple-500"
            />
          </div>
        </div>

        {/* Modal Footer */}
        <div className="px-8 pb-7 flex items-center justify-end gap-3">
          <button
            onClick={onClose}
            disabled={saving}
            className="px-5 py-2.5 rounded-xl text-sm font-medium text-slate-400 border border-slate-700 bg-[#111827] hover:bg-[#1F2937] transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            id="modal-save-btn"
            onClick={handleSave}
            disabled={saving}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-purple-600 hover:bg-purple-500 active:scale-[0.98] text-white transition-all custom-shadow-sm disabled:opacity-60"
          >
            {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────
// Main Page Component
// ─────────────────────────────────────────────

const TABLE_COLUMNS = [
  'Item Name',
  'Group',
  'Region / Quality',
  'Category',
  'Status',
  'Unit',
  'Rate / Factor',
  'Flat x',
  'Hillside x',
  'Coastal x',
  'Created',
  'Last Updated',
  'Actions',
];

const PricingManagementPage: React.FC = () => {
  // ── Data / async state ──
  const [items, setItems] = useState<PricingItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);

  // ── UI state ──
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<'All' | 'material' | 'labour'>('All');
  const [editingItem, setEditingItem] = useState<PricingItem | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [bannerMessage, setBannerMessage] = useState<string | null>(null);
  const [savedItemId, setSavedItemId] = useState<number | null>(null);
  const [historyItem, setHistoryItem] = useState<PricingItem | null>(null);
  const [history, setHistory] = useState<PricingHistoryItem[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  // ── Fetch on mount ──
  const fetchPricing = async () => {
    setLoading(true);
    setFetchError(null);
    try {
      const data = await pricingService.getAll();
      setItems(data.map(toViewModel));
    } catch {
      setFetchError('Could not load pricing data. Make sure the backend is running and reachable.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPricing();
  }, []);

  const filteredItems = useMemo(() => {
    const q = search.toLowerCase().trim();
    return items.filter((item) => {
      const matchSearch =
        !q ||
        item.name.toLowerCase().includes(q) ||
        item.displayGroup.toLowerCase().includes(q) ||
        item.category.toLowerCase().includes(q) ||
        item.region.toLowerCase().includes(q) ||
        item.qualityLevel.toLowerCase().includes(q) ||
        item.unit.toLowerCase().includes(q);
      const matchCategory =
        selectedCategory === 'All' || item.category === selectedCategory;
      return matchSearch && matchCategory;
    });
  }, [items, search, selectedCategory]);

  const materialCount = useMemo(
    () => items.filter((i) => i.isActive && i.category === 'material').length,
    [items]
  );
  const labourCount = useMemo(
    () => items.filter((i) => i.isActive && i.category === 'labour').length,
    [items]
  );

  // ── Handlers ──
  const handleSaveSuccess = (updated: PricingItem) => {
    setItems((prev) => prev.map((i) => (i.id === updated.id ? updated : i)));
    setSavedItemId(updated.id);
    setEditingItem(null);
    setBannerMessage(`Updated rate for "${updated.name}"`);
    setTimeout(() => {
      setSavedItemId(null);
      setBannerMessage(null);
    }, 3000);
  };

  const handleCreateSuccess = (created: PricingItem) => {
    setItems((prev) => [created, ...prev]);
    setSavedItemId(created.id);
    setIsCreateOpen(false);
    setBannerMessage(`Added new pricing item "${created.name}"`);
    setTimeout(() => {
      setSavedItemId(null);
      setBannerMessage(null);
    }, 3000);
  };

  const handleDeactivate = async (item: PricingItem) => {
    if (!window.confirm(`Deactivate ${item.name}? Existing estimates will remain unchanged.`)) return;
    try {
      const updated = toViewModel(await pricingService.deactivate(item.id, 'Archived from Constructor Pricing Management'));
      setItems((prev) => prev.map((current) => current.id === updated.id ? updated : current));
      setBannerMessage(`Deactivated pricing item "${updated.name}"`);
    } catch {
      setFetchError(`Could not deactivate "${item.name}".`);
    }
  };

  const handleShowHistory = async (item: PricingItem) => {
    setHistoryItem(item);
    setHistoryLoading(true);
    try { setHistory(await pricingService.getHistory(item.id)); }
    catch { setHistory([]); }
    finally { setHistoryLoading(false); }
  };

  // ── Loading state ──
  if (loading) {
    return (
      <div className="space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <p className="text-[10px] font-bold tracking-[0.2em] text-purple-400 uppercase mb-2">Cost Estimator</p>
            <h1 className="text-3xl font-bold text-white tracking-tight mb-2">Pricing Management</h1>
            <p className="text-sm text-slate-400 font-light max-w-lg leading-relaxed">
              Manage base unit costs and terrain multipliers used by the AI Cost Estimation Agent.
            </p>
          </div>
          <div className="flex-shrink-0 flex items-center gap-2 px-4 py-2.5 rounded-xl bg-purple-900/30 border border-indigo-100 text-purple-400 text-xs font-semibold">
            <DollarSign size={14} />
            <span>LKR Pricing Table</span>
          </div>
        </div>
        <div className="bg-[#111827] rounded-2xl border border-slate-800 custom-shadow-sm flex flex-col items-center justify-center py-24 gap-4">
          <Loader2 size={32} className="animate-spin text-indigo-400" />
          <p className="text-sm font-medium text-slate-400">Loading pricing data…</p>
          <p className="text-xs text-slate-400">Fetching from the HousePlanner backend.</p>
        </div>
      </div>
    );
  }

  // ── Error state ──
  if (fetchError) {
    return (
      <div className="space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <p className="text-[10px] font-bold tracking-[0.2em] text-purple-400 uppercase mb-2">Cost Estimator</p>
            <h1 className="text-3xl font-bold text-white tracking-tight mb-2">Pricing Management</h1>
          </div>
        </div>
        <div className="bg-[#111827] rounded-2xl border border-red-100 custom-shadow-sm flex flex-col items-center justify-center py-24 gap-4">
          <div className="w-12 h-12 rounded-2xl bg-red-50 border border-red-100 flex items-center justify-center">
            <AlertCircle size={22} className="text-red-400" />
          </div>
          <p className="text-sm font-semibold text-slate-300">Failed to load pricing data</p>
          <p className="text-xs text-slate-400 max-w-sm text-center">{fetchError}</p>
          <button
            id="pricing-retry-btn"
            onClick={fetchPricing}
            className="inline-flex items-center gap-2 mt-2 px-4 py-2 rounded-xl text-xs font-semibold bg-purple-600 text-white hover:bg-purple-500 transition-colors"
          >
            <RefreshCw size={13} />
            Retry
          </button>
        </div>
      </div>
    );
  }

  // ── Main render ──
  return (
    <div className="space-y-8">
      {/* ── Page Header ── */}
      <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
        <div>
          <p className="text-[10px] font-bold tracking-[0.2em] text-purple-400 uppercase mb-2">
            Cost Estimator
          </p>
          <h1 className="text-3xl font-bold text-white tracking-tight mb-2">
            Pricing Management
          </h1>
          <p className="text-sm text-slate-400 font-light max-w-lg leading-relaxed">
            Manage base unit costs and terrain multipliers used by the AI Cost Estimation Agent.
            Changes here directly govern all automated project cost calculations.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            id="add-pricing-btn"
            onClick={() => setIsCreateOpen(true)}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-semibold shadow-sm transition-all"
          >
            <Plus size={15} />
            <span>Add Pricing Item</span>
          </button>
        </div>
      </div>

      {/* ── Notification Banner ── */}
      {bannerMessage && (
        <div className="rounded-xl bg-emerald-900/30 border border-emerald-200 px-4 py-3 text-xs font-semibold text-emerald-800 flex items-center gap-2">
          <span className="w-2 h-2 rounded-full bg-emerald-900/300"></span>
          {bannerMessage}
        </div>
      )}

      {/* ── Summary Cards ── */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <SummaryCard
          icon={<PackageCheck size={18} />}
          label="Total Items"
          value={String(items.length)}
          sub="Configured in catalog"
        />
        <SummaryCard
          icon={<Building2 size={18} />}
          label="Material Items"
          value={String(materialCount)}
          sub="Rate per sq ft"
        />
        <SummaryCard
          icon={<Users size={18} />}
          label="Labour Factor"
          value={labourCount > 0 ? `${items.find((i) => i.isActive && i.category === 'labour')?.unitCost ?? 0}×` : 'None'}
          sub={labourCount === 1 ? '1 active factor' : labourCount === 0 ? 'Missing labour rate' : 'Multiple rates (Ambiguous)'}
        />
        <SummaryCard
          icon={<CalendarDays size={18} />}
          label="Last Updated"
          value={getLatestUpdate(items)}
          sub="Most recent change"
        />
      </div>

      {/* ── Filters & Table Card ── */}
      <div className="bg-[#111827] rounded-2xl border border-slate-800 custom-shadow-sm overflow-hidden">
        {/* Toolbar */}
        <div className="px-6 py-4 border-b border-slate-50 flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
          {/* Search */}
          <div className="relative w-full sm:max-w-xs">
            <Search
              size={15}
              className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none"
            />
            <input
              id="pricing-search"
              type="text"
              placeholder="Search items, groups…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-4 py-2.5 text-sm bg-[#1F2937] border border-slate-700 rounded-xl text-slate-100 placeholder-slate-500 outline-none focus:bg-[#111827] focus:border-purple-500 focus:ring-2 focus:ring-indigo-400/30 transition-all"
            />
            {search && (
              <button
                onClick={() => setSearch('')}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-400 transition-colors"
                aria-label="Clear search"
              >
                <X size={13} />
              </button>
            )}
          </div>

          {/* Machine Category filter buttons */}
          <div className="flex items-center gap-1.5 flex-wrap">
            <button
              onClick={() => setSelectedCategory('All')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                selectedCategory === 'All'
                  ? 'bg-purple-600 text-white shadow-sm'
                  : 'bg-[#1F2937] text-slate-400 border border-slate-700 hover:bg-[#374151] hover:text-slate-300'
              }`}
            >
              All Items ({items.length})
            </button>
            <button
              onClick={() => setSelectedCategory('material')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                selectedCategory === 'material'
                  ? 'bg-purple-600 text-white shadow-sm'
                  : 'bg-[#1F2937] text-slate-400 border border-slate-700 hover:bg-[#374151] hover:text-slate-300'
              }`}
            >
              Materials ({materialCount})
            </button>
            <button
              onClick={() => setSelectedCategory('labour')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                selectedCategory === 'labour'
                  ? 'bg-amber-600 text-white shadow-sm'
                  : 'bg-[#1F2937] text-slate-400 border border-slate-700 hover:bg-[#374151] hover:text-slate-300'
              }`}
            >
              Labour ({labourCount})
            </button>
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full min-w-[1380px]">
            <thead>
              <tr className="border-b border-slate-800">
                {TABLE_COLUMNS.map((col, i) => (
                  <th
                    key={i}
                    className={`px-5 py-3.5 text-left text-[11px] font-bold tracking-wider text-slate-400 uppercase ${
                      i === 0 ? 'pl-6' : ''
                    } ${i === TABLE_COLUMNS.length - 1 ? 'pr-6 text-right' : ''}`}
                  >
                    {col}
                  </th>
                ))}
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-800/50">
              {items.length === 0 ? (
                /* Empty state */
                <tr>
                  <td colSpan={TABLE_COLUMNS.length} className="px-6 py-16 text-center">
                    <div className="flex flex-col items-center gap-3 text-slate-400">
                      <div className="w-12 h-12 rounded-2xl bg-[#1F2937] border border-slate-800 flex items-center justify-center">
                        <Box size={20} className="text-slate-300" />
                      </div>
                      <p className="text-sm font-medium text-slate-400">No pricing items are configured yet.</p>
                      <p className="text-xs text-slate-400 max-w-sm">
                        Add material rates per sq ft and a labour factor to enable the AI Cost Estimation Agent.
                      </p>
                      <button
                        onClick={() => setIsCreateOpen(true)}
                        className="inline-flex items-center gap-1.5 mt-2 px-4 py-2 rounded-xl text-xs font-semibold bg-purple-600 text-white hover:bg-purple-500 shadow-sm transition-colors"
                      >
                        <Plus size={14} />
                        Add Pricing Item
                      </button>
                    </div>
                  </td>
                </tr>
              ) : filteredItems.length === 0 ? (
                /* Empty search results */
                <tr>
                  <td colSpan={TABLE_COLUMNS.length} className="px-6 py-16 text-center">
                    <div className="flex flex-col items-center gap-3 text-slate-400">
                      <div className="w-12 h-12 rounded-2xl bg-[#1F2937] border border-slate-800 flex items-center justify-center">
                        <Box size={20} className="text-slate-300" />
                      </div>
                      <p className="text-sm font-medium text-slate-400">No matching pricing items found</p>
                      <p className="text-xs text-slate-400">Try adjusting your search or category filter.</p>
                      <button
                        onClick={() => { setSearch(''); setSelectedCategory('All'); }}
                        className="mt-1 text-xs font-semibold text-purple-400 hover:text-purple-300 transition-colors"
                      >
                        Clear filters
                      </button>
                    </div>
                  </td>
                </tr>
              ) : (
                filteredItems.map((item) => (
                  <tr
                    key={item.id}
                    className={`group transition-colors hover:bg-[#1F2937]/70 ${
                      savedItemId === item.id ? 'bg-emerald-900/30/60' : ''
                    }`}
                  >
                    {/* Item Name */}
                    <td className="pl-6 pr-4 py-4">
                      <div className="flex items-center gap-2.5">
                        <div className="w-7 h-7 rounded-lg bg-[#374151] border border-slate-700 flex items-center justify-center flex-shrink-0">
                          <Tag size={13} className="text-slate-400" />
                        </div>
                        <div>
                          <span className="text-sm font-semibold text-slate-100 group-hover:text-slate-950 transition-colors">
                            {item.name}
                          </span>
                          {item.sourceReference && (
                            <p className="text-[11px] text-slate-400 font-light truncate max-w-xs">{item.sourceReference}</p>
                          )}
                        </div>
                        {savedItemId === item.id && (
                          <span className="ml-1 inline-flex items-center gap-1 text-[10px] font-bold text-emerald-400 bg-emerald-900/30 border border-emerald-100 px-1.5 py-0.5 rounded-full">
                            Saved
                          </span>
                        )}
                      </div>
                    </td>

                    {/* Group */}
                    <td className="px-4 py-4">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-medium ${getGroupColor(item.displayGroup)}`}>
                        {item.displayGroup}
                      </span>
                    </td>

                    <td className="px-4 py-4">
                      <p className="text-xs font-semibold text-slate-300">{item.region}</p>
                      <p className="text-[11px] text-slate-400">{item.qualityLevel}</p>
                    </td>

                    {/* Machine Category */}
                    <td className="px-4 py-4">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold ${
                        item.category === 'labour'
                          ? 'bg-amber-900/30 text-amber-300 ring-1 ring-amber-700'
                          : 'bg-purple-900/30 text-purple-300 ring-1 ring-purple-700/50'
                      }`}>
                        {item.category}
                      </span>
                    </td>

                    <td className="px-4 py-4">
                      <span className={`inline-flex rounded-full px-2.5 py-0.5 text-[11px] font-semibold ring-1 ${item.isActive ? 'bg-emerald-900/30 text-emerald-300 ring-emerald-700' : 'bg-[#374151] text-slate-400 ring-slate-200'}`}>
                        {item.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>

                    {/* Canonical Unit */}
                    <td className="px-4 py-4">
                      <span className="text-xs text-slate-400 font-mono font-medium bg-[#374151] px-2 py-0.5 rounded-md">
                        {item.unit}
                      </span>
                    </td>

                    {/* Rate / Factor */}
                    <td className="px-4 py-4">
                      <span className={`text-sm font-bold ${item.category === 'labour' ? 'text-amber-300 font-mono' : 'text-white'}`}>
                        {formatRateOrFactor(item)}
                      </span>
                    </td>

                    {/* Flat Multiplier */}
                    <td className="px-4 py-4"><MultiplierBadge value={item.flatMultiplier} /></td>

                    {/* Hillside Multiplier */}
                    <td className="px-4 py-4"><MultiplierBadge value={item.hillsideMultiplier} /></td>

                    {/* Coastal Multiplier */}
                    <td className="px-4 py-4"><MultiplierBadge value={item.coastalMultiplier} /></td>

                    <td className="px-4 py-4">
                      <span className="text-xs text-slate-400 font-medium">{formatDate(item.createdAt)}</span>
                    </td>

                    {/* Last Updated */}
                    <td className="px-4 py-4">
                      <span className="text-xs text-slate-400 font-medium">
                        {formatDate(item.lastUpdated)}
                      </span>
                      <p className="max-w-[140px] truncate text-[10px] text-slate-400" title={item.updatedByName || (item.updatedByUserId ? 'Unknown user' : 'System')}>
                        {item.updatedByName || (item.updatedByUserId ? 'Unknown user' : 'System')}
                      </p>
                    </td>

                    {/* Edit Action */}
                    <td className="px-4 pr-6 py-4 text-right">
                      <div className="flex justify-end gap-1.5">
                        <button onClick={() => void handleShowHistory(item)} className="inline-flex items-center gap-1 rounded-lg border border-slate-700 px-2 py-1.5 text-xs font-semibold text-slate-400 hover:text-purple-400" aria-label={`History for ${item.name}`}>
                          <History size={11} /> History
                        </button>
                        {item.isActive && <>
                          <button onClick={() => setEditingItem(item)} className="inline-flex items-center gap-1 rounded-lg border border-slate-700 px-2 py-1.5 text-xs font-semibold text-slate-400 hover:text-purple-400" aria-label={`Edit ${item.name}`}>
                            <Pencil size={11} /> Edit
                          </button>
                          <button onClick={() => void handleDeactivate(item)} className="inline-flex items-center gap-1 rounded-lg border border-rose-200 px-2 py-1.5 text-xs font-semibold text-rose-400 hover:bg-rose-900/30" aria-label={`Deactivate ${item.name}`}>
                            <Archive size={11} /> Deactivate
                          </button>
                        </>}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Table Footer */}
        {filteredItems.length > 0 && (
          <div className="px-6 py-3 border-t border-slate-50 flex items-center justify-between">
            <p className="text-xs text-slate-400">
              Showing{' '}
              <span className="font-semibold text-slate-400">{filteredItems.length}</span>
              {' '}of{' '}
              <span className="font-semibold text-slate-400">{items.length}</span>{' '}
              pricing items
            </p>
            <div className="flex items-center gap-1.5 text-[11px] text-slate-400">
              <TrendingUp size={12} />
              <span>Material rates in LKR / sqft · Labour rates as dimensionless factors</span>
            </div>
          </div>
        )}
      </div>

      {/* ── Create Modal ── */}
      {isCreateOpen && (
        <CreateModal
          onClose={() => setIsCreateOpen(false)}
          onCreateSuccess={handleCreateSuccess}
        />
      )}

      {/* ── Edit Modal ── */}
      {editingItem && (
        <EditModal
          item={editingItem}
          onClose={() => setEditingItem(null)}
          onSaveSuccess={handleSaveSuccess}
        />
      )}

      {historyItem && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/30 p-4 backdrop-blur-sm" onClick={() => setHistoryItem(null)}>
          <div className="w-full max-w-xl rounded-2xl border border-slate-700 bg-[#111827] p-6 shadow-xl" onClick={(event) => event.stopPropagation()}>
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <p className="text-[10px] font-bold uppercase tracking-widest text-purple-400">Pricing history</p>
                <h2 className="mt-1 text-xl font-bold text-white">{historyItem.name}</h2>
              </div>
              <button onClick={() => setHistoryItem(null)} className="rounded-lg p-2 text-slate-400 hover:bg-[#374151]" aria-label="Close pricing history"><X size={16} /></button>
            </div>
            {historyLoading ? <p className="py-8 text-center text-sm text-slate-400">Loading history…</p> : history.length === 0 ? (
              <p className="rounded-xl border border-slate-700 bg-[#1F2937] p-5 text-sm text-slate-400">No pricing changes recorded yet.</p>
            ) : (
              <div className="max-h-96 space-y-3 overflow-y-auto">
                {history.map((entry) => (
                  <div key={entry.id} className="rounded-xl border border-slate-700 p-4">
                    <div className="flex items-center justify-between gap-3">
                      <p className="font-semibold text-white">{entry.previousValue.toLocaleString()} → {entry.newValue.toLocaleString()}</p>
                      <time className="text-xs text-slate-400">{new Date(entry.changedAt).toLocaleString()}</time>
                    </div>
                    <p className="mt-1 text-xs text-slate-400">{entry.reason || 'No reason provided'}</p>
                    <p className="mt-1 text-[10px] text-slate-400">Updated by: {entry.changedByName || (entry.changedByUserId ? 'Unknown user' : 'System')}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default PricingManagementPage;
