"""
RAG pipeline for architecture knowledge retrieval.

Pipeline:
1. Document loader + chunker
2. OpenAI embedding generation  
3. pgvector storage in Supabase PostgreSQL
4. Semantic retrieval with top-k context selection
"""
from __future__ import annotations

import hashlib
import logging
import os
import re
from dataclasses import dataclass
from typing import Any

import psycopg2
import psycopg2.extras
import requests

from app.config import OPENAI_API_KEY

logger = logging.getLogger(__name__)

# ── Config ──────────────────────────────────────────────────────────────────

EMBEDDING_MODEL = "text-embedding-3-small"
EMBEDDING_DIM = 1536
CHUNK_MAX_CHARS = 800
CHUNK_OVERLAP_CHARS = 100
TOP_K_DEFAULT = 3
SIMILARITY_THRESHOLD = 0.3  # Minimum cosine similarity to include


def _get_db_connection_string() -> str:
    """Build a psycopg2-compatible DSN from the .env DATABASE_CONNECTION_STRING."""
    # Try agentic-service .env first, then fallback to HousePlanner.API .env
    for env_path in [
        os.path.join(os.path.dirname(__file__), '..', '..', '.env'),
        os.path.join(os.path.dirname(__file__), '..', '..', '..', 'HousePlanner.API', '.env'),
    ]:
        if not os.path.exists(env_path):
            continue
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line.startswith('DATABASE_CONNECTION_STRING='):
                    raw = line.split('=', 1)[1].strip().strip('"\'')
                    return _dotnet_to_psycopg2(raw)

    # Fallback to env var
    raw = os.getenv('DATABASE_CONNECTION_STRING', '')
    if raw:
        return _dotnet_to_psycopg2(raw)

    raise RuntimeError("DATABASE_CONNECTION_STRING not found in .env or environment.")


def _dotnet_to_psycopg2(dotnet_str: str) -> str:
    """Convert .NET-style connection string to psycopg2 DSN."""
    parts: dict[str, str] = {}
    for segment in dotnet_str.split(';'):
        segment = segment.strip()
        if '=' in segment:
            k, v = segment.split('=', 1)
            parts[k.strip().lower()] = v.strip()

    host = parts.get('host', 'localhost')
    port = parts.get('port', '5432')
    dbname = parts.get('database', 'postgres')
    user = parts.get('username', 'postgres')
    password = parts.get('password', '')
    sslmode = 'require' if 'require' in parts.get('ssl mode', '').lower() else 'prefer'

    return f"host={host} port={port} dbname={dbname} user={user} password={password} sslmode={sslmode}"


# ── Database Setup ──────────────────────────────────────────────────────────

def ensure_schema():
    """Create the pgvector extension and knowledge_chunks table if they don't exist."""
    dsn = _get_db_connection_string()
    conn = psycopg2.connect(dsn)
    conn.autocommit = True
    cur = conn.cursor()

    cur.execute("CREATE EXTENSION IF NOT EXISTS vector;")

    cur.execute(f"""
        CREATE TABLE IF NOT EXISTS knowledge_chunks (
            id SERIAL PRIMARY KEY,
            content_hash TEXT UNIQUE NOT NULL,
            title TEXT NOT NULL,
            content TEXT NOT NULL,
            category TEXT NOT NULL,
            source TEXT NOT NULL DEFAULT 'HomePlannerAI Knowledge Base',
            embedding vector({EMBEDDING_DIM}),
            created_at TIMESTAMPTZ DEFAULT NOW()
        );
    """)

    cur.execute("""
        CREATE INDEX IF NOT EXISTS idx_knowledge_chunks_embedding
        ON knowledge_chunks USING ivfflat (embedding vector_cosine_ops)
        WITH (lists = 10);
    """)

    cur.close()
    conn.close()
    logger.info("Knowledge schema ensured.")


# ── Embedding Generation ────────────────────────────────────────────────────

