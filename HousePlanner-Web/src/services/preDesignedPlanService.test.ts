import { beforeEach, describe, expect, it, vi } from 'vitest';
import apiClient from './apiClient';
import { preDesignedPlanService } from './preDesignedPlanService';

vi.mock('./apiClient',()=>({default:{get:vi.fn(),post:vi.fn(),put:vi.fn(),delete:vi.fn(),patch:vi.fn()}}));
describe('preDesignedPlanService',()=>{
 beforeEach(()=>vi.clearAllMocks());
 it('sends library filters without layout payload assumptions',async()=>{vi.mocked(apiClient.get).mockResolvedValue({data:[{id:'1'}]} as never);const result=await preDesignedPlanService.list({bedrooms:3,terrain:'flat'});expect(apiClient.get).toHaveBeenCalledWith('/pre-designed-plans',{params:{bedrooms:3,terrain:'flat'}});expect(result).toHaveLength(1)});
 it('uses the dedicated compatibility endpoint',async()=>{vi.mocked(apiClient.post).mockResolvedValue({data:{compatible:true,issues:[],warnings:[]}} as never);await preDesignedPlanService.compatibility('plan-1',{landSizePerches:10,terrain:'flat'});expect(apiClient.post).toHaveBeenCalledWith('/pre-designed-plans/plan-1/check-compatibility',{landSizePerches:10,terrain:'flat'})});
 it('uses admin status and soft-deactivation endpoints',async()=>{vi.mocked(apiClient.patch).mockResolvedValue({} as never);vi.mocked(apiClient.delete).mockResolvedValue({} as never);await preDesignedPlanService.setStatus('plan-1',false);await preDesignedPlanService.deactivate('plan-1');expect(apiClient.patch).toHaveBeenCalledWith('/admin/pre-designed-plans/plan-1/status',false);expect(apiClient.delete).toHaveBeenCalledWith('/admin/pre-designed-plans/plan-1')});
});
