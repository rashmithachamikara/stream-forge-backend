# Stream Forge - Execution Plan

## Phase Status

### Phase 1: Project Bootstrap and Foundation [X]
- [X] Create solution and Clean Architecture project structure
- [X] Configure project references and dependency flow
- [X] Create baseline folders for Domain, Application, Infrastructure, and API
- [X] Add baseline project documentation and development guides
- [X] Verify successful solution build

### Phase 2: Core Domain Model [X]
- [X] Implement core domain entities (19 tables aligned to schema)
- [X] Implement domain enumerations
- [X] Add domain exceptions and core interfaces
- [X] Implement access control and sharing domain concepts

### Phase 3: Infrastructure Data Layer [X]
- [X] Install EF Core and PostgreSQL packages
- [X] Implement `StreamForgeDbContext` with all required DbSets
- [X] Implement all entity configurations (Fluent API)
- [X] Align configurations with schema/domain naming
- [X] Verify successful build after infrastructure alignment

### Phase 4: Authentication and Authorization (Week 3-4) [X]
- [X] Install JWT packages
- [X] Implement JWT token service
- [X] Create authentication use cases (Login, Register, RefreshToken)
- [X] Add authentication middleware
- [X] Implement role-based authorization
- [X] Add auth abstractions (`ITokenService`, `ICurrentUserService`, `IAuthorizationService`)
- [X] Centralize video authorization rules (owner, visibility, access grants)
- [X] Add AccessControl query helpers for per-user and token-based sharing
- [X] Keep auth provider adapters pluggable for future OAuth/OIDC integration

### Phase 5: Video Upload (Week 4-5)
- [ ] Create storage abstraction (`IStorageService`)
- [ ] Implement local storage service
- [ ] Create UploadVideo use case
- [ ] Implement video repository
- [ ] Create Videos controller
- [ ] Add file upload endpoint

### Phase 6: Video Processing (Week 5-7)
- [ ] Install Hangfire/Quartz.NET
- [ ] Create video transcoding job
- [ ] Create thumbnail generation job
- [ ] Implement FFmpeg wrapper service
- [ ] Add job scheduling

### Phase 7: Video Streaming (Week 7-8)
- [ ] Implement HLS playlist generation
- [ ] Create streaming endpoint
- [ ] Add range request support
- [ ] Implement access control

### Phase 8: Engagement Features (Week 8-10)
- [ ] Implement likes/dislikes
- [ ] Add comments system
- [ ] Create bookmarks feature
- [ ] Build playlists functionality
- [ ] Add notifications

### Phase 9: Analytics (Week 10-11)
- [ ] Track video views
- [ ] Track watch time
- [ ] Create analytics dashboard
- [ ] Generate reports

### Phase 10: Testing (Week 11-12)
- [ ] Add unit tests for domain
- [ ] Add integration tests for repositories
- [ ] Add API tests for endpoints
- [ ] Add end-to-end tests

### Phase 11: Deployment (Week 12+)
- [ ] Dockerize application
- [ ] Set up CI/CD pipeline
- [ ] Configure production database
- [ ] Deploy to cloud

## Notes

- Detailed implementation notes for each phase will be maintained under `documentation/plans/`.
- This document tracks phase-level status only.
- Schema source of truth remains `documentation/schema/schema-v1.0.md`.
