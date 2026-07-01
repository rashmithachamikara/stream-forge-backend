# Embedding Worker - Local Execution Plan

Status: [~] In Progress

## Purpose

This plan covers the internal worker that generates transcript embeddings for Stream Forge.

This worker is not a public product API and should not own Stream Forge persistence rules. Its job is to:

- accept internal embedding requests from Stream Forge
- run embedding model inference over transcript chunk text
- return vectors back to Stream Forge
- expose simple worker health/runtime information

## Responsibilities

- expose an internal HTTP API for embedding inference
- load and run local embedding models
- accept chunk-text batches from Stream Forge
- return embeddings in a stable request/response contract
- support local runtime/model configuration

## Non-Responsibilities

- public user-facing APIs
- direct ownership of Stream Forge domain tables
- authorization logic for video access
- deciding which transcript chunks are canonical
- long-running callback orchestration like the transcription worker

## Suggested Project Shape

```text
services/embedding-worker/
  EXECUTION_PLAN.md
  README.md
  requirements.txt
  Dockerfile
  app/
    main.py
    api/
    models/
    services/
    adapters/
```

## Runtime Architecture

Recommended first implementation:

- FastAPI for the internal inference API
- stateless request/response embedding batches
- one or more local embedding models loaded in-process
- `.NET` Hangfire jobs orchestrate batching and persistence

Important design choice:

- the worker should be synchronous from the perspective of one HTTP request
- the overall product flow remains asynchronous because `.NET` invokes the worker from Hangfire

This means:

1. transcription callback persists transcript chunks
2. `.NET` enqueues an embedding job
3. Hangfire loads a batch of chunks
4. Hangfire calls the worker
5. worker returns embeddings in the same response
6. `.NET` stores vectors
7. Hangfire continues with the next batch if needed

## Internal API Contract

### Embed Batch

- `POST /embed`

Suggested request fields:

- `provider`
- `model`
- `items`
  - `chunkId`
  - `text`

Suggested request example:

```json
{
  "provider": "local-sentence-transformer",
  "model": "mixedbread-ai/mxbai-embed-large-v1",
  "items": [
    {
      "chunkId": "8c4d8f9a-0b1e-4fd8-9d55-3d5c9c1d2a10",
      "text": "Hello there. General Kenobi."
    }
  ]
}
```

Suggested response fields:

- `provider`
- `model`
- `vectorSize`
- `items`
  - `chunkId`
  - `embedding`

Suggested response example:

```json
{
  "provider": "local-sentence-transformer",
  "model": "mixedbread-ai/mxbai-embed-large-v1",
  "vectorSize": 1024,
  "items": [
    {
      "chunkId": "8c4d8f9a-0b1e-4fd8-9d55-3d5c9c1d2a10",
      "embedding": [0.0123, -0.0441, 0.9832]
    }
  ]
}
```

Format choices:

- request: `application/json`
- response: `application/json`
- embeddings represented as arrays of floats

Batching expectations:

- the worker should accept chunk batches rather than single-item requests by default
- `.NET` should send chunks in moderate batches such as 50-200 items depending on model/runtime constraints

### Health

- `GET /health`

Suggested checks:

- service is running
- model/runtime is available
- configured model can be loaded

### Optional Model Discovery Later

- `GET /models`

This can remain optional for the first pass.

## Model Support

Start with one local embedding path behind a provider abstraction, for example:

- sentence-transformers style embedding models
- BGE / E5 / mixedbread-style local models if supported by the chosen runtime

Worker config should support:

- provider name
- model name
- device (`cpu`, later optional `cuda`)
- batch size
- optional normalization behavior
- max input length / truncation behavior

## Input Handling

The worker should take normalized transcript chunk text from `.NET`.

Avoid:

- sending video files
- sending `VTT`
- sending `SRT`
- sending `segments.json`

The correct source of truth for embedding requests is:

- already-persisted `VideoTranscriptChunks`

`.NET` should:

- read chunk rows from the database
- batch them
- send text to the worker
- store returned vectors

## Output Handling

The worker should:

- return embeddings aligned with the submitted `chunkId`s
- keep response ordering stable or explicitly keyed by `chunkId`

The worker should not:

- write to Stream Forge tables
- persist embeddings itself

For Stream Forge, the cleaner long-term design is:

- worker computes vectors
- Stream Forge stores vectors and indexing metadata through its own persistence rules

## Error Handling

The worker should handle:

- unsupported provider/model configuration
- model load failures
- batch inference failures
- invalid request shape

Recommended first behavior:

- fail the whole batch if inference fails
- return explicit error details
- let Hangfire/.NET handle retries

Per-item partial failure can be added later if needed, but it should not be the default first contract.

## Security

This service should be internal-only.

Recommended protections:

- bind to internal network only
- validate request shape strictly
- avoid exposing local file-system details
- keep secrets and provider credentials out of public responses

## Integration With `.NET`

Recommended `.NET` side contract:

- `ITranscriptEmbeddingProvider`
- Hangfire embedding job service
- database persistence stays in `.NET`

The worker should be treated as a stateless inference dependency, not as an autonomous job orchestrator.

## Implementation Steps

- [X] Create Python project scaffold under `services/embedding-worker/`
- [X] Add `requirements.txt`
- [X] Add FastAPI entry point
- [X] Add request/response models for embedding batches
- [X] Add `POST /embed`
- [X] Add `GET /health`
- [X] Add local configuration model
- [X] Add local embedding-model integration service
- [ ] Add batching/performance safeguards for large chunk sets
- [X] Add Dockerfile
- [X] Add worker README with install/run instructions
- [ ] Add smoke test path

## Notes

- Stream Forge `.NET` remains the source of truth for canonical application state and embedding persistence.
- Hangfire should orchestrate embedding generation jobs; the worker should stay focused on synchronous inference for each submitted batch.
- This worker is intentionally simpler than the transcription worker because it does not need callback-based long-running job orchestration in the first version.
