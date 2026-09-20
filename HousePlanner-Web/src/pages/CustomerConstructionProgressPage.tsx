import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, RefreshCw } from 'lucide-react';
import { customerConstructionService } from '../services/customerConstructionService';

export default function CustomerConstructionProgressPage() {
 const { projectId } = useParams(); const [data,setData]=useState<any>(null); const [error,setError]=useState('');
 const load=useCallback(async()=>{if(!projectId)return;try{setData(await customerConstructionService.project(projectId));setError('');}catch{setError('Project not found or unavailable.');}},[projectId]);
 useEffect(()=>{void load();},[load]);
 const days=useMemo(()=>{const result=[];const end=new Date();const start=new Date(end);start.setDate(end.getDate()-83);for(let d=new Date(start);d<=end;d.setDate(d.getDate()+1))result.push(new Date(d));return result;},[]);
 const activity=new Map<string,any>((data?.activity||[]).map((x:any)=>[String(x.date),x]));
 if(error)return <div role="alert" className="p-8">{error}</div>; if(!data)return <div className="p-8">Loading construction progress…</div>;
 return <main className="mx-auto max-w-6xl p-4 sm:p-8 space-y-7"><Link to="/dashboard/construction" className="inline-flex gap-2"><ArrowLeft size={18}/>Construction</Link>
 <header className="flex justify-between"><div><h1 className="text-3xl font-bold">Construction Progress</h1><p>Approved design v{data.project.designVersion} · {data.project.constructorName}</p></div><button onClick={()=>void load()} aria-label="Refresh progress" className="p-3 rounded-xl border"><RefreshCw size={18}/></button></header>
 <section className="grid sm:grid-cols-3 gap-3"><Card label="Status" value={data.project.status}/><Card label="Current Phase" value={data.project.currentPhase||'Awaiting update'}/><Card label="Overall Progress" value={`${data.progress.overallProgress}%`}/></section>
 <section className="rounded-2xl border p-5"><h2 className="text-xl font-bold mb-4">Project Progress</h2><div className="space-y-3">{data.phases.map((p:any)=><div key={p.id} className="flex gap-3"><span aria-hidden>{p.status==='completed'?'✓':p.status==='in_progress'?'●':'○'}</span><span>{p.phaseName}</span><span className="ml-auto text-sm text-zinc-500">{p.status.replace('_',' ')}</span></div>)}</div></section>
 <section className="rounded-2xl border p-5 overflow-x-auto"><h2 className="text-xl font-bold">Construction Activity</h2><p className="text-sm text-zinc-500 mb-4">Color intensity shows the number of recorded updates, not construction quality.</p><div className="grid grid-rows-7 grid-flow-col gap-1 w-max">{days.map(d=>{const key=d.toISOString().slice(0,10);const a=activity.get(key);const n=a?.count||0;const color=n===0?'bg-zinc-100 dark:bg-gray-800':n===1?'bg-emerald-300':n===2?'bg-emerald-500':'bg-emerald-700';return <div key={key} title={`${d.toLocaleDateString()}: ${n} construction update${n===1?'':'s'}`} aria-label={`${d.toLocaleDateString()}: ${n} construction updates`} className={`h-3.5 w-3.5 rounded-sm ${color}`}/>})}</div></section>
 <section><h2 className="text-xl font-bold mb-4">Construction Updates</h2>{!data.logs.length?<p className="text-zinc-500">No construction updates yet.</p>:<div className="space-y-3">{data.logs.map((l:any)=><article key={l.id} className="rounded-2xl border p-5"><time className="text-sm text-zinc-500">{new Date(l.date).toLocaleDateString()}</time><h3 className="font-bold">{l.phase||'Project update'}</h3><p className="mt-2">{l.completedWork}</p><p className="text-sm text-indigo-600 mt-2">Progress recorded: {l.progressPercentage}%</p>{l.issues&&<p className="text-sm text-amber-700">Issue: {l.issues}</p>}</article>)}</div>}</section></main>;
}
const Card=({label,value}:{label:string,value:string})=><div className="rounded-2xl border p-5"><p className="text-sm text-zinc-500">{label}</p><p className="text-xl font-bold capitalize">{value}</p></div>;

