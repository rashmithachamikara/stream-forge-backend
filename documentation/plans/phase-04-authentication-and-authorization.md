# Phase 4 - Authentication and Authorization

Status: [X] Done

## Implemented Scope

- JWT bearer authentication
- Login, register, refresh token flows
- Role-based authorization
- Extensible auth abstraction for future OAuth/OIDC integration

## Architectural Guardrails

- Keep auth provider details behind interfaces
- Centralize video authorization rules (owner/visibility/access grants)
- Use AccessControl-based query helpers for per-user and token sharing
- Keep authorization based on the internal `Users.Id` GUID, not email, provider subject IDs, issuer names, or token-specific claims.
- External identity providers must resolve to a local StreamForge `User` before application use cases make authorization decisions.

## Deliverables

- `AuthController` with register, login, refresh, and current-user endpoints
- JWT token service and current-user resolver
- Auth service implementation backed by the existing `Users` table
- Centralized video authorization helper for ownership, visibility, and access-grant checks
- JWT configuration blocks in API appsettings files

## Dependencies

- Phase 2 domain model for `User`, `AccessControl`, and related enums
- Phase 3 infrastructure layer and EF Core migrations
- API configuration support for JWT settings and token secrets

## Implementation Plan

1. Add authentication and authorization abstractions to the Application layer.
2. Implement JWT token issuance and validation in Infrastructure.
3. Add auth use cases for login, registration, token refresh, and current-user context.
4. Register authentication middleware and authorization policies in the API startup pipeline.
5. Introduce reusable authorization helpers for owner, role, visibility, and access-grant checks.
6. Keep provider-specific logic behind interfaces so OAuth/OIDC can be added later without changing use cases.

## Deliverables

- `ITokenService`, `ICurrentUserService`, and `IAuthorizationService` abstractions
- JWT bearer authentication configuration in the API project
- Login, register, refresh token, and current-user flows
- Authorization policies and helper methods for protected video operations
- Configurable JWT settings in appsettings and environment-specific overrides

## Detailed Work Items

- Define auth contracts in the Application layer and keep them provider-agnostic.
- Create JWT settings options and bind them from configuration.
- Implement token generation, signing, expiration, and refresh-token support.
- Add password hashing and verification for credential-based login.
- Implement registration with role assignment and duplicate-account checks.
- Add current-user resolution from the request context for downstream use cases.
- Enforce role-based access in controllers and/or application handlers.
- Add AccessControl query helpers to support owner, public, and token-based sharing checks.
- Wire authentication and authorization into the middleware pipeline.

## Acceptance Criteria

- Users can register, authenticate, refresh tokens, and resolve the current user from a request.
- Protected endpoints reject anonymous or unauthorized access.
- Video access checks consistently honor owner, visibility, and AccessControl rules.
- Auth provider details remain isolated behind interfaces and can be swapped without rewriting use cases.
- The project builds successfully after auth wiring is added.

## Notes

- This phase should not introduce direct OAuth/OIDC dependencies; external identity provider integration is tracked later in [Phase 13](phase-13-external-identity-provider-integration.md).
- Token-related settings should come from configuration, not hard-coded values.
- Access control logic should stay centralized so future streaming and engagement features reuse the same rules.