def _generate_embedding(text: str) -> list[float]:
    """Generate an embedding using OpenAI text-embedding-3-small."""
    if not OPENAI_API_KEY:
        raise RuntimeError("OPENAI_API_KEY is required for embedding generation.")

    resp = requests.post(
        "https://api.openai.com/v1/embeddings",
        headers={
            "Authorization": f"Bearer {OPENAI_API_KEY}",
            "Content-Type": "application/json",
        },
        json={
            "model": EMBEDDING_MODEL,
            "input": text,
        },
        timeout=30,
    )
    resp.raise_for_status()
    return resp.json()["data"][0]["embedding"]


def _generate_embeddings_batch(texts: list[str]) -> list[list[float]]:
    """Generate embeddings for a batch of texts."""
    if not OPENAI_API_KEY:
        raise RuntimeError("OPENAI_API_KEY is required for embedding generation.")

    resp = requests.post(
        "https://api.openai.com/v1/embeddings",
        headers={
            "Authorization": f"Bearer {OPENAI_API_KEY}",
            "Content-Type": "application/json",
        },
        json={
            "model": EMBEDDING_MODEL,
            "input": texts,
        },
        timeout=60,
    )
    resp.raise_for_status()
    data = resp.json()["data"]
    # Sort by index to maintain order
    data.sort(key=lambda x: x["index"])
    return [d["embedding"] for d in data]


# ── Chunking ────────────────────────────────────────────────────────────────

@dataclass
class Chunk:
    title: str
    content: str
    category: str
    source: str
    content_hash: str


