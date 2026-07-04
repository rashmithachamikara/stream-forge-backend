# Phase 17 - Transcript Intelligence and RAG

Status: [~] In Progress

## Purpose

Build the transcript-intelligence layer on top of existing persisted `VideoTranscriptChunks`.

This phase adds:

- PostgreSQL full-text transcript search
- semantic retrieval over transcript embeddings
- hybrid retrieval using lexical plus semantic recall
- grounded Q&A with cited transcript evidence
- both per-video and cross-video answering flows

This phase starts after Phase 15 has already delivered:

- transcription orchestration
- caption persistence and delivery
- transcript chunk persistence
- keyword transcript search
- admin-managed transcription settings

## Backend Direction

This phase is backend-only.

It covers:

- schema additions required for full-text and vector retrieval
- retrieval and answer-generation abstractions
- embedding generation pipeline
- new search and Q&A APIs
- backend tests for retrieval, grounding, and authorization

It does not cover:

- transcript reader UX
- admin UI
- conversation/chat UI

## Planned Work

### 1. Full-Text Search Upgrade

Upgrade the existing transcript keyword search to PostgreSQL full-text search.

Behavior:

- keep `GET /api/v1/videos/{videoId}/transcript-search?q=...`
- preserve current authorization and `shareToken` behavior
- preserve current chunk-oriented response model
- replace naive keyword matching with PostgreSQL full-text search
- add relevance-based ordering tuned for transcript chunks
- support timestamped chunk matches as the output shape

Recommended result behavior:

- order by lexical relevance first
- then apply stable time ordering as a tie-breaker
- keep current paging and optional `language` filter

### 2. Embedding Pipeline

Add a dedicated embedding stage after transcript chunk persistence.

Direction chosen:

- do not generate embeddings inside the Python transcription worker
- do not block transcription callback completion on embedding generation
- use a separate background embedding pipeline

Recommended implementation:

- add an `ITranscriptEmbeddingProvider`
- add an `ITranscriptEmbeddingQueue`
- queue embedding generation automatically whenever transcript chunks are created or replaced
- implement the first queue/runtime using Hangfire

Current implementation status:

- `.NET` now enqueues a dedicated embedding job after canonical transcript chunks are persisted
- Hangfire now owns the first embedding-stage runtime
- a dedicated `services/embedding-worker/` FastAPI worker scaffold now exists with `POST /embed` and `GET /health`
- transcript chunk embeddings are now persisted with provider/model metadata
- schema v7.0 now adds `pgvector` storage support plus a first HNSW cosine index for transcript embeddings
- the embedding column is now fixed to `vector(384)` to match the current default embedding model `sentence-transformers/all-MiniLM-L6-v2`
- a follow-up migration now removes the previously-added redundant `EmbeddingDimensions` column
- runtime retrieval settings now include semantic/full-text candidate sizes plus starter hybrid weighting controls
- per-video and cross-video semantic search endpoints are now implemented over persisted transcript chunk embeddings
- per-video and cross-video hybrid search endpoints are now implemented over combined lexical + semantic retrieval
- grounded per-video and cross-video Q&A endpoints are now implemented over hybrid retrieval with backend-validated chunk citations
- the first answer-generation provider path now supports direct Gemini Developer API integration from the `.NET` backend

Background flow:

1. transcription callback persists/replaces transcript chunks for a `(VideoId, Language)` set
2. `.NET` enqueues an embedding job for that chunk set
3. embedding job loads the current canonical chunks
4. embedding provider generates vectors
5. vectors are stored on those chunk rows

Operational rules:

- embedding generation is eventually consistent
- lexical/full-text search remains usable even if embeddings are missing or stale
- re-transcription of a language replaces the old chunk set and triggers re-embedding automatically

### 3. Schema And Storage

Add a new schema version before implementing semantic retrieval.

Schema additions should include:

- embedding/vector storage on `VideoTranscriptChunks`
- provider/model metadata needed to know how vectors were produced
- PostgreSQL full-text search support for chunk content
- `pgvector` indexes for semantic retrieval

Keep the current chunk ownership model intact:

- one canonical chunk set per `(VideoId, Language)` replacement cycle
- `VideoTranscription` remains the canonical artifact metadata parent
- `VideoTranscriptChunks` remains the retrieval unit

