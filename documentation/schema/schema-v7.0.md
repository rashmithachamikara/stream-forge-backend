# Stream Forge Database Schema v7.0

**Version:** 7.0  
**Date:** July 1, 2026  
**Status:** Updated - Transcript Embedding Storage And `pgvector` Support

---

## Overview

Schema v7.0 builds on [schema-v6.0.md](schema-v6.0.md) and adds the persistence foundation for transcript embeddings and semantic retrieval.

This revision introduces:

- embedding/vector storage on `VideoTranscriptChunks`
- embedding metadata needed to track how vectors were produced
- PostgreSQL `pgvector` support
- a first vector index for transcript-chunk semantic retrieval

All unchanged tables and relationships from v6.0 remain part of the schema. This document focuses on the schema changes introduced for transcript embedding persistence.

---

## Changed Tables

### 22. VideoTranscriptChunks

Searchable transcript chunks derived from transcription segment data, now extended to support both lexical and semantic retrieval.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique transcript chunk identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Parent video |
| TranscriptionId | GUID | FK -> VideoTranscriptions(Id), NOT NULL | Source transcription artifact row chosen as the canonical language artifact |
| Language | VARCHAR(10) | NOT NULL | ISO language code |
| StartSeconds | DOUBLE PRECISION | NOT NULL | Chunk start time |
| EndSeconds | DOUBLE PRECISION | NOT NULL | Chunk end time |
| Content | TEXT | NOT NULL | Transcript text for the chunk |
| SearchVector | TSVECTOR | GENERATED, NOT NULL | PostgreSQL full-text search vector derived from `Content` |
| Embedding | VECTOR | NULL | `pgvector` embedding for semantic retrieval |
| EmbeddingProvider | VARCHAR(100) | NULL | Provider used to generate the embedding |
| EmbeddingModel | VARCHAR(200) | NULL | Model used to generate the embedding |
| EmbeddingDimensions | INTEGER | NULL | Stored vector dimension count |
| EmbeddingGeneratedAt | TIMESTAMP | NULL | When the embedding was last generated |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_VideoTranscriptChunks_VideoId`
- `IX_VideoTranscriptChunks_TranscriptionId`
- `IX_VideoTranscriptChunks_VideoId_StartSeconds`
- `IX_VideoTranscriptChunks_VideoId_Language`
- `IX_VideoTranscriptChunks_VideoId_Language_EmbeddingGeneratedAt`
- `IX_VideoTranscriptChunks_EmbeddingProvider_EmbeddingModel`
- `IX_VideoTranscriptChunks_SearchVector` (GIN)
- `IX_VideoTranscriptChunks_Content_Trgm` (GIN, `gin_trgm_ops`)
- `IX_VideoTranscriptChunks_Embedding_Hnsw` (HNSW, `vector_cosine_ops`)

**Notes:**
- lexical retrieval from v6.0 remains intact
- `Embedding` is nullable because transcript chunks may exist before embedding generation completes
- embedding metadata is stored on each chunk row so the system can reason about model/provider provenance and re-embedding behavior
- the first vector index is optimized for cosine similarity search over transcript chunk embeddings

---

## Relationships Summary

Unchanged from v6.0:

```text
Videos (1) -> (N) VideoTranscriptChunks
VideoTranscriptions (1) -> (N) VideoTranscriptChunks
```

---

## Implementation Notes

### Embedding pipeline behavior

- transcript chunks are still persisted first through the transcription-completion flow
- embedding generation is a separate post-transcription background stage
- `VideoTranscriptChunks` remain the canonical retrieval unit for both lexical and semantic retrieval
- chunks can exist without embeddings during eventual-consistency windows

### Embedding storage model

- embeddings are stored directly on `VideoTranscriptChunks`
- provider/model/dimension/generation-time metadata is stored alongside the vector
- re-transcription of a `(VideoId, Language)` set replaces the old chunk set, and the replacement set gets re-embedded

### PostgreSQL extension requirements

Schema v7.0 requires:

- `pg_trgm`
- `vector`

`pg_trgm` continues to support fuzzy lexical search, while `vector` enables transcript embedding storage and semantic indexing.

---

## Deferred From v7.0

The following are still intentionally deferred:

- semantic search endpoints
- hybrid lexical + semantic retrieval orchestration
- grounded Q&A endpoints
- conversation/session persistence

Those remain part of the later Phase 17 retrieval and answering work.

---

## Change Log

### v7.0 (July 1, 2026)
- Added transcript embedding storage to `VideoTranscriptChunks`
- Added embedding provenance and indexing metadata columns
- Added PostgreSQL `vector` extension requirement
- Added HNSW vector index for cosine-similarity transcript retrieval
- Established the schema foundation for semantic transcript search and RAG
