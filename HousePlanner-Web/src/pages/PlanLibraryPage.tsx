import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Bath, BedDouble, Layers3, Ruler, Search } from 'lucide-react';
import { preDesignedPlanService, type PlanFilters, type PreDesignedPlanSummary } from '../services/preDesignedPlanService';

export default function PlanLibraryPage(){
 const [plans,setPlans]=useState<PreDesignedPlanSummary[]>([]), [filters,setFilters]=useState<PlanFilters>({}), [loading,setLoading]=useState(true), [error,setError]=useState('');
 useEffect(()=>{setLoading(true); preDesignedPlanService.list(filters).then(setPlans).catch(()=>setError('Could not load the plan library.')).finally(()=>setLoading(false));},[filters]);
 const field=(key:keyof PlanFilters,value:string)=>setFilters(f=>({...f,[key]:value===''?undefined:['bedrooms','bathrooms','floors','minimumLandSizePerches','maximumBuiltUpArea'].includes(key)?Number(value):value}));
 return <main className="p-6 md:p-10 max-w-7xl mx-auto text-zinc-900 dark:text-white">
  <div className="mb-8"><p className="text-indigo-500 font-semibold text-sm">CONCEPTUAL PLAN LIBRARY</p><h1 className="text-3xl font-bold mt-1">Pre-designed house plans</h1><p className="text-zinc-500 mt-2">Compare practical starting points before creating your project.</p></div>
  <div className="grid md:grid-cols-4 gap-3 mb-8 bg-white dark:bg-gray-900 p-4 rounded-2xl border border-zinc-200 dark:border-gray-800">
   <label className="relative md:col-span-2"><Search className="absolute left-3 top-3 text-zinc-400" size={18}/><input aria-label="Search plans" className="w-full pl-10 p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border border-zinc-200 dark:border-gray-700" placeholder="Search name, style or tag" onChange={e=>field('search',e.target.value)}/></label>
   <select aria-label="Bedrooms" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" onChange={e=>field('bedrooms',e.target.value)}><option value="">Any bedrooms</option>{[2,3,4].map(x=><option key={x}>{x}</option>)}</select>
   <select aria-label="Floors" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" onChange={e=>field('floors',e.target.value)}><option value="">Any floors</option><option>1</option><option>2</option></select>
   <select aria-label="Bathrooms" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" onChange={e=>field('bathrooms',e.target.value)}><option value="">Any bathrooms</option>{[1,2,3].map(x=><option key={x}>{x}</option>)}</select>
   <input aria-label="Style" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" placeholder="Style" onChange={e=>field('style',e.target.value)}/>
   <select aria-label="Terrain" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" onChange={e=>field('terrain',e.target.value)}><option value="">Any terrain</option>{['flat','urban','hillside','coastal'].map(x=><option key={x}>{x}</option>)}</select>
   <input aria-label="Maximum land perches" type="number" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" placeholder="Available land (perches)" onChange={e=>field('minimumLandSizePerches',e.target.value)}/>
   <input aria-label="Maximum built-up area" type="number" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" placeholder="Max built-up area (ft²)" onChange={e=>field('maximumBuiltUpArea',e.target.value)}/>
   <input aria-label="Category" className="p-2.5 rounded-xl bg-zinc-50 dark:bg-gray-950 border" placeholder="Category" onChange={e=>field('category',e.target.value)}/>
   <label className="flex items-center gap-2 px-2"><input type="checkbox" onChange={e=>setFilters(f=>({...f,parking:e.target.checked||undefined}))}/> Parking</label><label className="flex items-center gap-2 px-2"><input type="checkbox" onChange={e=>setFilters(f=>({...f,office:e.target.checked||undefined}))}/> Office</label><label className="flex items-center gap-2 px-2"><input type="checkbox" onChange={e=>setFilters(f=>({...f,balcony:e.target.checked||undefined}))}/> Balcony</label><label className="flex items-center gap-2 px-2"><input type="checkbox" onChange={e=>setFilters(f=>({...f,accessible:e.target.checked||undefined}))}/> Accessible</label>
  </div>
  {loading?<p>Loading plans…</p>:error?<p role="alert">{error}</p>:plans.length===0?<div className="p-12 text-center border border-dashed rounded-2xl">No plans match these filters.</div>:<div className="grid sm:grid-cols-2 xl:grid-cols-3 gap-5">{plans.map(p=><Link key={p.id} to={`/dashboard/plans/${p.id}`} className="group bg-white dark:bg-gray-900 border border-zinc-200 dark:border-gray-800 rounded-2xl overflow-hidden hover:border-indigo-400 hover:-translate-y-1 transition">
   <div className="h-36 bg-gradient-to-br from-indigo-100 via-white to-emerald-100 dark:from-indigo-950 dark:via-gray-900 dark:to-emerald-950 flex items-center justify-center"><span className="font-mono text-indigo-500">{p.designCode}</span></div>
   <div className="p-5"><div className="flex justify-between gap-2"><h2 className="font-bold text-lg">{p.name}</h2><span className="text-xs px-2 py-1 bg-zinc-100 dark:bg-gray-800 rounded-full h-fit capitalize">{p.category}</span></div><p className="text-sm text-zinc-500 capitalize mt-1">{p.style} · {p.suitableTerrain}</p><div className="grid grid-cols-4 gap-2 mt-5 text-xs text-zinc-600 dark:text-gray-300"><span><BedDouble size={16}/>{p.bedrooms}</span><span><Bath size={16}/>{p.bathrooms}</span><span><Layers3 size={16}/>{p.floorCount}</span><span><Ruler size={16}/>{Math.round(p.totalBuiltUpAreaSqft)} ft²</span></div></div>
  </Link>)}</div>}
 </main>
}
