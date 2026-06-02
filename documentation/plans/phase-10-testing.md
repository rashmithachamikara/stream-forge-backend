# Phase 10 - Testing

Status: [ ] Planned

## Purpose

Add automated confidence around completed core behavior, including upload flows that were manually smoke-tested during Phase 5.

## Scope

- Add unit tests for domain entities and validation rules.
- Add Application use-case tests for video upload session creation, chunk upload, completion, and failure paths.
- Add integration tests for repositories, unit of work, EF mappings, and migrations.
- Add API tests for authentication, upload endpoints, video metadata, streaming, engagement, and analytics endpoints as those phases complete.
- Add end-to-end smoke coverage for the local/backend upload flow.

## Deferred Upload Tests From Phase 5

- Create-session succeeds with category, visibility, and tags.
- Create-session rejects invalid category, invalid tags, oversized files, and invalid MIME type.
- Upload-part rejects invalid part number, oversized chunk, checksum mismatch, and duplicate part.
- Complete-session rejects missing parts, non-contiguous parts, expired sessions, wrong owner, and total-size mismatch.
- Complete-session creates `VideoVersion` and `VideoFile`, marks the existing video ready, computes final checksum, and deletes chunks after success.
- Failure paths mark linked uploading videos failed where applicable.

## Acceptance Criteria

- Test projects are added to the solution with a repeatable local test command.
- Core domain and Application upload use cases have focused automated coverage.
- Repository and migration behavior is covered by integration tests.
- API-level upload tests cover create, part upload, completion, and common validation failures.
- CI can run the test suite without requiring manual local services beyond documented dependencies.
