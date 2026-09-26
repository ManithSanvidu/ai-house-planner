import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { preDesignedPlanService, type PreDesignedPlanSummary } from '../services/preDesignedPlanService';

export default function AdminPlansPage(){
 const [plans,setPlans]=useState<PreDesignedPlanSummary[]>([]),[status,setStatus]=useState('all'),[error,setError]=useState('');
 const load=()=>preDesignedPlanService.adminList(status).then(setPlans).catch(()=>setError('Could not load plans.'));
 useEffect(()=>{preDesignedPlanService.adminList(status).then(setPlans).catch(()=>setError('Could not load plans.'));},[status]);
 const toggle=async(p:PreDesignedPlanSummary)=>{if(!p.isActive&&!confirm(`Reactivate ${p.name}?`))return;if(p.isActive&&!confirm(`Deactivate ${p.name}? Existing designs will remain available.`))return; await preDesignedPlanService.setStatus(p.id,!p.isActive); load();};
 return <main className="p-6 md:p-10 max-w-7xl mx-auto text-zinc-900 dark:text-text-primary"><div className="flex justify-between items-center mb-7"><div><p className="text-indigo-500 font-semibold text-sm">ADMIN</p><h1 className="text-3xl font-bold">Plan catalogue</h1></div><Link to="/dashboard/admin/plans/new" className="bg-indigo-600 text-text-primary px-5 py-3 rounded-xl">Add plan</Link></div>
 <select value={status} onChange={e=>setStatus(e.target.value)} className="mb-5 p-2 rounded-lg bg-surface border"><option value="all">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select>{error&&<p role="alert">{error}</p>}
 <div className="overflow-auto border rounded-2xl"><table className="w-full bg-surface text-sm"><thead><tr className="text-left border-b"><th className="p-4">Code</th><th>Name</th><th>Configuration</th><th>Status</th><th className="text-right p-4">Actions</th></tr></thead><tbody>{plans.map(p=><tr key={p.id} className="border-b"><td className="p-4 font-mono">{p.designCode}</td><td>{p.name}</td><td>{p.bedrooms} bed · {p.bathrooms} bath · {p.floorCount} floor</td><td><span className={p.isActive?'text-emerald-600':'text-text-secondary'}>{p.isActive?'Active':'Inactive'}</span></td><td className="p-4 text-right"><Link className="text-indigo-500 mr-4" to={`/dashboard/admin/plans/${p.id}/edit`}>Edit</Link><button onClick={()=>toggle(p)} className="text-amber-600">{p.isActive?'Deactivate':'Reactivate'}</button></td></tr>)}</tbody></table></div></main>
}