def _chunk_document(doc: dict[str, str]) -> list[Chunk]:
    """Split a document into overlapping chunks."""
    title = doc["title"]
    content = doc["content"]
    category = doc["category"]
    source = doc.get("source", "HomePlannerAI Knowledge Base")

    # If content is small enough, return as a single chunk
    if len(content) <= CHUNK_MAX_CHARS:
        h = hashlib.sha256(f"{title}:{content}".encode()).hexdigest()[:16]
        return [Chunk(title=title, content=content, category=category, source=source, content_hash=h)]

    # Split on sentence boundaries
    sentences = re.split(r'(?<=[.!?])\s+', content)
    chunks = []
    current = ""
    for sentence in sentences:
        if len(current) + len(sentence) > CHUNK_MAX_CHARS and current:
            h = hashlib.sha256(f"{title}:{current}".encode()).hexdigest()[:16]
            chunks.append(Chunk(title=title, content=current.strip(), category=category, source=source, content_hash=h))
            # Overlap: keep last portion
            words = current.split()
            overlap_words = words[-max(1, len(words) // 4):]
            current = " ".join(overlap_words) + " " + sentence
        else:
            current = (current + " " + sentence).strip()

    if current.strip():
        h = hashlib.sha256(f"{title}:{current}".encode()).hexdigest()[:16]
        chunks.append(Chunk(title=title, content=current.strip(), category=category, source=source, content_hash=h))

    return chunks


# ── Storage ─────────────────────────────────────────────────────────────────

def store_chunks(chunks: list[Chunk], embeddings: list[list[float]]):
    """Store chunks with embeddings into pgvector."""
    dsn = _get_db_connection_string()
    conn = psycopg2.connect(dsn)
    cur = conn.cursor()

    stored = 0
    skipped = 0
    for chunk, emb in zip(chunks, embeddings):
        emb_str = "[" + ",".join(str(x) for x in emb) + "]"
        try:
            cur.execute(
                """
                INSERT INTO knowledge_chunks (content_hash, title, content, category, source, embedding)
                VALUES (%s, %s, %s, %s, %s, %s::vector)
                ON CONFLICT (content_hash) DO NOTHING
                """,
                (chunk.content_hash, chunk.title, chunk.content, chunk.category, chunk.source, emb_str)
            )
            if cur.rowcount > 0:
                stored += 1
            else:
                skipped += 1
        except Exception as e:
            logger.warning(f"Failed to store chunk {chunk.content_hash}: {e}")
            conn.rollback()
            skipped += 1

    conn.commit()
    cur.close()
    conn.close()
    logger.info(f"Stored {stored} chunks, skipped {skipped} (already exist).")
    return stored, skipped


# ── Seed Pipeline ───────────────────────────────────────────────────────────

def seed_knowledge_base():
    """Full pipeline: load seed docs → chunk → embed → store."""
    from app.knowledge.seed_documents import KNOWLEDGE_DOCUMENTS

    print("[RAG] Ensuring database schema...")
    ensure_schema()

    print(f"[RAG] Processing {len(KNOWLEDGE_DOCUMENTS)} documents...")
    all_chunks: list[Chunk] = []
    for doc in KNOWLEDGE_DOCUMENTS:
        all_chunks.extend(_chunk_document(doc))

    print(f"[RAG] Created {len(all_chunks)} chunks. Generating embeddings...")
    texts = [f"{c.title}: {c.content}" for c in all_chunks]

    # Batch in groups of 20
    all_embeddings: list[list[float]] = []
    batch_size = 20
    for i in range(0, len(texts), batch_size):
        batch = texts[i:i + batch_size]
        embs = _generate_embeddings_batch(batch)
        all_embeddings.extend(embs)
        print(f"  Embedded {min(i + batch_size, len(texts))}/{len(texts)}")

    print("[RAG] Storing chunks in pgvector...")
    stored, skipped = store_chunks(all_chunks, all_embeddings)
    print(f"[RAG] Done! Stored: {stored}, Skipped (duplicates): {skipped}")
    return stored, skipped


# ── Semantic Retrieval ──────────────────────────────────────────────────────

@dataclass
class RetrievedChunk:
    title: str
    content: str
    category: str
    source: str
    similarity: float


def search_architecture_knowledge(
    query: str,
    top_k: int = TOP_K_DEFAULT,
    category_filter: str | None = None,
) -> list[RetrievedChunk]:
    """
    Semantic search against the knowledge base.
    Returns top-k most relevant chunks above the similarity threshold.
    """
    query_embedding = _generate_embedding(query)
    emb_str = "[" + ",".join(str(x) for x in query_embedding) + "]"

    dsn = _get_db_connection_string()
    conn = psycopg2.connect(dsn)
    cur = conn.cursor()

    if category_filter:
        cur.execute(
            """
            SELECT title, content, category, source,
                   1 - (embedding <=> %s::vector) AS similarity
            FROM knowledge_chunks
            WHERE category = %s
            ORDER BY embedding <=> %s::vector
            LIMIT %s
            """,
            (emb_str, category_filter, emb_str, top_k)
        )
    else:
        cur.execute(
            """
            SELECT title, content, category, source,
                   1 - (embedding <=> %s::vector) AS similarity
            FROM knowledge_chunks
            ORDER BY embedding <=> %s::vector
            LIMIT %s
            """,
            (emb_str, emb_str, top_k)
        )

    results = []
    for row in cur.fetchall():
        title, content, category, source, similarity = row
        if similarity >= SIMILARITY_THRESHOLD:
            results.append(RetrievedChunk(
                title=title,
                content=content,
                category=category,
                source=source,
                similarity=round(float(similarity), 4),
            ))

    cur.close()
    conn.close()
    return results


def search_knowledge_as_dicts(
    query: str,
    top_k: int = TOP_K_DEFAULT,
    category_filter: str | None = None,
) -> list[dict[str, Any]]:
    """Convenience wrapper returning dicts for API serialization."""
    results = search_architecture_knowledge(query, top_k, category_filter)
    return [
        {
            "title": r.title,
            "content": r.content,
            "category": r.category,
            "source": r.source,
            "relevance_score": r.similarity,
        }
        for r in results
    ]
