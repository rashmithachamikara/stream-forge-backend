# Stream Forge Database Schema v6.0

**Version:** 6.0  
**Date:** July 1, 2026  
**Status:** Updated - PostgreSQL Full-Text Search For Transcript Chunks

---

## Overview

Schema v6.0 builds on [schema-v5.0.md](schema-v5.0.md) and upgrades transcript search from naive keyword matching to PostgreSQL full-text search.

This revision is intentionally narrow:

- keep the current `VideoTranscriptChunks` table as the transcript retrieval unit
- add PostgreSQL full-text search support for transcript chunk content
- keep embeddings, vectors, and semantic retrieval deferred to a later schema version

All unchanged tables and relationships from v5.0 remain part of the schema. This document focuses only on the schema changes introduced for lexical full-text transcript search.

---

## Changed Tables

### 22. VideoTranscriptChunks

Searchable transcript chunks derived from transcription segment data.

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
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_VideoTranscriptChunks_VideoId`
- `IX_VideoTranscriptChunks_TranscriptionId`
- `IX_VideoTranscriptChunks_VideoId_StartSeconds`
- `IX_VideoTranscriptChunks_VideoId_Language`
- `IX_VideoTranscriptChunks_SearchVector` (GIN)

**Notes:**
- `SearchVector` is generated from `Content` using PostgreSQL text search configuration `english`.
- The application still filters transcript search by `VideoId` and optional `Language`.
- Search results are now ordered by lexical relevance first, then by `StartSeconds` as a stable tie-breaker.
- This revision improves lexical retrieval only; it does not add semantic embeddings or vector similarity search.

---

## Relationships Summary

Unchanged from v5.0:

```
Videos (1) -> (N) VideoTranscriptChunks
VideoTranscriptions (1) -> (N) VideoTranscriptChunks
```

---

## Implementation Notes

### Transcript search behavior

- `GET /api/v1/videos/{videoId}/transcript-search?q=...` keeps the same API shape.
- The backing implementation now uses PostgreSQL full-text search instead of `ILIKE` matching.
- This keeps authorization, paging, and chunk-oriented response behavior stable while improving lexical relevance.

### Search vector maintenance

- `SearchVector` is database-generated from `Content`.
- Transcript chunk ingestion does not need separate application logic to maintain the search vector.
- Replacing transcript chunks for a `(VideoId, Language)` set automatically rebuilds full-text searchable content through normal row replacement.

---

## Deferred From v6.0

The following are still intentionally deferred:

- embedding/vector columns and metadata
- `pgvector` indexes
- semantic retrieval endpoints
- grounded Q&A persistence concerns

Those remain part of the later transcript-intelligence and RAG phase.

---

## Change Log

### v6.0 (July 1, 2026)
- Added generated `SearchVector` support to `VideoTranscriptChunks`
- Added GIN index for transcript chunk full-text search
- Upgraded transcript search persistence support from naive keyword matching to PostgreSQL full-text search
