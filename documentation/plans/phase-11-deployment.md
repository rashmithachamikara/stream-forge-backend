# Phase 11 - Deployment

Status: [ ] In Progress

## Purpose

Prepare Stream Forge for repeatable non-local environments by packaging the API as a deployable service, defining its runtime dependencies, and introducing a safe deployment workflow for database migrations, storage, background jobs, and operational configuration.

Phase 11 is not just "make it run in Docker." It should leave the backend with a deployment shape that is understandable, reproducible, and ready for CI/CD and production hardening.

## Current Runtime Shape

The backend currently depends on:

- ASP.NET Core Web API (`StreamForge.Api`)
- PostgreSQL for application data and Hangfire storage
- Local filesystem storage for uploaded files, source media, HLS assets, and thumbnails
- FFmpeg and ffprobe in the runtime environment
- Hangfire background processing running inside the API host

This means deployment must account for both request-serving and background-job execution in the same application process unless that topology changes later.

## Goals

- Package the API into a deployable container image.
- Make local and server deployment reproducible with explicit environment configuration.
- Define production-safe configuration boundaries for secrets, storage, CORS, JWT, analytics, and rate limits.
- Ensure PostgreSQL migrations and startup expectations are operationally clear.
- Introduce a first CI/CD workflow that builds, tests, and publishes deployable artifacts.
- Document a baseline production deployment topology and rollout flow.

## Out Of Scope

- Moving processing to a dedicated worker service
- Real S3 or Azure Blob production storage implementation
- CDN integration
- Kubernetes-specific manifests unless deployment targets require them immediately
- Full observability platform rollout beyond baseline logs and health checks
- Multi-region deployment or high-availability database architecture

Those can build on top of this phase once a stable single-environment deployment story exists.

## Proposed Deployment Topology

### Baseline

- One `streamforge-api` service
- One PostgreSQL service
- One persistent volume for local media storage
- Optional reverse proxy or ingress in front of the API

### Runtime Behavior

- The API serves HTTP requests and also runs Hangfire processing jobs.
- Uploaded source files, generated HLS assets, and thumbnails live under the configured `Upload:StoragePath`.
- FFmpeg and ffprobe must be installed in the container image or host environment.

### Production Assumption

The first production deployment can remain a single API container with persistent storage attached, as long as operators understand that scaling horizontally is limited while local-disk storage remains the active provider.

## Work Streams

## 1. Containerization

- Add a production `Dockerfile` for `StreamForge.Api`.
- Use a multi-stage .NET publish build.
- Install FFmpeg in the runtime image.
- Expose the API over a fixed container port.
- Ensure writable directories exist for `Upload:StoragePath`.
- Add `.dockerignore` to keep build context small and avoid shipping test outputs, uploads, and temp artifacts.

### Acceptance Notes

- `docker build` succeeds from the repo root.
- The image starts with environment-provided config and can connect to PostgreSQL.
- FFmpeg and ffprobe are available inside the container.

## 2. Compose-Based Environment

- Add a `docker-compose.yml` or `compose.yaml` for local deployment and server bootstrapping.
- Include:
  - `streamforge-api`
  - `postgres`
  - named volumes for PostgreSQL data and media storage
- Provide environment-variable-driven configuration instead of hard-coded secrets.
- Mount persistent media storage to the configured upload path.

### Acceptance Notes

- A new developer or operator can run the stack with one compose command after supplying env values.
- Uploads and generated media survive container restarts through a mounted volume.

## 3. Configuration And Secrets

- Standardize environment-variable mapping for all deployment-critical options:
  - `ConnectionStrings__DefaultConnection`
  - `Jwt__Issuer`
  - `Jwt__Audience`
  - `Jwt__SigningKey`
  - `Upload__StoragePath`
  - `VideoProcessing__FfmpegPath`
  - `VideoProcessing__FfprobePath`
  - `Cors__AllowedOrigins__0...n`
  - analytics and rate-limiter overrides where needed
- Document which values must never be committed and must come from secret stores or environment configuration.
- Add a production `.env.example` or deployment example config with placeholders only.
- Make production-safe defaults explicit where possible, but never for secrets.

### Acceptance Notes

- Operators can deploy without editing tracked source files.
- Production secrets are not expected to live in `appsettings.json`.

## 4. Database Migration Strategy

- Decide and document how migrations are applied in deployment:
  - manual pre-deploy command, or
  - dedicated migration job/container, or
  - controlled startup migration path
- Keep the current app behavior in mind:
  - startup checks pending migrations
  - startup does not apply migrations automatically
  - startup seeds only when the schema is current
