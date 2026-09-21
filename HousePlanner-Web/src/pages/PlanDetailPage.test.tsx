import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { beforeEach, expect, test, vi } from 'vitest';
import PlanDetailPage from './PlanDetailPage';
import { preDesignedPlanService } from '../services/preDesignedPlanService';

vi.mock('../components/floorplan/FloorPlanViewer',()=>({FloorPlanViewer:()=> <div>Floor plan</div>}));
vi.mock('../services/preDesignedPlanService',()=>({preDesignedPlanService:{detail:vi.fn(),currentProjectCompatibility:vi.fn()}}));
const plan={id:'11111111-1111-1111-1111-111111111111',name:'Plan',slug:'plan',designCode:'HP-2',style:'modern',bedrooms:2,bathrooms:1,floorCount:1,totalBuiltUpAreaSqft:600,minimumLandSizePerches:8,minimumPlotWidthFt:30,minimumPlotLengthFt:40,suitableTerrain:'flat',parkingSpaces:0,hasBalcony:false,hasVeranda:false,hasOffice:false,hasUtilityRoom:false,isAccessibleFriendly:false,tags:[],isActive:true,layout:{floor_count:1,rooms:[],connections:[],entrances:[]},conceptualDisclaimer:'Concept only'};
const Location=()=>{const location=useLocation();return <p>Location: {location.pathname}{location.search}</p>};
const renderPage=()=>render(<MemoryRouter initialEntries={[`/dashboard/plans/${plan.id}`]}><Routes><Route path="/dashboard/plans/:id" element={<PlanDetailPage/>}/><Route path="/dashboard/new-project" element={<Location/>}/></Routes></MemoryRouter>);

beforeEach(()=>{vi.clearAllMocks();vi.mocked(preDesignedPlanService.detail).mockResolvedValue(plan as any);vi.mocked(preDesignedPlanService.currentProjectCompatibility).mockResolvedValue({compatible:true,issues:[],warnings:[],landSizePerches:10,minimumLandSizePerches:8})});

test('uses owned backend project data for compatibility and exposes no dummy adapt action',async()=>{
 renderPage(); await screen.findByRole('button',{name:'Check Compatibility'});
 expect(screen.queryByRole('button',{name:'Adapt This Plan'})).toBeNull();
 fireEvent.click(screen.getByRole('button',{name:'Check Compatibility'}));
 expect(await screen.findByText('✓ Compatible with your project')).toBeTruthy();
 expect(preDesignedPlanService.currentProjectCompatibility).toHaveBeenCalledWith(plan.id);
});

test('missing project details offers the existing intake flow without fake compatibility',async()=>{
 vi.mocked(preDesignedPlanService.currentProjectCompatibility).mockRejectedValue({response:{status:404,data:{code:'project_details_required'}}});
 renderPage(); await screen.findByRole('button',{name:'Check Compatibility'});
 fireEvent.click(screen.getByRole('button',{name:'Check Compatibility'}));
 expect(await screen.findByText('Add your project details first')).toBeTruthy();
 expect(screen.queryByText('✓ Compatible with your project')).toBeNull();
 fireEvent.click(screen.getByRole('button',{name:'Enter Project Details'}));
 expect(await screen.findByText(`Location: /dashboard/new-project?basePlanId=${plan.id}&mode=use`)).toBeTruthy();
});

test('Use This Plan preserves the catalogue ID and mode in the existing intake route',async()=>{
 renderPage(); const button=await screen.findByRole('button',{name:'Use This Plan'});fireEvent.click(button);
 expect(await screen.findByText(`Location: /dashboard/new-project?basePlanId=${plan.id}&mode=use`)).toBeTruthy();
});

test('checking state prevents duplicate compatibility submissions',async()=>{
 let resolve!:(value:any)=>void;vi.mocked(preDesignedPlanService.currentProjectCompatibility).mockReturnValue(new Promise(r=>{resolve=r}));
 renderPage(); const button=await screen.findByRole('button',{name:'Check Compatibility'});fireEvent.click(button);fireEvent.click(button);
 expect(await screen.findByRole('button',{name:'Checking…'})).toBeTruthy();expect(preDesignedPlanService.currentProjectCompatibility).toHaveBeenCalledTimes(1);
 resolve({compatible:true,issues:[],warnings:[]});await waitFor(()=>expect(screen.getByText('✓ Compatible with your project')).toBeTruthy());
});

test('compatibility API failure is customer friendly',async()=>{
 vi.mocked(preDesignedPlanService.currentProjectCompatibility).mockRejectedValue(new Error('network'));
 renderPage();fireEvent.click(await screen.findByRole('button',{name:'Check Compatibility'}));
 expect(await screen.findByRole('alert')).toHaveProperty('textContent','Compatibility could not be checked. Please try again.');
});
