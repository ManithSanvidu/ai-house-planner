import pytest
from app.design.catalogue.base_plan_library import load_base_plan_catalog, _load_seed_records

def test_load_base_plan_catalog_is_strictly_original():
    """
    Ensure the catalog only loads original, immutable designs
    without appending synthetic rotated/mirrored variants.
    """
    originals = _load_seed_records()
    # load_base_plan_catalog is lru_cached, we can clear cache to be safe
    load_base_plan_catalog.cache_clear()
    catalog = load_base_plan_catalog()

    # Must return exact same count as the JSON source
    assert len(catalog) == len(originals), f"Expected {len(originals)} plans, but got {len(catalog)}. Synthetic padding is still active!"

    # Ensure no synthetic identifiers exist in the catalog
    for plan in catalog:
        assert '-R90' not in plan.plan_code
        assert '-R180' not in plan.plan_code
        assert '-R270' not in plan.plan_code
        assert '-MH' not in plan.plan_code
        assert '-MV' not in plan.plan_code
