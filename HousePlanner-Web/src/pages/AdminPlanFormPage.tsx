import { useEffect, useState, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { preDesignedPlanService, type SavePlan } from '../services/preDesignedPlanService';
import { Plus, Trash } from 'lucide-react';

import { getImageUrl } from '../utils/imageUtils';

const blank: any = {
  name: '', slug: '', designCode: '', description: '', style: 'modern',
  bedrooms: 2, bathrooms: 1, floorCount: 1, totalBuiltUpAreaSqft: 1088,
  minimumLandSizePerches: 8, suitableTerrain: 'flat', parkingSpaces: 0,
  category: 'Starter', tags: [], isActive: true
};

const ROOM_TYPES = [
  { value: 'living_room', label: 'Living Room' },
  { value: 'kitchen', label: 'Kitchen' },
  { value: 'bedroom', label: 'Bedroom' },
  { value: 'bathroom', label: 'Bathroom' },
  { value: 'dining', label: 'Dining' },
  { value: 'staircase', label: 'Staircase' },
  { value: 'balcony', label: 'Balcony' },
  { value: 'veranda', label: 'Veranda' },
  { value: 'office', label: 'Office' },
  { value: 'utility', label: 'Utility Room' }
];

type Room = { id: string; room_type: string; name: string; floor: number; width: number; length: number; isAuto?: boolean; isCustomized?: boolean; };

export default function AdminPlanFormPage() {
  const { id } = useParams();
  const nav = useNavigate();

  const [form, setForm] = useState<any>(blank);
  const [rooms, setRooms] = useState<Room[]>([]);
  const [error, setError] = useState<string[]>([]);



  // Auto-init on new plan
  useEffect(() => {
    if (!id && rooms.length === 0) {
      setRooms([
        { id: crypto.randomUUID(), room_type: 'living_room', name: 'Living Room', floor: 1, width: 14, length: 12, isAuto: true },
        { id: crypto.randomUUID(), room_type: 'kitchen', name: 'Kitchen', floor: 1, width: 10, length: 10, isAuto: true },
        { id: crypto.randomUUID(), room_type: 'bathroom', name: 'Bathroom 1', floor: 1, width: 6, length: 8, isAuto: true },
        { id: crypto.randomUUID(), room_type: 'bedroom', name: 'Bedroom 1', floor: 1, width: 12, length: 12, isAuto: true },
        { id: crypto.randomUUID(), room_type: 'bedroom', name: 'Bedroom 2', floor: 1, width: 12, length: 12, isAuto: true }
      ]);
    }
  }, [id, rooms.length]);

  // Sync bedrooms count
  useEffect(() => {
    if (rooms.length === 0) return;
    const beds = rooms.filter(r => r.room_type === 'bedroom');
    if (form.bedrooms > beds.length) {
      const toAdd = form.bedrooms - beds.length;
      const newRooms = Array.from({length: toAdd}).map((_, i) => ({
        id: crypto.randomUUID(), room_type: 'bedroom', name: `Bedroom ${beds.length + i + 1}`, floor: 1, width: 12, length: 12, isAuto: true
      }));
      setRooms(prev => [...prev, ...newRooms]);
    } else if (form.bedrooms < beds.length) {
      const diff = beds.length - form.bedrooms;
      let safeToRemove = 0;
      const toRemoveIds: string[] = [];
      for (let i = beds.length - 1; i >= 0; i--) {
        if (beds[i].isAuto && !beds[i].isCustomized) {
          toRemoveIds.push(beds[i].id);
          safeToRemove++;
          if (safeToRemove === diff) break;
        }
      }
      if (safeToRemove === diff) {
        setRooms(prev => prev.filter(r => !toRemoveIds.includes(r.id)));
      }
    }
  }, [form.bedrooms]);

  // Sync bathrooms count
  useEffect(() => {
    if (rooms.length === 0) return;
    const baths = rooms.filter(r => r.room_type === 'bathroom');
    if (form.bathrooms > baths.length) {
      const toAdd = form.bathrooms - baths.length;
      const newRooms = Array.from({length: toAdd}).map((_, i) => ({
        id: crypto.randomUUID(), room_type: 'bathroom', name: `Bathroom ${baths.length + i + 1}`, floor: 1, width: 6, length: 8, isAuto: true
      }));
      setRooms(prev => [...prev, ...newRooms]);
    } else if (form.bathrooms < baths.length) {
      const diff = baths.length - form.bathrooms;
      let safeToRemove = 0;
      const toRemoveIds: string[] = [];
      for (let i = baths.length - 1; i >= 0; i--) {
        if (baths[i].isAuto && !baths[i].isCustomized) {
          toRemoveIds.push(baths[i].id);
          safeToRemove++;
          if (safeToRemove === diff) break;
        }
      }
      if (safeToRemove === diff) {
        setRooms(prev => prev.filter(r => !toRemoveIds.includes(r.id)));
      }
    }
  }, [form.bathrooms]);

  useEffect(() => {
    if (id) {
      preDesignedPlanService.adminDetail(id).then(p => {
        setForm(p);
        if (p.layout?.rooms) {
          setRooms(p.layout.rooms.filter((r:any) => r.room_type !== 'hallway').map((r: any) => ({
            id: crypto.randomUUID(),
            room_type: r.room_type,
            name: r.name || r.room_type,
            floor: r.floor,
            width: r.width,
            length: r.length
          })));
        }
      }).catch(() => setError(['Plan could not be loaded.']));
    }
  }, [id]);

  const change = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setForm((x: any) => ({ ...x, [name]: e.target instanceof HTMLInputElement && e.target.type === 'number' ? Number(value) : value }));
  };

  const updateRoom = (id: string, field: string, value: any) => {
    setRooms(rooms.map(r => r.id === id ? { ...r, [field]: value, isCustomized: true } : r));
  };

  const addRoom = () => {
    setRooms([...rooms, { id: crypto.randomUUID(), room_type: 'bedroom', name: 'New Room', floor: 1, width: 10, length: 10 }]);
  };

  const removeRoom = (id: string) => {
    setRooms(rooms.filter(r => r.id !== id));
  };

  const toggleFeature = (type: string, name: string, defaultW: number, defaultL: number) => {
    const exists = rooms.some(r => r.room_type === type);
    if (exists) setRooms(rooms.filter(r => r.room_type !== type));
    else setRooms([...rooms, { id: crypto.randomUUID(), room_type: type, name, floor: 1, width: defaultW, length: defaultL, isAuto: true }]);
  };

  const validate = useCallback(() => {
    const errs: string[] = [];
    const bedCount = rooms.filter(r => r.room_type === 'bedroom').length;
    const bathCount = rooms.filter(r => r.room_type === 'bathroom').length;

    if (form.bedrooms !== bedCount) errs.push(`Bedroom count is ${form.bedrooms} but ${bedCount} bedroom rooms exist. Please adjust rooms to match.`);
    if (form.bathrooms !== bathCount) errs.push(`Bathroom count is ${form.bathrooms} but ${bathCount} bathroom rooms exist. Please adjust rooms to match.`);

    const maxFloor = rooms.length ? Math.max(...rooms.map(r => r.floor)) : 1;
    if (form.floorCount < maxFloor) errs.push(`Floor count (${form.floorCount}) is lower than maximum room floor (${maxFloor}).`);

    if (!rooms.some(r => r.room_type === 'living_room')) errs.push('A living room must exist.');
    if (!rooms.some(r => r.room_type === 'kitchen')) errs.push('A kitchen must exist.');
    if (!rooms.some(r => r.room_type === 'bathroom')) errs.push('A bathroom must exist.');
    if (rooms.some(r => r.width < 2.5 || r.length < 2.5)) errs.push('Rooms must have at least 2.5ft width and length.');

    return errs;
  }, [form.bedrooms, form.bathrooms, form.floorCount, rooms]);

  const generateLayoutJson = useCallback(() => {
    let layoutRooms: any[] = [];
    let connections: any[] = [];
    const floors = Array.from(new Set(rooms.map(r => r.floor))).sort((a, b) => a - b);

    floors.forEach(floor => {
      const fRooms = rooms.filter(r => r.floor === floor).sort((a, b) => {
        const order: any = { 'living_room': 1, 'kitchen': 2, 'bathroom': 3, 'bedroom': 4 };
        const getOrder = (type: string) => (Object.entries(order).find(([k]) => type.includes(k))?.[1] as number) || 5;
        return getOrder(a.room_type) - getOrder(b.room_type);
      });

      let top: any[] = [], bot: any[] = [];
      let xTop = 0, xBot = 0;
      fRooms.forEach(r => {
        if (xTop <= xBot) { top.push(r); xTop += Number(r.width); }
        else { bot.push(r); xBot += Number(r.width); }
      });

      const maxLBot = bot.length > 0 ? Math.max(...bot.map(r => Number(r.length))) : 0;
      const hallwayWidth = Math.max(xTop, xBot, 3);
      const hallwayId = `f${floor}_corridor`;

      layoutRooms.push({ x: 0, y: maxLBot, floor, width: hallwayWidth, length: 3, room_id: hallwayId, room_type: 'corridor', name: 'Corridor' });

      let currX = 0;
      top.forEach((r, i) => {
        const room_id = `f${floor}_t${i}`;
        layoutRooms.push({ x: currX, y: maxLBot + 3, floor, width: Number(r.width), length: Number(r.length), room_id, room_type: r.room_type, name: r.name });
        connections.push({ from_room: hallwayId, to_room: room_id, kind: 'door' });
        currX += Number(r.width);
      });

      currX = 0;
      bot.forEach((r, i) => {
        const room_id = `f${floor}_b${i}`;
        layoutRooms.push({ x: currX, y: maxLBot - Number(r.length), floor, width: Number(r.width), length: Number(r.length), room_id, room_type: r.room_type, name: r.name });
        connections.push({ from_room: hallwayId, to_room: room_id, kind: 'door' });
        currX += Number(r.width);
      });
    });

    const stairs = layoutRooms.filter(r => r.room_type === 'staircase');
    for (let i = 1; i < stairs.length; i++) {
      if (stairs[i].floor > stairs[i-1].floor) {
         connections.push({ from_room: stairs[i-1].room_id, to_room: stairs[i].room_id, kind: 'stair' });
      }
    }

    const f1Rooms = layoutRooms.filter(r => r.floor === 1);
    const entranceRoom = f1Rooms.find(r => r.room_type === 'living_room') || f1Rooms.find(r => r.room_type === 'hallway') || f1Rooms[0];
    const entrances = entranceRoom ? [{ room_id: entranceRoom.room_id, wall: 'south', width: 3, offset: entranceRoom.width / 2 }] : [];

    return {
      design_id: form.layout?.design_id || crypto.randomUUID(),
      floor_count: form.floorCount,
      total_built_up_area_sqft: form.totalBuiltUpAreaSqft,
      rooms: layoutRooms,
      entrances,
      connections
    };
  }, [form.floorCount, form.totalBuiltUpAreaSqft, form.layout?.design_id, rooms]);



  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs = validate();
    if (errs.length > 0) return setError(errs);

    try {
      const layout = generateLayoutJson();
      const payload = {
        ...form,
        hasBalcony: rooms.some(r => r.room_type === 'balcony'),
        hasVeranda: rooms.some(r => r.room_type === 'veranda'),
        hasOffice: rooms.some(r => r.room_type === 'office'),
        hasUtilityRoom: rooms.some(r => r.room_type === 'utility'),
        layout,
        tags: typeof form.tags === 'string' ? form.tags.split(',').map((x: string) => x.trim()).filter(Boolean) : form.tags
      } as SavePlan;

      let savedPlan;
      if (id) {
        savedPlan = await preDesignedPlanService.update(id, payload);
      } else {
        savedPlan = await preDesignedPlanService.create(payload);
      }

      if (selectedFile) {
        await preDesignedPlanService.uploadImage(savedPlan.id, selectedFile);
      }

      nav('/dashboard/admin/plans');
    } catch (ex: any) {
      setError([ex.response?.data?.errors?.join(' ') || 'The plan could not be saved.']);
    }
  };

  return (
    <main className="p-6 md:p-10 max-w-7xl mx-auto text-zinc-900 dark:text-text-primary">
      <h1 className="text-3xl font-bold mb-6">{id ? 'Edit' : 'Add'} pre-designed plan</h1>

      {error.length > 0 && (
        <div role="alert" className="bg-red-50 text-red-700 p-4 rounded-xl mb-6">
          <ul className="list-disc ml-5">
            {error.map((e, i) => <li key={i}>{e}</li>)}
          </ul>
        </div>
      )}

      <form onSubmit={save} className="grid lg:grid-cols-2 gap-8">
        <section className="space-y-6">
          <div className="bg-surface border rounded-2xl p-5">
            <h2 className="text-lg font-bold mb-1">Design Image</h2>
            <p className="text-xs text-text-muted mb-4">Upload a floor plan or house design image</p>
            <div className="flex items-center gap-4">
              <input type="file" accept="image/jpeg,image/png,image/webp" onChange={e => setSelectedFile(e.target.files?.[0] || null)} className="block w-full text-sm text-text-muted file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-indigo-50 file:text-indigo-700 hover:file:bg-indigo-100" />
              {(form.thumbnailUrl || selectedFile) && (
                <button type="button" onClick={async () => {
                  if (id && !selectedFile && form.thumbnailUrl) {
                    await preDesignedPlanService.removeImage(id);
                    setForm({ ...form, thumbnailUrl: null });
                  } else {
                    setSelectedFile(null);
                    (document.querySelector('input[type="file"]') as HTMLInputElement).value = '';
                  }
                }} className="text-xs text-red-600 hover:text-red-700 font-medium px-3 py-1.5 border border-red-200 rounded-lg whitespace-nowrap">
                  Remove Image
                </button>
              )}
            </div>
            {(form.thumbnailUrl || selectedFile) && (
              <div className="mt-4">
                <img src={selectedFile ? URL.createObjectURL(selectedFile) : getImageUrl(form.thumbnailUrl)} alt="Preview" className="max-h-40 rounded border object-contain bg-background" />
              </div>
            )}
          </div>

          <div className="bg-surface border rounded-2xl p-5">
            <h2 className="text-lg font-bold mb-4">Plan Details</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              {[
                ['name', 'Name', 'text'], ['slug', 'Slug', 'text'], ['designCode', 'Design code', 'text'],
                ['style', 'Style', 'text'], ['bedrooms', 'Bedrooms', 'number'], ['bathrooms', 'Bathrooms', 'number'],
                ['floorCount', 'Floors', 'number'], ['totalBuiltUpAreaSqft', 'Built-up area (ft²)', 'number']
              ].map(([name, label, type]) => (
                <label key={name} className="text-sm font-medium">
                  {label}
                  <input required name={name} type={type} min={type === 'number' ? 1 : undefined} value={form[name] ?? ''} onChange={change} className="block w-full mt-1.5 p-2.5 rounded-lg bg-surface-elevated bg-background border" />
                </label>
              ))}
              <label className="text-sm font-medium">
                Terrain
                <select name="suitableTerrain" value={form.suitableTerrain} onChange={change} className="block w-full mt-1.5 p-2.5 rounded-lg bg-surface-elevated bg-background border">
                  {['flat', 'urban', 'hillside', 'coastal', 'all'].map(x => <option key={x}>{x}</option>)}
                </select>
              </label>
              <label className="text-sm font-medium">
                Category
                <input required name="category" type="text" value={form.category ?? ''} onChange={change} className="block w-full mt-1.5 p-2.5 rounded-lg bg-surface-elevated bg-background border" />
              </label>
              <label className="text-sm font-medium sm:col-span-2">
                Estimated Construction Cost (LKR)
                <input name="estimatedConstructionCost" type="number" min="0" step="0.01" value={form.estimatedConstructionCost ?? ''} onChange={change} className="block w-full mt-1.5 p-2.5 rounded-lg bg-surface-elevated bg-background border" />
                <span className="text-xs text-text-muted mt-1 block">Approximate construction cost in LKR</span>
              </label>
              <label className="sm:col-span-2 text-sm font-medium">
                Description
                <textarea name="description" value={form.description ?? ''} onChange={change} className="block w-full mt-1.5 p-2.5 rounded-lg bg-surface-elevated bg-background border" rows={3} />
              </label>

              <div className="sm:col-span-2 mt-2">
                <h3 className="text-sm font-bold mb-2">Features</h3>
                <div className="flex flex-wrap gap-4">
                  {[
                    ['balcony', 'Balcony', 8, 5], ['veranda', 'Veranda', 12, 6],
                    ['office', 'Office', 10, 8], ['utility', 'Utility', 6, 8]
                  ].map(([type, label, w, l]) => (
                    <label key={type as string} className="flex items-center gap-2 cursor-pointer">
                      <input type="checkbox" checked={rooms.some(r => r.room_type === type)} onChange={() => toggleFeature(type as string, label as string, w as number, l as number)} className="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-600" />
                      <span className="text-sm">{label}</span>
                    </label>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className="space-y-6">
          <div>
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-bold">Rooms & Layout</h2>
            </div>

            <div className="space-y-4">
              {rooms.map(r => (
                <div key={r.id} className="bg-surface-elevated bg-background p-3 rounded-xl border">
                  <div className="flex justify-between items-center mb-2">
                    <select value={r.room_type} onChange={(e) => updateRoom(r.id, 'room_type', e.target.value)} className="font-semibold bg-transparent border-b border-dashed border-gray-400 focus:outline-none focus:border-indigo-500 pb-0.5">
                      {ROOM_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                    </select>
                    <button type="button" onClick={() => removeRoom(r.id)} className="text-red-500 hover:bg-red-50 p-1.5 rounded-lg text-xs font-medium flex items-center gap-1"><Trash size={14} /> Remove</button>
                  </div>
                  <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600 dark:text-gray-300">
                    <label className="flex items-center gap-2">Name <input type="text" value={r.name} onChange={(e) => updateRoom(r.id, 'name', e.target.value)} className="w-24 p-1 rounded border bg-white dark:bg-gray-800" /></label>
                    <label className="flex items-center gap-2">Floor <input type="number" min={1} value={r.floor} onChange={(e) => updateRoom(r.id, 'floor', Number(e.target.value))} className="w-16 p-1 rounded border bg-white dark:bg-gray-800 text-center" /></label>
                    <label className="flex items-center gap-2">Width <input type="number" min={3} value={r.width} onChange={(e) => updateRoom(r.id, 'width', Number(e.target.value))} className="w-16 p-1 rounded border bg-white dark:bg-gray-800 text-center" /> ft</label>
                    <label className="flex items-center gap-2">Length <input type="number" min={3} value={r.length} onChange={(e) => updateRoom(r.id, 'length', Number(e.target.value))} className="w-16 p-1 rounded border bg-white dark:bg-gray-800 text-center" /> ft</label>
                  </div>
                </div>
              ))}
            </div>

            <button type="button" onClick={addRoom} className="mt-4 flex w-full justify-center items-center gap-2 text-sm bg-indigo-50 text-indigo-700 px-4 py-3 rounded-xl font-semibold hover:bg-indigo-100 border border-indigo-100 border-dashed transition-colors">
              <Plus size={18} /> Add Room
            </button>
          </div>
        </section>

        <div className="lg:col-span-2 flex justify-end gap-3 mt-4">
          <button type="button" onClick={() => nav('/dashboard/admin/plans')} className="border px-6 py-2.5 rounded-xl font-medium">Cancel</button>
          <button type="submit" className="bg-indigo-600 text-white px-6 py-2.5 rounded-xl font-medium shadow-sm hover:bg-indigo-700">Save Plan</button>
        </div>
      </form>
    </main>
  );
}