Do not add Q&A conversation/session persistence in the first version.

### 4. Retrieval Abstractions

Add application abstractions for:

- `ITranscriptSearchProvider`
- `ITranscriptEmbeddingProvider`
- `IVideoQuestionAnsweringProvider`

Responsibilities:

- `ITranscriptSearchProvider`
  - run lexical retrieval
  - run semantic retrieval
  - combine and deduplicate hybrid retrieval results
- `ITranscriptEmbeddingProvider`
  - generate embeddings for transcript chunks
  - support model/version-aware indexing
- `IVideoQuestionAnsweringProvider`
  - generate answers from retrieved transcript evidence only
  - return answer metadata useful for citations/observability

The first implementation should stay provider-agnostic and not bake the feature into a single LLM vendor contract.

### 5. Semantic Retrieval

Add a separate semantic transcript search endpoint rather than overloading the existing one.

Per-video endpoint:

- `GET /api/v1/videos/{videoId}/transcript-semantic-search?q=...`

Behavior:

- same authorization model as the current transcript search endpoint
- scoped to one video
- returns chunk-level timestamped results
- includes semantic relevance score
- supports `language`, `page`, and `pageSize`

Cross-video endpoint:

- `GET /api/v1/transcript-semantic-search?q=...`

Behavior:

- supports both:
  - explicit `videoIds` scope
  - all accessible videos when no `videoIds` are supplied
- backend must intersect explicit `videoIds` with the caller’s allowed videos
- backend must never retrieve from unauthorized videos and filter afterward
- response remains chunk-level and must include:
  - `videoId`
  - `videoTitle`
  - `transcriptionId`
  - `chunkId`
  - `language`
  - `startSeconds`
  - `endSeconds`
  - `content`
  - `score`

Recommended retrieval semantics:

- semantic retrieval runs only over authorized chunks inside the resolved scope
- if embeddings are not available for part of the scope, return partial semantic results rather than failing the whole request

### 6. Hybrid Retrieval

Use hybrid retrieval as the default retrieval strategy for Q&A.

Hybrid retrieval should:

- run lexical/full-text retrieval
- run semantic/vector retrieval
- merge candidates
- deduplicate by `chunkId`
- rerank before answer generation

Suggested use:

- full-text search remains strongest for exact terms, names, and quoted phrases
- semantic retrieval handles paraphrases and concept-level similarity
- hybrid retrieval is the default for question answering

For direct search endpoints:

- per-video `transcript-search` remains lexical/full-text
- per-video and cross-video semantic endpoints remain semantic-first
- dedicated per-video and cross-video hybrid endpoints provide combined retrieval without changing the legacy lexical route

### 7. Grounded Q&A

Add stateless grounded Q&A endpoints.

Per-video endpoint:

- `POST /api/v1/videos/{videoId}/questions`

Cross-video endpoint:

- `POST /api/v1/questions`

Direction chosen:

- stateless only in v1
- no conversation/session persistence
- no chat history tables

Per-video Q&A behavior:

1. authorize access to the video
2. retrieve candidate chunks for that video using hybrid retrieval
3. build an answer-generation prompt from retrieved chunks only
4. generate an answer through `IVideoQuestionAnsweringProvider`
5. return answer with citations

Cross-video Q&A behavior:

1. resolve scope:
   - explicit `videoIds`, or
   - all accessible videos
2. retrieve candidate chunks only from the authorized scoped set
3. build answer-generation prompt from retrieved chunks only
4. return answer with cross-video citations

Response requirements:

- answer text
- citations
- each citation must include:
  - `videoId`
  - `videoTitle`
  - `chunkId`
  - `startSeconds`
  - `endSeconds`
  - cited excerpt text

Failure behavior:

- no relevant evidence should return a no-answer style response, not a hallucinated answer
- provider failure should return a clear backend failure response
- partial indexing state should not block lexical search or lexical-only fallback

### 8. Settings And Runtime Configuration

Use the existing `SystemSettings` model for Phase 17 runtime settings unless a later schema requires stronger provider/config tables.

Add settings groups for:

- transcript search
  - full-text enabled
  - semantic enabled
  - hybrid retrieval enabled
- embeddings
  - provider
  - model
  - automatic indexing enabled
  - batch size
