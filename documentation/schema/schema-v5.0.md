# Stream Forge Database Schema v5.0

**Version:** 5.0  
**Date:** June 21, 2026  
**Status:** Updated - Transcript Chunk Persistence and System Settings Foundation

---

## Overview

Schema v5.0 builds on [schema-v4.0.md](schema-v4.0.md) and adds the first persistence layer needed for:

- transcript chunk storage with timestamps
- keyword transcript search
- admin-managed transcription settings backed by a database table

All unchanged tables and relationships from v4.0 remain part of the schema. This document focuses on the new and changed schema elements introduced for this implementation pass.

---

## New Tables

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
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_VideoTranscriptChunks_VideoId`
- `IX_VideoTranscriptChunks_TranscriptionId`
- `IX_VideoTranscriptChunks_VideoId_StartSeconds`
- `IX_VideoTranscriptChunks_VideoId_Language`

**Notes:**
- The first implementation persists one chunk per ingested transcription segment.
- Chunks are currently optimized for keyword search and grounded retrieval.
- Embedding/vector columns are intentionally deferred to a later schema revision when semantic retrieval is introduced.

---

### 23. SystemSettings

Generic key-value store for platform-level runtime settings managed through the application.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique system setting identifier |
| Key | VARCHAR(200) | NOT NULL, UNIQUE | Stable configuration key |
| Value | TEXT | NOT NULL | Serialized setting value |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_SystemSettings_Key` (UNIQUE)

**Notes:**
- The first use of this table is admin-managed transcription settings.
- Application configuration from appsettings remains the fallback/default source.
- This table stores product-level settings, not deployment secrets such as callback secrets or worker base URLs.

---

## Changed Tables

### 10. VideoTranscriptions

`VideoTranscriptions` remains the canonical metadata table for stored caption/transcription artifacts.

There is no structural change to `VideoTranscriptions` in v5.0, but it now acts as the parent source for `VideoTranscriptChunks`.

**Relationship additions:**
- `VideoTranscriptions (1) -> (N) VideoTranscriptChunks`

**Behavior notes:**
- A completed transcription callback now persists caption artifacts and derives chunk rows from `segments.json`.
- When a language is re-transcribed, prior chunks for that `(VideoId, Language)` are replaced by the latest ingested set.

---

## Relationships Summary Additions

```
Videos (1) -> (N) VideoTranscriptChunks
VideoTranscriptions (1) -> (N) VideoTranscriptChunks
```

---

## Implementation Notes

### Transcript chunk persistence

- The Python worker continues to stage `segments.json`.
- `.NET` ingests `segments.json` during transcription completion.
- The current implementation stores one database row per transcript segment with start/end times and text.

### Admin-managed transcription settings

The first set of settings expected in `SystemSettings` includes keys such as:

- `transcription.enabled`
- `transcription.autoTranscribeOnReady`
- `transcription.provider`
- `transcription.defaultLanguage`
- `transcription.outputFormats`
- `transcription.localFasterWhisper.model`
- `transcription.localFasterWhisper.device`
- `transcription.localFasterWhisper.computeType`
- `transcription.localFasterWhisper.beamSize`
- `transcription.localFasterWhisper.enableVad`
- `transcription.localFasterWhisper.enableWordTimestamps`

These values override appsettings defaults at runtime for product behavior, but operational infrastructure values remain outside this table.

---

## Deferred From v5.0

The following are intentionally not part of this schema revision:

- `pgvector` columns and indexes
- embedding storage
- semantic retrieval tables or metadata
- Q&A conversation/session persistence

Those should land in a later schema version once semantic retrieval and answer-generation behavior are implemented.

---

## Change Log

### v5.0 (June 21, 2026)
- Added `VideoTranscriptChunks` for timestamped transcript chunk persistence
- Added `SystemSettings` for admin-managed platform settings
- Defined v5 relationship model between `VideoTranscriptions` and transcript chunks
- Clarified that embeddings and vector retrieval are deferred beyond this schema revision
