# Phase 13 - External Identity Provider Integration

Status: [ ] Planned

## Purpose

Add OAuth/OIDC-based external sign-in after the core product APIs are in place. This phase should support providers such as Google without coupling StreamForge content, upload, playback, engagement, or analytics use cases to provider-specific claims or token formats.

## Strategy

Use Option A first: external login with internal StreamForge JWTs.

The external provider proves the user's identity, but StreamForge remains the authorization system. After a successful external login, the API resolves or creates a local `User`, links the external identity to that user, and issues normal StreamForge access and refresh tokens. Downstream controllers and Application use cases continue to consume `ICurrentUserService.UserId`, `ICurrentUserService.Role`, and `IAuthorizationService`; they should not know whether the user signed in with password credentials, Google, Azure AD, Auth0, or another OIDC provider.

## Goals

- Add provider configuration for enabled OAuth/OIDC providers.
- Add Sign in with Google as the first external-provider flow if product requirements still call for it.
- Add external identity linking through a local `UserExternalLogins` model.
- Resolve external identities to internal `Users.Id` before authorization.
- Return normal StreamForge `AuthResponseDto` tokens after external sign-in.
- Preserve a later migration path to direct external bearer-token validation if enterprise OIDC requires it.

## Planned External Login Flow

1. React app starts provider login, such as Sign in with Google.
2. Client sends the provider ID token or authorization-code result to StreamForge.
3. StreamForge validates the external credential with the provider.
4. StreamForge reads the provider's stable subject identifier, such as Google `sub`.
5. StreamForge finds or creates a local `User`.
6. StreamForge links `(Provider, ProviderSubjectId)` to the local `User.Id`.
7. StreamForge returns its own normal `AuthResponseDto` with internal access and refresh tokens.

## Planned API Surface

- `GET /api/v1/auth/external/providers` to list enabled providers for the frontend.
- `POST /api/v1/auth/external/{provider}` to complete external sign-in and return StreamForge tokens.
- `GET /api/v1/auth/external/logins` to list linked providers for the current user if account settings require it.
- `DELETE /api/v1/auth/external/logins/{externalLoginId}` to unlink a provider if account settings require it.

## Planned Data Model

- `UserExternalLogins.Id`
- `UserExternalLogins.UserId`
- `UserExternalLogins.Provider`
- `UserExternalLogins.ProviderSubjectId`
- `UserExternalLogins.Email`
- `UserExternalLogins.CreatedAt`

## Decoupling Requirements

- Do not identify external users by email alone; use the provider's stable subject ID plus provider name.
- Do not store provider-specific authorization logic in content, upload, playback, engagement, or analytics use cases.
- Do not expose external provider IDs as the primary identity in StreamForge APIs.
- Keep local roles and permissions on the StreamForge user/access-control model unless a future enterprise role-mapping feature explicitly changes this.
- Keep the design adaptable to Option B later, where the API accepts external bearer tokens directly, by preserving the same final boundary: external principal resolves to local `User.Id` before authorization.

## Dependencies

- Phase 4 local authentication, JWT issuance, current-user resolution, and authorization abstractions.
- Phase 7.5 content APIs should already depend on internal `User.Id` rather than provider-specific claims.
- Provider-specific client IDs, secrets, issuer metadata, and redirect/origin settings must come from configuration.

## Acceptance Criteria

- The frontend can discover enabled external providers.
- A user can sign in with a configured external provider and receive normal StreamForge access and refresh tokens.
- External identities are linked to local StreamForge users by provider and provider subject ID.
- Existing authorization checks continue to use internal `User.Id` and StreamForge roles.
- Provider-specific details remain isolated from product use cases.
- The design does not block future direct external bearer-token validation.
- The project builds successfully after implementation.

## Notes

- Email from an external provider is metadata and may help with initial account matching, but it must not be the stable external identity key.
- Start with one provider before generalizing provider-specific edge cases too far.
- Account-linking, unlinking, and duplicate-email policies should be explicit before enabling multiple providers for the same user.