- Q&A
  - provider
  - model
  - enabled
  - max retrieved chunks
  - max citations

Current implemented runtime retrieval keys:

- `rag.retrieval.defaultMode`
- `rag.retrieval.semanticTopK`
- `rag.retrieval.fullTextTopK`
- `rag.retrieval.hybridSemanticWeight`
- `rag.retrieval.hybridLexicalWeight`
- `rag.retrieval.hybridMaxCandidates`

Keep these separate from Phase 15 transcription-generation settings even though both live under the broader admin settings surface.

### Good First `SystemSettings` Key Set

The first persisted Phase 17 key set should include:

- `rag.enabled`
- `rag.semanticSearch.enabled`
- `rag.videoQuestions.enabled`
- `rag.crossVideoQuestions.enabled`
- `rag.embedding.provider`
- `rag.embedding.model`
- `rag.embedding.batchSize`
- `rag.retrieval.defaultMode`
- `rag.retrieval.semanticTopK`
- `rag.retrieval.fullTextTopK`
- `rag.retrieval.hybridSemanticWeight`
- `rag.retrieval.hybridLexicalWeight`
- `rag.retrieval.hybridMaxCandidates`
- `rag.qa.provider`
- `rag.qa.maxContextChunks`
- `rag.qa.maxCitations`
- `rag.qa.temperature`
- `rag.qa.maxOutputTokens`

Recommended intent for these keys:

- feature toggles:
  - `rag.enabled`
  - `rag.semanticSearch.enabled`
  - `rag.videoQuestions.enabled`
  - `rag.crossVideoQuestions.enabled`
- embedding runtime:
  - `rag.embedding.provider`
  - `rag.embedding.model`
  - `rag.embedding.batchSize`
- retrieval behavior:
  - `rag.retrieval.defaultMode`
  - `rag.retrieval.semanticTopK`
  - `rag.retrieval.fullTextTopK`
  - `rag.retrieval.hybridSemanticWeight`
  - `rag.retrieval.hybridLexicalWeight`
  - `rag.retrieval.hybridMaxCandidates`
- Q&A runtime:
  - `rag.qa.provider`
  - `rag.qa.maxContextChunks`
  - `rag.qa.maxCitations`
  - `rag.qa.temperature`
  - `rag.qa.maxOutputTokens`

Keep secrets and deployment-specific infrastructure values out of `SystemSettings`.
Those should remain in app configuration, environment variables, or a secret store.

## API Direction

### Keep And Upgrade

- `GET /api/v1/videos/{videoId}/transcript-search?q=...`
  - upgrade to PostgreSQL full-text behavior
  - keep current chunk-oriented shape

### Add

- `GET /api/v1/videos/{videoId}/transcript-semantic-search?q=...`
- `GET /api/v1/transcript-semantic-search?q=...`
- `GET /api/v1/videos/{videoId}/transcript-hybrid-search?q=...`
- `GET /api/v1/transcript-hybrid-search?q=...`
- `POST /api/v1/videos/{videoId}/questions`
- `POST /api/v1/questions`

### Optional Later Admin/Repair Endpoints

Possible later additions if needed:

- manual re-embed endpoint for a video/language set
- admin indexing status endpoint
- admin repair/resync endpoint for semantic index state

## Testing

Add backend tests for:

- PostgreSQL full-text search ranking and filtering
- lexical search authorization and `shareToken` behavior
- embedding job enqueue-on-transcript-completion behavior
- embedding refresh after transcript replacement
- semantic retrieval correctness for per-video scope
- semantic retrieval correctness for cross-video scope
- explicit `videoIds` scope intersected with authorization
- all-accessible-videos scope behavior
- hybrid retrieval deduping and reranking
- Q&A citation integrity
- no-answer behavior when evidence is insufficient
- provider failure behavior for embeddings and Q&A
- regression coverage so existing chunk and caption endpoints remain stable

## Notes

- Keep the current transcript chunk contract stable where possible so Phase 15 consumers are not broken.
- Signed media delivery remains Phase 16 and stays separate from transcript intelligence work.
- Full-text search remains part of the design even with RAG; semantic retrieval complements it rather than replacing it.
- Keep embedding-worker-specific implementation details in `services/embedding-worker/EXECUTION_PLAN.md`.
