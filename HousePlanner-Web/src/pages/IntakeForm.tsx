import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Check, CheckCircle2 } from 'lucide-react';

interface LandRangeDto { id: string; label: string; minPerches: number; maxPerches: number; approxSqft: string; }
interface FeatureAvailabilityDto { available: boolean; reason?: string; }
interface DesignOptionsResponseDto {
  landRanges: LandRangeDto[]; plotShapes: string[]; bedrooms: number[]; bathrooms: number[];
  floors: number[]; architecturalStyles: string[]; features: Record<string, FeatureAvailabilityDto>;
  validatedDesignCount: number;
}
interface IntakeFormData {
  landRangeId: string; plotShape: string; terrainType: string; roadSide: string;
  plotWidth: string; plotLength: string; floors: string; bedrooms: string; bathrooms: string;
  architecturalStyle: string; spacePriority: string; openPlan: boolean; masterEnsuite: boolean;
  separateDining: boolean; homeOffice: boolean; balcony: boolean; veranda: boolean;
  utilityRoom: boolean; parkingRequired: boolean; accessibility: boolean;
}

const featureDefinitions = [
  ['openPlan', 'open_plan', 'Open plan'], ['masterEnsuite', 'master_ensuite', 'Master ensuite'],
  ['separateDining', 'separate_dining', 'Separate dining'], ['homeOffice', 'home_office', 'Home office'],
  ['balcony', 'balcony', 'Balcony'], ['veranda', 'veranda', 'Veranda'],
  ['utilityRoom', 'utility_room', 'Utility room'], ['parkingRequired', 'parking', 'Parking'],
] as const;

const initialForm: IntakeFormData = {
  landRangeId: '', plotShape: 'BALANCED', terrainType: 'flat/urban', roadSide: 'south',
  plotWidth: '', plotLength: '', floors: '', bedrooms: '', bathrooms: '', architecturalStyle: '',
  spacePriority: 'balanced', openPlan: false, masterEnsuite: false, separateDining: false,
  homeOffice: false, balcony: false, veranda: false, utilityRoom: false,
  parkingRequired: false, accessibility: false,
};

