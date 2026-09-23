/**
 * Matches the backend TerrainMultiplierData owned type.
 * Properties use camelCase as serialised by ASP.NET Core's default JSON policy.
 */
export interface TerrainMultiplier {
  flat: number;
  hillside: number;
  coastal: number;
}

/**
 * Matches the backend PricingData entity returned by GET /pricing.
 */
export interface PricingItem {
  id: number;
  itemName: string;
  category: string;
  unitCostLkr: number;
  unit: string;
  terrainMultiplier: TerrainMultiplier;
  displayGroup?: string | null;
  provider?: string | null;
  sourceReference?: string | null;
  updatedAt: string; // ISO-8601 DateTimeOffset
}

/**
 * Request body sent to POST /pricing.
 * Mirrors CreatePricingDto on the backend.
 */
export interface CreatePricingItemRequest {
  itemName: string;
  category: string;
  displayGroup?: string | null;
  unitCostLkr: number;
  terrainMultiplier: TerrainMultiplier;
  sourceReference?: string | null;
}

/**
 * Request body sent to PUT /pricing/{id}.
 * Mirrors UpdatePricingDto on the backend.
 */
export interface UpdatePricingItemRequest {
  unitCostLkr: number;
  terrainMultiplier: TerrainMultiplier;
}
