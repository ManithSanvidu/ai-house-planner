import apiClient from './apiClient';
import type { FloorPlanData } from '../components/floorplan/FloorPlanViewer';

export interface PreDesignedPlanSummary {
  id:string; name:string; slug:string; designCode:string; style:string; bedrooms:number; bathrooms:number;
  floorCount:number; totalBuiltUpAreaSqft:number; minimumLandSizePerches:number; suitableTerrain:string;
  parkingSpaces:number; hasBalcony:boolean; hasVeranda:boolean; hasOffice:boolean;
  isAccessibleFriendly:boolean; category?:string; tags:string[]; thumbnailUrl?:string; isActive:boolean;
}
export interface PreDesignedPlanDetail extends PreDesignedPlanSummary {
  description?:string; minimumPlotWidthFt?:number; minimumPlotLengthFt?:number; hasUtilityRoom:boolean;
  layout:FloorPlanData; conceptualDisclaimer:string;
}
export interface PlanFilters { bedrooms?:number; bathrooms?:number; floors?:number; style?:string; terrain?:string; minimumLandSizePerches?:number; maximumBuiltUpArea?:number; parking?:boolean; office?:boolean; balcony?:boolean; accessible?:boolean; category?:string; search?:string }
export interface PlanCompatibility { compatible:boolean; issues:string[]; warnings:string[] }
export type SavePlan = Omit<PreDesignedPlanDetail,'id'|'conceptualDisclaimer'>;

export const preDesignedPlanService = {
  list: async (filters:PlanFilters={}) => (await apiClient.get<PreDesignedPlanSummary[]>('/pre-designed-plans',{params:filters})).data,
  detail: async (id:string) => (await apiClient.get<PreDesignedPlanDetail>(`/pre-designed-plans/${id}`)).data,
  compatibility: async (id:string, request:object) => (await apiClient.post<PlanCompatibility>(`/pre-designed-plans/${id}/check-compatibility`,request)).data,
  adminList: async (status='all') => (await apiClient.get<PreDesignedPlanSummary[]>('/admin/pre-designed-plans',{params:{status}})).data,
  adminDetail: async (id:string) => (await apiClient.get<PreDesignedPlanDetail>(`/admin/pre-designed-plans/${id}`)).data,
  create: async (plan:SavePlan) => (await apiClient.post<PreDesignedPlanDetail>('/admin/pre-designed-plans',plan)).data,
  update: async (id:string, plan:SavePlan) => (await apiClient.put<PreDesignedPlanDetail>(`/admin/pre-designed-plans/${id}`,plan)).data,
  deactivate: async (id:string) => apiClient.delete(`/admin/pre-designed-plans/${id}`),
  setStatus: async (id:string,isActive:boolean) => apiClient.patch(`/admin/pre-designed-plans/${id}/status`,isActive),
};
