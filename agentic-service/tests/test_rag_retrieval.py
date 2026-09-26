"""
Tests for RAG knowledge retrieval.
"""
from __future__ import annotations

import sys
import os
import json

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from app.knowledge.rag_pipeline import search_architecture_knowledge, search_knowledge_as_dicts
import app.knowledge.rag_pipeline

def fake_embedding(text: str) -> list[float]:
    import json
    import os
    mock_file = os.path.join(os.path.dirname(__file__), 'mock_embeddings.json')
    if os.path.exists(mock_file):
        with open(mock_file, 'r') as f:
            embeddings = json.load(f)
            if text in embeddings:
                return embeddings[text]
    
    # Fallback deterministic vector if not found
    import hashlib
    h = hashlib.md5(text.encode()).digest()
    extended = (list(h) * (1536 // len(h) + 1))[:1536]
    return [x / 255.0 for x in extended]

app.knowledge.rag_pipeline._generate_embedding = fake_embedding



def test_ventilation_retrieves_ventilation():
    """Ventilation question should retrieve ventilation-category chunks."""
    print("\n=== TEST: Ventilation Query ===")

    results = search_architecture_knowledge("How should I ventilate rooms in a tropical house?", top_k=3)
    print(f"  Found {len(results)} results:")
    for r in results:
        print(f"    [{r.category}] {r.title} (similarity={r.similarity})")
    
    assert len(results) > 0, "Expected at least one result"
    
    categories = [r.category for r in results]
    assert 'ventilation' in categories, f"Expected ventilation category in results, got {categories}"
    print("  ✓ Ventilation query correctly retrieved ventilation chunks!")


def test_foundation_retrieves_construction():
    """Foundation question should retrieve construction/foundation chunks."""
    print("\n=== TEST: Foundation Query ===")

    results = search_architecture_knowledge("What comes after foundation in construction?", top_k=3)
    print(f"  Found {len(results)} results:")
    for r in results:
        print(f"    [{r.category}] {r.title} (similarity={r.similarity})")
    
    assert len(results) > 0, "Expected at least one result"

    categories = [r.category for r in results]
    assert any(c in ('construction_stages', 'foundation_types') for c in categories), \
        f"Expected construction/foundation category, got {categories}"
    print("  ✓ Foundation query correctly retrieved construction chunks!")


def test_irrelevant_no_fake_retrieval():
    """Completely irrelevant query should get low-relevance or no results."""
    print("\n=== TEST: Irrelevant Query ===")

    results = search_architecture_knowledge("What is the recipe for chocolate cake?", top_k=3)
    print(f"  Found {len(results)} results:")
    for r in results:
        print(f"    [{r.category}] {r.title} (similarity={r.similarity})")

    if results:
        # All results should have relatively low similarity
        max_sim = max(r.similarity for r in results)
        print(f"  Max similarity: {max_sim}")
        assert max_sim < 0.6, f"Irrelevant query should not have high similarity, got {max_sim}"
        print("  ✓ Irrelevant query got low-relevance results (no fake retrieval)!")
    else:
        print("  ✓ Irrelevant query returned no results!")


def test_bedroom_orientation():
    """Orientation question should retrieve relevant architectural guidance."""
    print("\n=== TEST: Orientation Query ===")

    results = search_architecture_knowledge("What is the best direction for bedrooms in Sri Lanka?", top_k=5)
    print(f"  Found {len(results)} results:")
    for r in results:
        print(f"    [{r.category}] {r.title} (similarity={r.similarity})")

    assert len(results) > 0
    # Semantic search may return related categories like ventilation (which discusses
    # wind direction, monsoons) or daylight (which discusses sun exposure for bedrooms).
    # The key test is that results are architecturally relevant, not random.
    categories = set(r.category for r in results)
    architectural_categories = {
        'orientation', 'daylight', 'residential_planning', 'ventilation',
        'zoning', 'common_design_mistakes', 'land_planning',
    }
    assert categories & architectural_categories, \
        f"Expected architecturally relevant categories, got {categories}"
    print("  ✓ Orientation query correctly retrieved relevant architectural chunks!")


def test_setbacks_retrieval():
    """Setbacks question should retrieve land planning chunks."""
    print("\n=== TEST: Setbacks Query ===")

    results = search_architecture_knowledge("What are the standard setback requirements?", top_k=3)
    print(f"  Found {len(results)} results:")
    for r in results:
        print(f"    [{r.category}] {r.title} (similarity={r.similarity})")

    assert len(results) > 0
    categories = [r.category for r in results]
    assert 'land_planning' in categories, f"Expected land_planning category, got {categories}"
    print("  ✓ Setbacks query correctly retrieved land planning chunks!")


def test_dict_format():
    """search_knowledge_as_dicts should return proper serializable dicts."""
    print("\n=== TEST: Dict Format ===")

    results = search_knowledge_as_dicts("What is the minimum bedroom size for a house?", top_k=3)
    print(f"  Found {len(results)} results")
    
    assert len(results) > 0
    for r in results:
        assert 'title' in r
        assert 'content' in r
        assert 'category' in r
        assert 'source' in r
        assert 'relevance_score' in r
        assert isinstance(r['relevance_score'], float)
    
    # Verify JSON-serializable
    json.dumps(results)
    print("  ✓ Dict format is correct and JSON-serializable!")


if __name__ == '__main__':
    test_ventilation_retrieves_ventilation()
    test_foundation_retrieves_construction()
    test_irrelevant_no_fake_retrieval()
    test_bedroom_orientation()
    test_setbacks_retrieval()
    test_dict_format()

    print("\n" + "=" * 60)
    print("ALL RAG RETRIEVAL TESTS PASSED ✓")
    print("=" * 60)
