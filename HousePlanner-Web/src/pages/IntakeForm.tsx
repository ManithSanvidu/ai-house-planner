import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { CheckCircle2, ChevronRight, ChevronLeft } from 'lucide-react';

interface LandRangeDto { id: string; label: string; minPerches: number; maxPerches: number; approxSqft: string; }
interface FeatureAvailabilityDto { available: boolean; reason?: string; }
interface DesignOptionsResponseDto {
  landRanges: LandRangeDto[];
  plotShapes: string[];
  bedrooms: number[];
  bathrooms: number[];
  floors: number[];
  architecturalStyles: string[];
  features: Record<string, FeatureAvailabilityDto>;
  validatedDesignCount: number;
}

interface IntakeFormData {
  landRangeId: string;
  plotShape: string;
  terrainType: string;
  roadSide: string;
  plotWidth: string;
  plotLength: string;
  floors: string;
  bedrooms: string;
  bathrooms: string;
  architecturalStyle: string;
  spacePriority: string;
  openPlan: boolean;
  masterEnsuite: boolean;
  separateDining: boolean;
  homeOffice: boolean;
  balcony: boolean;
  veranda: boolean;
  utilityRoom: boolean;
  parkingRequired: boolean;
  accessibility: boolean;
}

const IntakeForm: React.FC = () => {
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [options, setOptions] = useState<DesignOptionsResponseDto | null>(null);
  
  const [formData, setFormData] = useState<IntakeFormData>({
    landRangeId: '', plotShape: 'BALANCED', terrainType: 'flat/urban', roadSide: 'south',
    plotWidth: '', plotLength: '',
    floors: '', bedrooms: '', bathrooms: '', architecturalStyle: '',
    spacePriority: 'balanced',
    openPlan: false, masterEnsuite: false, separateDining: false,
    homeOffice: false, balcony: false, veranda: false, utilityRoom: false,
    parkingRequired: false, accessibility: false,
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [workflowId, setWorkflowId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState('');
  const [conflicts, setConflicts] = useState<string[]>([]);
  const [suggestions, setSuggestions] = useState<any[]>([]);

  const applySuggestion = (field: string, value: any) => {
    const nextData = { ...formData, [field]: value };
    setFormData(nextData);
    setConflicts([]);
    setErrorMessage('');
    fetchCompatibleOptions(nextData);
  };

  // Initial fetch
  useEffect(() => {
    fetch('http://localhost:5265/api/v1/design-options')
      .then(res => res.json())
      .then((data: DesignOptionsResponseDto) => {
        setOptions(data);
        if (!formData.landRangeId && data.landRanges.length > 0) {
          setFormData(prev => ({ ...prev, landRangeId: data.landRanges[0].id }));
        }
      })
      .catch(err => console.error("Failed to load options", err));
  }, []);

  const fetchCompatibleOptions = useCallback(async (currentData: IntakeFormData) => {
    try {
      const payload = {
        landRangeId: currentData.landRangeId || undefined,
        plotShape: currentData.plotShape || undefined,
        floors: currentData.floors ? parseInt(currentData.floors) : undefined,
        bedrooms: currentData.bedrooms ? parseInt(currentData.bedrooms) : undefined,
        bathrooms: currentData.bathrooms ? parseInt(currentData.bathrooms) : undefined,
        architecturalStyle: currentData.architecturalStyle || undefined,
        openPlan: currentData.openPlan,
        masterEnsuite: currentData.masterEnsuite,
        separateDining: currentData.separateDining,
        homeOffice: currentData.homeOffice,
        balcony: currentData.balcony,
        veranda: currentData.veranda,
        utilityRoom: currentData.utilityRoom,
        parkingRequired: currentData.parkingRequired,
        accessibility: currentData.accessibility
      };
      
      const res = await fetch('http://localhost:5265/api/v1/design-options/compatible', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      const data: DesignOptionsResponseDto = await res.json();
      setOptions(data);

      // Auto-clear invalid selections
      setFormData(prev => {
        const next = { ...prev };
        if (next.floors && !data.floors.includes(parseInt(next.floors))) next.floors = '';
        if (next.bedrooms && !data.bedrooms.includes(parseInt(next.bedrooms))) next.bedrooms = '';
        if (next.bathrooms && !data.bathrooms.includes(parseInt(next.bathrooms))) next.bathrooms = '';
        if (next.architecturalStyle && !data.architecturalStyles.includes(next.architecturalStyle)) next.architecturalStyle = '';
        
        ['openPlan', 'masterEnsuite', 'separateDining', 'homeOffice', 'balcony', 'veranda', 'utilityRoom', 'parkingRequired', 'accessibility'].forEach(feat => {
          const key = feat === 'openPlan' ? 'open_plan' : feat === 'masterEnsuite' ? 'master_ensuite' : feat === 'separateDining' ? 'separate_dining' : feat === 'homeOffice' ? 'home_office' : feat === 'utilityRoom' ? 'utility_room' : feat === 'parkingRequired' ? 'parking' : feat;
          if (next[feat as keyof IntakeFormData] && !data.features[key]?.available) {
            (next as any)[feat] = false;
          }
        });
        return next;
      });
    } catch (err) {
      console.error(err);
    }
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    const nextData = { ...formData, [name]: value };
    if (name === 'floors' && value === '1') nextData.balcony = false;
    setFormData(nextData);
    
    if (['landRangeId', 'plotShape', 'floors', 'bedrooms', 'bathrooms', 'architecturalStyle'].includes(name)) {
      fetchCompatibleOptions(nextData);
    }
  };

  const handleToggle = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, checked } = e.target;
    const nextData = { ...formData, [name]: checked };
    setFormData(nextData);
    fetchCompatibleOptions(nextData);
  };

  const nextStep = () => {
    setErrorMessage('');
    setConflicts([]);
    if (step === 1 && formData.plotShape === 'CUSTOM_DIMENSIONS') {
      if (!formData.plotWidth || !formData.plotLength) {
        setErrorMessage("Please enter both width and length for custom dimensions.");
        return;
      }
    }
    if (step === 2) {
      if (!formData.floors || !formData.bedrooms || !formData.bathrooms || !formData.architecturalStyle) {
        setErrorMessage("Please select all house requirements.");
        return;
      }
    }
    setStep(s => s + 1);
  };

  const prevStep = () => setStep(s => s - 1);

  const handleSubmit = async () => {
    setIsSubmitting(true);
    setErrorMessage('');
    try {
      const selectedRange = options?.landRanges.find(r => r.id === formData.landRangeId);
      const landSizePerches = selectedRange ? selectedRange.minPerches + ((selectedRange.maxPerches - selectedRange.minPerches)/2) : 10;
      
      const payload: any = {
        landSizePerches,
        manualTerrainType: formData.terrainType,
        preferences: {
          bedrooms: parseInt(formData.bedrooms) || 3,
          bathrooms: parseInt(formData.bathrooms) || 1,
          floors: parseInt(formData.floors) || 1,
          architecturalStyle: formData.architecturalStyle,
          openPlan: formData.openPlan,
          masterEnsuite: formData.masterEnsuite,
          separateDining: formData.separateDining,
          homeOffice: formData.homeOffice,
          balcony: formData.balcony,
          veranda: formData.veranda,
          utilityRoom: formData.utilityRoom,
          parkingRequired: formData.parkingRequired,
          accessibility: formData.accessibility,
          spacePriority: formData.spacePriority,
          circulationPreference: 'space_efficient'
        }
      };

      payload.plotConstraints = {
        road_side: formData.roadSide,
        entrance_side: formData.roadSide,
        ...(formData.plotShape === 'CUSTOM_DIMENSIONS' && formData.plotWidth ? { plot_width_ft: Number(formData.plotWidth) } : {}),
        ...(formData.plotShape === 'CUSTOM_DIMENSIONS' && formData.plotLength ? { plot_length_ft: Number(formData.plotLength) } : {})
      };
      
      payload.designSeed = crypto.getRandomValues(new Uint32Array(1))[0];

      const res = await fetch('http://localhost:5265/api/v1/ai-generation/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      
      if (!res.ok) {
        const err = await res.json();
        if (err.code === 'UNSUPPORTED_DESIGN_CONFIGURATION') {
            setConflicts(err.conflicts || []);
            setSuggestions(err.suggestions || []);
            throw new Error('');
        }
        throw new Error(err.message || "Failed to generate design");
      }
      const result = await res.json();
      setWorkflowId(result.workflowId);
      setIsSuccess(true);
    } catch (error: any) {
      if (error.message) {
        setErrorMessage(error.message || 'An error occurred while connecting to the server.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isSuccess && workflowId) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] text-center p-8 bg-white/80 dark:bg-gray-900/80 backdrop-blur-xl rounded-[2rem] border border-white/60 dark:border-gray-800 shadow-[0_8px_30px_rgb(0,0,0,0.04)] max-w-2xl mx-auto">
        <CheckCircle2 size={64} className="text-emerald-500 mb-6" />
        <h2 className="text-3xl font-extrabold text-zinc-900 dark:text-white mb-3">AI Plan Generated!</h2>
        <button onClick={() => navigate(`/dashboard/workflows/${workflowId}`)} className="px-8 py-4 bg-indigo-600 text-white font-bold rounded-2xl">Review Floor Plan</button>
      </div>
    );
  }

  if (!options) return <div className="text-center p-10">Loading design options...</div>;

  return (
    <div className="max-w-3xl mx-auto p-8 sm:p-10 bg-white/90 dark:bg-gray-900/90 backdrop-blur-xl rounded-[2rem] shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-white/60 dark:border-gray-800/80 relative">
      <div className="mb-10 text-center">
        <h1 className="text-3xl font-extrabold text-zinc-900 dark:text-white tracking-tight mb-2">New Project Setup</h1>
        <p className="text-zinc-500 dark:text-gray-400">Step {step} of 5</p>
      </div>

      {errorMessage && <div className="mb-6 p-4 bg-red-50 text-red-600 rounded-lg text-sm border border-red-200">{errorMessage}</div>}

      <div className="space-y-6">
        {step === 1 && (
          <div className="space-y-4">
            <h3 className="font-bold text-zinc-900 dark:text-white text-xl">Land Details</h3>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Land Size Range</label>
              <select name="landRangeId" value={formData.landRangeId} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                {options.landRanges.map(r => (
                  <option key={r.id} value={r.id}>{r.label} (Approx. {r.approxSqft})</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Plot Shape</label>
              <select name="plotShape" value={formData.plotShape} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                <option value="NARROW_DEEP">Narrow / Deep</option>
                <option value="BALANCED">Balanced</option>
                <option value="WIDE_SHALLOW">Wide / Shallow</option>
                <option value="CUSTOM_DIMENSIONS">I know my dimensions</option>
              </select>
            </div>
            {formData.plotShape === 'CUSTOM_DIMENSIONS' && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Width (ft)</label>
                  <input type="number" name="plotWidth" value={formData.plotWidth} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Length (ft)</label>
                  <input type="number" name="plotLength" value={formData.plotLength} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
                </div>
              </div>
            )}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Terrain</label>
                <select name="terrainType" value={formData.terrainType} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                  <option value="flat/urban">Flat / Urban</option>
                  <option value="hillside">Hillside / Sloped</option>
                  <option value="coastal">Coastal</option>
                  <option value="forested">Forested</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Road Side</label>
                <select name="roadSide" value={formData.roadSide} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                  {['south', 'north', 'east', 'west'].map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
            </div>
          </div>
        )}

        {step === 2 && (
          <div className="space-y-4">
            <h3 className="font-bold text-zinc-900 dark:text-white text-xl">House Details</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Floors</label>
                <select name="floors" value={formData.floors} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                  <option value="">Select...</option>
                  {options.floors.map(f => <option key={f} value={f}>{f}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bedrooms</label>
                <select name="bedrooms" value={formData.bedrooms} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                  <option value="">Select...</option>
                  {options.bedrooms.map(b => <option key={b} value={b}>{b}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bathrooms</label>
                <select name="bathrooms" value={formData.bathrooms} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                  <option value="">Select...</option>
                  {options.bathrooms.map(b => <option key={b} value={b}>{b}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Style</label>
                <select name="architecturalStyle" value={formData.architecturalStyle} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                  <option value="">Select...</option>
                  {options.architecturalStyles.map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
            </div>
          </div>
        )}

        {step === 3 && (
          <div className="space-y-4">
            <h3 className="font-bold text-zinc-900 dark:text-white text-xl">Priorities</h3>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Space Priority</label>
              <select name="spacePriority" value={formData.spacePriority} onChange={handleInputChange} className="w-full px-4 py-2 rounded-lg border border-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                <option value="compact_cost_efficient">Compact &amp; Cost Efficient</option>
                <option value="balanced">Balanced</option>
                <option value="outdoor_garden">Outdoor / Garden Priority</option>
              </select>
            </div>
            
            <div className="mt-4 border rounded-xl p-4 border-gray-200 dark:border-gray-700">
              <label className={`flex items-start gap-3 text-sm text-gray-700 dark:text-gray-200 ${!options.features['accessibility']?.available ? 'opacity-50' : ''}`}>
                <input type="checkbox" name="accessibility" checked={formData.accessibility} onChange={handleToggle} disabled={!options.features['accessibility']?.available} className="mt-1 h-4 w-4" />
                <div>
                  <span className="font-medium">Accessibility (Reduced-step layout)</span>
                  {!options.features['accessibility']?.available && <p className="text-xs text-red-500 mt-1">{options.features['accessibility'].reason}</p>}
                </div>
              </label>
            </div>
          </div>
        )}

        {step === 4 && (
          <div className="space-y-4">
            <h3 className="font-bold text-zinc-900 dark:text-white text-xl">Optional Features</h3>
            <p className="text-sm text-gray-500">Only features compatible with your current selections are available.</p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mt-2">
              {[
                { name: 'openPlan', key: 'open_plan', label: 'Open-plan Living/Dining' },
                { name: 'masterEnsuite', key: 'master_ensuite', label: 'Master Ensuite' },
                { name: 'separateDining', key: 'separate_dining', label: 'Separate Dining' },
                { name: 'homeOffice', key: 'home_office', label: 'Home Office' },
                { name: 'balcony', key: 'balcony', label: 'Balcony' },
                { name: 'veranda', key: 'veranda', label: 'Veranda' },
                { name: 'utilityRoom', key: 'utility_room', label: 'Utility Room' },
                { name: 'parkingRequired', key: 'parking', label: 'Parking' }
              ].map(f => {
                const avail = options.features[f.key]?.available;
                const reason = options.features[f.key]?.reason;
                return (
                  <div key={f.name} className={`border rounded-xl p-4 border-gray-200 dark:border-gray-700 ${!avail ? 'opacity-50 bg-gray-50 dark:bg-gray-800' : ''}`}>
                    <label className="flex items-start gap-3 text-sm text-gray-700 dark:text-gray-200 cursor-pointer">
                      <input type="checkbox" name={f.name} checked={(formData as any)[f.name]} onChange={handleToggle} disabled={!avail} className="mt-1 h-4 w-4" />
                      <div>
                        <span className="font-medium">{f.label}</span>
                        {!avail && <p className="text-xs text-red-500 mt-1">{reason}</p>}
                      </div>
                    </label>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {step === 5 && (
          <div className="space-y-4">
            <h3 className="font-bold text-zinc-900 dark:text-white text-xl">Review Configuration</h3>
            <div className="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 space-y-2 text-sm text-gray-700 dark:text-gray-300">
              <p><strong>Land:</strong> {options.landRanges.find(r => r.id === formData.landRangeId)?.label}, {formData.plotShape}</p>
              <p><strong>House:</strong> {formData.floors} floors, {formData.bedrooms} beds, {formData.bathrooms} baths</p>
              <p><strong>Style:</strong> {formData.architecturalStyle}</p>
              <p><strong>Priority:</strong> {formData.spacePriority}</p>
              <p><strong>Selected extras:</strong> {[
                ['openPlan', 'Open plan'], ['masterEnsuite', 'Master ensuite'], ['separateDining', 'Separate dining'],
                ['homeOffice', 'Home office'], ['balcony', 'Balcony'], ['veranda', 'Veranda'],
                ['utilityRoom', 'Utility room'], ['parkingRequired', 'Parking'], ['accessibility', 'Accessibility']
              ].filter(([key]) => Boolean(formData[key as keyof IntakeFormData])).map(([, label]) => label).join(', ') || 'None'}</p>
            </div>
            <p className={`font-semibold ${options.validatedDesignCount === 0 ? 'text-red-600' : 'text-emerald-600'}`}>
              Validated designs available: {options.validatedDesignCount}
            </p>
            
            {conflicts.length > 0 && (
                <div className="mt-6 p-6 border rounded-2xl bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800/30 text-center">
                    <p className="text-red-700 dark:text-red-400 font-medium mb-4">This combination is not currently available.</p>
                    <div className="flex flex-col gap-3">
                        {suggestions.map((s: any, idx: number) => (
                            <button key={idx} type="button" onClick={() => applySuggestion(s.field, s.value)} className="px-4 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 rounded-xl font-medium hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors shadow-sm">
                                {s.label}
                            </button>
                        ))}
                        <button type="button" onClick={() => { setErrorMessage(''); setConflicts([]); }} className="text-indigo-600 dark:text-indigo-400 text-sm font-medium hover:underline mt-2">
                            Edit configuration
                        </button>
                    </div>
                </div>
            )}
          </div>
        )}

        <div className="flex justify-between pt-6 border-t border-gray-100 dark:border-gray-800">
          {step > 1 ? (
            <button type="button" onClick={prevStep} className="px-6 py-3 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 font-semibold rounded-xl flex items-center gap-2">
              <ChevronLeft size={18} /> Back
            </button>
          ) : <div></div>}
          
          {step < 5 ? (
            <button type="button" onClick={nextStep} className="px-6 py-3 bg-indigo-600 text-white font-semibold rounded-xl flex items-center gap-2">
              Next <ChevronRight size={18} />
            </button>
          ) : (
            <button type="button" onClick={handleSubmit} disabled={isSubmitting || options.validatedDesignCount === 0} className="px-8 py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl flex items-center gap-2 disabled:opacity-70">
              {isSubmitting ? "Validating..." : "Generate AI Plan"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default IntakeForm;
