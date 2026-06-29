# Phase 17 - Transcript Intelligence and RAG

Status: [ ] Not Started

## Purpose

Build the next backend layer on top of persisted transcript chunks:

- PostgreSQL full-text transcript search
- semantic retrieval over transcript embeddings
- grounded video Q&A with cited time ranges

This phase starts after Phase 15 has already delivered:

- transcription orchestration
- caption persistence and delivery
- transcript chunk persistence
- keyword transcript search

## Scope

This phase includes backend execution only.

It covers:

- search/indexing improvements
- embedding pipeline design and persistence
- retrieval and answer-generation APIs
- schema additions required for semantic retrieval
- backend tests for retrieval and grounding behavior

## Planned Work

### 1. Full-Text Transcript Search

- replace the current keyword search implementation with PostgreSQL full-text search
- add ranking and ordering tuned for transcript chunks
- preserve filtering by video and language
- keep timestamped chunk results as the output shape

### 2. Embedding Pipeline

- introduce a dedicated embedding stage after transcript chunk persistence
- keep embedding generation decoupled from the Python transcription worker
- define provider abstraction for embeddings
- support re-indexing when models change

Recommended default direction:

- use a separate worker or background stage for embeddings
- do not couple embedding generation to the latency of caption completion

### 3. Schema And Storage

- add a new schema version before semantic retrieval changes
- add embedding/vector storage for transcript chunks
- add `pgvector` indexes and any supporting metadata needed for retrieval
- keep Phase 15 chunk persistence backward compatible

### 4. Semantic Retrieval

- add vector similarity retrieval over transcript chunks
- support hybrid retrieval where useful:
  - full-text recall
  - semantic recall
- keep retrieved chunks grounded to:
  - `VideoId`
  - `TranscriptionId`
  - `Language`
  - `StartSeconds`
  - `EndSeconds`

### 5. Video Q&A

- add question-answering endpoints over retrieved transcript passages
- require cited time ranges in answers
- keep answers grounded only in retrieved transcript evidence
- define failure behavior for:
  - no relevant passages
  - provider failures
  - partial retrieval/index state

## Suggested API Direction

- `GET /api/v1/videos/{videoId}/transcript-search?q=...` upgraded to full-text behavior
- `POST /api/v1/videos/{videoId}/questions`
- optional admin/indexing endpoints later if re-embedding or repair workflows are needed

## Testing

Add backend tests for:

- PostgreSQL full-text ranking/filtering behavior
- embedding pipeline orchestration
- semantic retrieval correctness
- citation integrity in Q&A responses
- no-result and provider-failure behavior
- re-index/re-embed flows when transcript content changes

## Notes

- Keep the current transcript chunk contract stable where possible so Phase 15 consumers do not break.
- Signed media delivery remains Phase 16 and should stay separate from transcript intelligence work.