const IntakeForm: React.FC = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState(initialForm);
  const [options, setOptions] = useState<DesignOptionsResponseDto | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [workflowId, setWorkflowId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState('');

  const fetchCompatibleOptions = useCallback(async (current: IntakeFormData) => {
    try {
      const response = await fetch('http://localhost:5265/api/v1/design-options/compatible', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          landRangeId: current.landRangeId || undefined, plotShape: current.plotShape,
          floors: current.floors ? Number(current.floors) : undefined,
          bedrooms: current.bedrooms ? Number(current.bedrooms) : undefined,
          bathrooms: current.bathrooms ? Number(current.bathrooms) : undefined,
          architecturalStyle: current.architecturalStyle || undefined,
          openPlan: current.openPlan, masterEnsuite: current.masterEnsuite,
          separateDining: current.separateDining, homeOffice: current.homeOffice,
          balcony: current.balcony, veranda: current.veranda, utilityRoom: current.utilityRoom,
          parkingRequired: current.parkingRequired, accessibility: current.accessibility,
        }),
      });
      const data: DesignOptionsResponseDto = await response.json();
      setOptions(data);
      setFormData(previous => {
        const next = { ...previous };
        for (const [field, key] of featureDefinitions) {
          if (next[field] && !data.features[key]?.available) next[field] = false;
        }
        if (next.accessibility && !data.features.accessibility?.available) next.accessibility = false;
        return next;
      });
    } catch { setErrorMessage('Could not refresh compatible designs.'); }
  }, []);

  useEffect(() => {
    fetch('http://localhost:5265/api/v1/design-options').then(response => response.json())
      .then((data: DesignOptionsResponseDto) => {
        setOptions(data);
        setFormData(previous => ({ ...previous, landRangeId: previous.landRangeId || data.landRanges[0]?.id || '' }));
      }).catch(() => setErrorMessage('Could not load design options.'));
  }, []);

  const update = (field: keyof IntakeFormData, value: string | boolean) => {
    const next = { ...formData, [field]: value };
    if (field === 'floors' && value === '1') next.balcony = false;
    setFormData(next); setErrorMessage('');
    fetchCompatibleOptions(next);
  };

  const submit = async () => {
    if (!options || options.validatedDesignCount === 0) return;
    if (!formData.floors || !formData.bedrooms || !formData.bathrooms || !formData.architecturalStyle) {
      setErrorMessage('Choose floors, bedrooms, bathrooms, and an architectural style.'); return;
    }
    setIsSubmitting(true); setErrorMessage('');
    try {
      const range = options.landRanges.find(item => item.id === formData.landRangeId);
      const response = await fetch('http://localhost:5265/api/v1/ai-generation/generate', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          landSizePerches: range ? range.minPerches + (range.maxPerches - range.minPerches) / 2 : 10,
          manualTerrainType: formData.terrainType,
          preferences: {
            bedrooms: Number(formData.bedrooms), bathrooms: Number(formData.bathrooms), floors: Number(formData.floors),
            architecturalStyle: formData.architecturalStyle, openPlan: formData.openPlan,
            masterEnsuite: formData.masterEnsuite, separateDining: formData.separateDining,
            homeOffice: formData.homeOffice, balcony: formData.balcony, veranda: formData.veranda,
            utilityRoom: formData.utilityRoom, parkingRequired: formData.parkingRequired,
            accessibility: formData.accessibility, spacePriority: formData.spacePriority,
            circulationPreference: 'space_efficient',
          },
          plotConstraints: {
            road_side: formData.roadSide, entrance_side: formData.roadSide,
            ...(formData.plotWidth ? { plot_width_ft: Number(formData.plotWidth) } : {}),
            ...(formData.plotLength ? { plot_length_ft: Number(formData.plotLength) } : {}),
          },
          designSeed: crypto.getRandomValues(new Uint32Array(1))[0],
        }),
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result.message || 'Failed to generate design.');
      setWorkflowId(result.workflowId);
    } catch (error: any) { setErrorMessage(error.message || 'Failed to generate design.'); }
    finally { setIsSubmitting(false); }
  };

  if (!options) return <div className="p-10 text-center">Loading design options…</div>;
  if (workflowId) return <div className="max-w-xl mx-auto p-10 text-center bg-white rounded-3xl shadow-sm">
    <CheckCircle2 className="mx-auto text-emerald-500 mb-4" size={56}/>
    <h2 className="text-2xl font-bold mb-3">Design generation started</h2>
    <p className="text-zinc-500 mb-6">Your project and every generated version are saved to My Designs.</p>
    <button onClick={() => navigate(`/dashboard/workflows/${workflowId}`)} className="px-6 py-3 bg-indigo-600 text-white rounded-xl font-bold">Open Design</button>
  </div>;

  const card = 'rounded-2xl border border-zinc-200 dark:border-gray-700 bg-white dark:bg-gray-900 p-5 shadow-sm';
  const selectClass = 'w-full rounded-xl border border-zinc-200 dark:border-gray-700 bg-white dark:bg-gray-950 px-3 py-2.5';
  const Choice = ({ field, value, label }: { field: keyof IntakeFormData; value: string; label: string }) =>
    <button type="button" onClick={() => update(field, value)} className={`px-3 py-2 rounded-xl border text-sm font-semibold ${formData[field] === value ? 'bg-indigo-600 border-indigo-600 text-white' : 'bg-white dark:bg-gray-950 border-zinc-200 dark:border-gray-700'}`}>{label}</button>;

  return <div className="max-w-6xl mx-auto p-4 sm:p-8">
    <div className="mb-7"><h1 className="text-3xl font-extrabold">Create a new home design</h1><p className="text-zinc-500 mt-2">Set your requirements on one page. Availability updates as you choose.</p></div>
    {errorMessage && <div role="alert" className="mb-5 p-4 rounded-xl bg-red-50 text-red-700 border border-red-200">{errorMessage}</div>}
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start">
      <section className={card}><h2 className="text-lg font-bold mb-4">1. Land Details</h2><div className="space-y-4">
        <label className="block text-sm font-medium">Land size range<select aria-label="Land size range" value={formData.landRangeId} onChange={e => update('landRangeId', e.target.value)} className={`${selectClass} mt-1`}>{options.landRanges.map(r => <option key={r.id} value={r.id}>{r.label} · {r.approxSqft}</option>)}</select></label>
        <div><span className="text-sm font-medium">Plot shape</span><div className="flex flex-wrap gap-2 mt-2">{[['NARROW_DEEP','Narrow / deep'],['BALANCED','Balanced'],['WIDE_SHALLOW','Wide / shallow'],['CUSTOM_DIMENSIONS','Custom']].map(([value,label]) => <Choice key={value} field="plotShape" value={value} label={label}/>)}</div></div>
        <div className="grid grid-cols-2 gap-3"><label className="text-sm font-medium">Terrain<select aria-label="Terrain" value={formData.terrainType} onChange={e => update('terrainType', e.target.value)} className={`${selectClass} mt-1`}><option value="flat/urban">Flat / Urban</option><option value="hillside">Hillside</option><option value="coastal">Coastal</option><option value="forested">Forested</option></select></label><label className="text-sm font-medium">Road side<select aria-label="Road side" value={formData.roadSide} onChange={e => update('roadSide', e.target.value)} className={`${selectClass} mt-1`}>{['south','north','east','west'].map(side => <option key={side}>{side}</option>)}</select></label></div>
        <div className="grid grid-cols-2 gap-3"><label className="text-sm">Exact width (ft), optional<input aria-label="Exact width" type="number" value={formData.plotWidth} onChange={e => update('plotWidth', e.target.value)} className={`${selectClass} mt-1`}/></label><label className="text-sm">Exact length (ft), optional<input aria-label="Exact length" type="number" value={formData.plotLength} onChange={e => update('plotLength', e.target.value)} className={`${selectClass} mt-1`}/></label></div>
      </div></section>

      <section className={card}><h2 className="text-lg font-bold mb-4">2. House Requirements</h2><div className="space-y-4">
        <div><span className="text-sm font-medium">Floors</span><div className="flex gap-2 mt-2">{options.floors.map(value => <Choice key={value} field="floors" value={String(value)} label={`${value} floor${value > 1 ? 's' : ''}`}/>)}</div></div>
        <div><span className="text-sm font-medium">Bedrooms</span><div className="flex flex-wrap gap-2 mt-2">{options.bedrooms.map(value => <Choice key={value} field="bedrooms" value={String(value)} label={`${value} bed`}/>)}</div></div>
        <div><span className="text-sm font-medium">Bathrooms</span><div className="flex flex-wrap gap-2 mt-2">{options.bathrooms.map(value => <Choice key={value} field="bathrooms" value={String(value)} label={`${value} bath`}/>)}</div></div>
        <label className="block text-sm font-medium">Architectural style<select aria-label="Architectural style" value={formData.architecturalStyle} onChange={e => update('architecturalStyle', e.target.value)} className={`${selectClass} mt-1`}><option value="">Choose a style</option>{options.architecturalStyles.map(style => <option key={style}>{style}</option>)}</select></label>
      </div></section>

      <section className={card}><h2 className="text-lg font-bold mb-4">3. Priority</h2><div className="flex flex-wrap gap-2">{[['compact_cost_efficient','Compact / cost-efficient'],['balanced','Balanced'],['outdoor_garden','Outdoor / garden']].map(([value,label]) => <Choice key={value} field="spacePriority" value={value} label={label}/>)}</div>
        <Toggle label="Accessibility" checked={formData.accessibility} available={options.features.accessibility?.available} reason={options.features.accessibility?.reason} onChange={value => update('accessibility', value)}/>
      </section>

      <section className={card}><h2 className="text-lg font-bold mb-4">4. Optional Features</h2><div className="grid grid-cols-1 sm:grid-cols-2 gap-3">{featureDefinitions.map(([field,key,label]) => <Toggle key={field} label={label} checked={formData[field]} available={options.features[key]?.available} reason={options.features[key]?.reason} onChange={value => update(field, value)}/>)}</div></section>
    </div>
    <div className="sticky bottom-4 mt-6 rounded-2xl border border-zinc-200 bg-white/95 dark:bg-gray-900/95 backdrop-blur p-4 shadow-lg flex flex-col sm:flex-row gap-4 items-center justify-between">
      <div><p className={`font-bold ${options.validatedDesignCount === 0 ? 'text-red-600' : 'text-emerald-600'}`}>Validated designs available: {options.validatedDesignCount}</p><p className="text-xs text-zinc-500">Your generated versions will be saved automatically.</p></div>
      <button type="button" onClick={submit} disabled={isSubmitting || options.validatedDesignCount === 0} className="px-7 py-3 rounded-xl bg-indigo-600 text-white font-bold disabled:opacity-50">{isSubmitting ? 'Generating…' : 'Generate Design'}</button>
    </div>
  </div>;
};

const Toggle = ({ label, checked, available, reason, onChange }: { label: string; checked: boolean; available?: boolean; reason?: string; onChange: (value: boolean) => void }) =>
  <label className={`block rounded-xl border p-3 mt-3 ${available ? 'cursor-pointer' : 'opacity-55 bg-zinc-50 dark:bg-gray-800'}`}><span className="flex items-center gap-3"><input type="checkbox" checked={checked} disabled={!available} onChange={e => onChange(e.target.checked)} className="sr-only"/><span className={`w-5 h-5 rounded-md border flex items-center justify-center ${checked ? 'bg-indigo-600 border-indigo-600 text-white' : 'border-zinc-300'}`}>{checked && <Check size={14}/>}</span><span className="font-medium text-sm">{label}</span></span>{!available && <span className="block text-xs text-red-600 mt-2">{reason || 'No validated design is available for this configuration.'}</span>}</label>;

export default IntakeForm;