- Add deployment docs for running:
  - `dotnet ef database update --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api`
  - or the containerized equivalent

### Recommendation

Start with an explicit migration step outside normal app startup. That is easier to reason about and avoids surprise schema changes during container restarts.

## 5. Health And Readiness

- Add health endpoints for deployment orchestration and monitoring.
- Separate basic liveness from deeper readiness where practical:
  - process alive
  - database reachable
  - storage path writable
- Define how startup should behave when PostgreSQL is unavailable or migrations are pending.

### Acceptance Notes

- Deployment systems can distinguish "container is alive" from "service is ready to receive traffic."

## 6. Persistent Storage And Backup Expectations

- Document that current production deployment requires persistent shared or host-mounted storage because local storage is the active provider.
- Define what data must be persisted:
  - PostgreSQL database
  - uploaded source media
  - generated HLS files
  - thumbnails and other derived media
- Call out current scaling limitation:
  - multiple API replicas are risky without shared storage or a remote object store

### Acceptance Notes

- Production deployment docs clearly explain that media persistence is not optional.
- Operators understand current horizontal-scaling limits.

## 7. Reverse Proxy And Networking

- Document expected front-door behavior for Nginx, Caddy, Traefik, or cloud ingress.
- Cover:
  - TLS termination
  - request body size limits for uploads
  - timeouts for chunk uploads
  - forwarded headers if required later
  - caching rules for playback assets if used

### Acceptance Notes

- Deployment docs include at least baseline reverse-proxy considerations for uploads and playback traffic.

## 8. CI/CD Pipeline

- Add an initial CI pipeline to:
  - restore
  - build
  - run default tests
  - optionally run Docker build validation
- Add a first CD-oriented workflow to publish container images on tagged releases or selected branches.
- Keep Docker/Testcontainers integration suites opt-in until CI agents with Docker are deliberately configured.

### Recommended First Step

- CI: build + `dotnet test StreamForge.sln --no-restore`
- Optional CI extension: `docker build`
- CD: publish versioned image to a container registry

## 9. Environment Profiles

- Define at least three deployment profiles:
  - local development
  - staging
  - production
- Document which settings differ by environment:
  - database connection
  - JWT signing key
  - allowed CORS origins
  - analytics toggles
  - rate limits
  - logging verbosity
  - storage path / mounted volume path

### Acceptance Notes

- The repo contains enough documentation and examples to stand up staging without guessing.

## 10. Operational Documentation

- Update `README.md` with Docker and compose startup instructions once implemented.
- Add deployment-specific documentation for:
  - required environment variables
  - migration flow
  - persistent-volume requirements
  - FFmpeg runtime requirement
  - production checklist
- Include common failure modes:
  - pending migrations
  - invalid JWT signing key
  - missing writable upload directory
  - FFmpeg not found
  - PostgreSQL connectivity failures

## Suggested Implementation Order

1. Add deployment plan and document runtime assumptions.
2. Add `Dockerfile` and `.dockerignore`.
3. Add compose-based local deployment with PostgreSQL and persistent volumes.
4. Add deployment environment variable examples and README instructions.
5. Add health checks and readiness endpoints.
6. Add CI workflow for build and default tests.
7. Add image publish workflow and deployment notes for staging/production rollout.

## Acceptance Criteria

- The API can be built into a container image from the repo root.
- A compose-based deployment can start PostgreSQL and the API with persistent storage.
- FFmpeg is available in the deployed runtime environment.
- Environment-variable-based configuration is documented for deployment-critical settings.
- Deployment docs explain the migration step and do not assume automatic schema upgrades.
- CI validates restore, build, and default tests on every relevant change.
- The deployment story clearly states current scaling constraints caused by local media storage.

## Risks And Constraints

- Local filesystem storage means true horizontal scaling is limited without shared storage or a future object-storage provider.
- Running Hangfire in the same API container is simpler now, but it couples background throughput to web-host scaling.
- Large uploads and playback traffic may require reverse-proxy tuning that is environment-specific.
- Production deployments need explicit retention, backup, and disk-capacity planning because media assets grow quickly.

## Dependencies

- Phase 5 upload flow
- Phase 6 processing and streaming
- Phase 9 analytics configuration
- Phase 10 testing for CI confidence
- Phase 12 advanced storage work for future production-scale media storage
- Phase 13 external identity provider integration for future auth-provider deployment configuration

## Notes

- The first milestone of Phase 11 should be Dockerization plus compose-based deployment, because that unlocks everything else.
- Keep deployment artifacts aligned with the app's current architecture instead of prematurely designing for a future worker/service split.
- Once object storage exists, revisit the deployment topology and scaling guidance in this plan.
