import apiClient from './apiClient';
import type { PricingItem, CreatePricingItemRequest, UpdatePricingItemRequest } from '../types/pricing.types';

/**
 * Pricing Service
 * Wraps the HousePlanner.API pricing endpoints.
 * Uses the shared apiClient (baseURL already includes /api/v1).
 */
const pricingService = {
  /**
   * GET /pricing
   * Fetches all pricing items from the database.
   */
  getAll: async (): Promise<PricingItem[]> => {
    const response = await apiClient.get<PricingItem[]>('/pricing');
    return response.data;
  },

  /**
   * POST /pricing
   * Creates a new pricing item (Constructor role required).
   */
  create: async (body: CreatePricingItemRequest): Promise<PricingItem> => {
    const response = await apiClient.post<PricingItem>('/pricing', body);
    return response.data;
  },

  /**
   * PUT /pricing/{id}
   * Updates a single pricing item's unit cost and terrain multipliers (Constructor role required).
   */
  update: async (id: number, body: UpdatePricingItemRequest): Promise<PricingItem> => {
    const response = await apiClient.put<PricingItem>(`/pricing/${id}`, body);
    return response.data;
  },
};

export { pricingService };
export default pricingService;
