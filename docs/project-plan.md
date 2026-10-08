# Orbit Dock Keymaster: .NET 10 project plan

## Purpose and scope

Build an independent public portfolio project demonstrating secure multi-tenant authorization, resource ownership, tenant isolation, and engineering judgment. This is not an identity provider or a production-ready SaaS platform.

All project source, documentation, local Git history, and project-specific agent context will live under:

`D:\private_repos\orbit-dock-keymaster`

Open this folder as a separate editor workspace before implementation. Do not copy employer source, internal configuration, credentials, or confidential designs from another repository. Public publishing is a separate user-approved step.

Keep secrets out of this directory and Git. Use .NET user secrets for local identity configuration and an approved runtime secret mechanism for any later deployment. User secrets are a development convenience, not an encrypted production vault. Build outputs and machine-local databases must be ignored by Git.

## Portfolio theme

Use a shared space mission-control theme for personal portfolio projects. This project's display name is Orbit Dock Keymaster, with organizations presented as fleets, projects as missions, and audit history as the flight log. The repository folder is orbit-dock-keymaster; solution and namespace names remain Orbit.

Keep technical names and authorization policies conventional. Explain themed terms in the documentation and show conventional role labels alongside friendly names where needed. Use clear permission errors, accessible contrast, keyboard navigation, and reduced-motion support. The theme must not obscure security behavior or imply unverified guarantees.

## Portfolio outcome

A reviewer can run the automated tests without a cloud account, see two synthetic organizations with different memberships, and understand why unauthorized or cross-tenant requests fail. A browser demonstration shows successful sign-in, tenant switching, role differences, ownership checks, and redacted audit history.

The README must clearly distinguish tested application behavior from identity-provider configuration, cloud controls, and operational guarantees that were not verified.

## Proposed stack

- .NET 10 and ASP.NET Core, with a supported installed SDK pinned in global.json.
- Razor Pages for a small server-rendered UI and minimal APIs for resource operations in the same host.
- Official ASP.NET Core cookie and OpenID Connect handlers for browser authentication. Never implement token validation or an identity provider.
- EF Core 10 and SQLite for the local MVP. Use relational SQLite tests, not EF Core's non-relational in-memory provider, for integrity behavior.
- MSTest for unit and integration tests, WebApplicationFactory for HTTP tests, and test-only authentication replacement.
- First-party ASP.NET Core OpenAPI support with the required package references verified during scaffolding; expose documentation in Development initially.
- Structured logging, ProblemDetails, and health/readiness endpoints.
- GitHub Actions for restore, build, tests, coverage reporting, and appropriate repository security checks when published.

Start with one web project and two test projects. Organize features into folders rather than adding microservices, CQRS infrastructure, a policy language, or unnecessary layers.

## Identity model

Application organizations are application tenants, not Microsoft Entra directories. The MVP supports multiple application organizations within one configured identity-provider trust boundary. Accepting arbitrary external issuers or multiple Entra directories is out of scope.

For the first real sign-in integration, use a personal Entra development tenant if available. Otherwise choose a maintained local OpenID Connect provider and document its setup. Verify account, licensing, SDK, and provider requirements before committing to provider-specific configuration.

Use a stable provider identifier, such as the configured issuer plus subject, to identify application users. Do not use email as a durable authorization identifier. Use validated claims supplied by the configured authentication handler, not manually decoded tokens. Verify the provider's claim mapping during integration.

Memberships and roles are owned by the application database. Do not grant application permissions merely because a token or client supplies a role or tenant ID. Tenant selection is a requested scope, not proof of membership.

Use cookies for same-origin browser requests in the MVP. Keep tokens off browser storage and out of logs. Configure cookie security, expiration, sign-in/out, and provider logout behavior explicitly. Bearer-token API clients are a later, separate design.

## Domain model

- User: internal ID and unique stable external identity.
- Organization: ID and display name.
- Membership: organization ID, user ID, role, and concurrency version; one membership per user per organization.
- Project: ID, organization ID, owner user ID, name, description, and concurrency version.
- Audit event: organization ID, actor internal ID, action, target ID, outcome, UTC timestamp, and correlation ID.

Use database foreign keys and composite constraints to prevent a project's owner from belonging to another organization. Index organization-scoped lookups. Never accept tenant ID, owner ID, or privileged fields through unrestricted entity binding.

Seed only synthetic local data. Disable development seeding and test authentication outside their intended environments.

## Authorization contract

| Operation | Viewer | Editor | Admin |
|---|---|---|---|
| List/read organization projects | Yes | Yes | Yes |
| Create project | No | Yes, self-owned | Yes, self-owned |
| Update/delete own project | No | Yes | Yes |
| Update/delete another member's project | No | No | Yes |
| List/manage memberships | No | No | Yes |
| Transfer ownership to an existing member | No | No | Yes |
| Read organization audit history | No | No | Yes |

All roles require active membership in the addressed organization. Admin rights are organization-scoped, never global. Ownership alone does not override a role downgrade.

Prevent self-promotion and non-admin membership changes. Prevent removing or demoting the last admin, including concurrent requests. Before membership removal, require explicit reassignment of owned projects. Keep integrity constraints and authorization decisions consistent.

For an authenticated nonmember or a resource outside the authorized organization, return 404 to avoid disclosing existence. Return 403 for a known member lacking permission to perform an operation. API requests without authentication return 401, not an HTML login redirect. Browser navigation can initiate sign-in.

## Tenant isolation and request security

- Require authenticated, authorized tenant scope before listing or loading resources.
- Scope reads, writes, joins, pagination, audit access, and membership changes by organization.
- Use tenant-scoped lookup plus resource-based authorization handlers; do not load a globally identified resource and trust a client tenant field.
- Use EF query filters only as defense in depth, never as the sole boundary. Avoid ambient or static tenant state.
- Reject attempts to change organization, owner, or role through unrelated DTO fields.
- Require antiforgery protection for cookie-authenticated state-changing API requests and forms. Verify rejection behavior in tests.
- Use input size limits, validation, secure output encoding, and safe error responses.
- Use HTTPS and appropriately scoped secure cookies. Do not enable permissive cross-origin access.
- Resolve membership from authoritative application state per request for the MVP so membership revocation applies to subsequent requests without waiting for a cookie renewal.
- Coordinate authorization and state mutation to prevent concurrency races from bypassing role, membership, ownership, or last-admin constraints.

## Delivery phases

### Phase 0: decisions and isolated setup

Confirm the project name, personal root, identity-provider option, and permission matrix. Scaffold only in the personal workspace. Pin compatible SDK/packages, enable nullable analysis, and document prerequisites.

Exit: independent solution builds and a smoke test passes; no employer repository files are changed.

### Phase 1: persistence and authorization core

Implement entities, migrations, scoped services, and resource-based authorization handlers. Seed two organizations and users with deliberately different memberships, including one user in both organizations.

Exit: role-matrix tests, ownership tests, tenant isolation tests, and relational integrity tests pass. Tests do not require a live identity provider.

### Phase 2: API and adversarial integration tests

Implement organization project CRUD, membership administration, and audit listing. Add explicit DTOs, validation, concurrency handling, ProblemDetails, and OpenAPI metadata.

Exit: HTTP tests prove 401/403/404 behavior, cross-tenant denial, mass-assignment resistance, immediate subsequent-request membership revocation, antiforgery enforcement, and concurrent last-admin protection. Verify that failed requests do not modify data.

### Phase 3: real sign-in and small UI

Configure the chosen OpenID Connect provider using official handlers. Build sign-in/out, tenant selection, project list/details, and admin-only membership/audit pages. Presentation adapts to permissions but never replaces server checks.

Exit: manually verify real login/logout with synthetic accounts. Verify the runtime configuration independently from fake-auth tests. Confirm provider-specific issuer/subject mapping, cookie protections, and redirect configuration. Record limitations such as already-issued sessions and in-flight requests.

### Phase 4: auditability and operational behavior

Persist successful administrative audit records transactionally with changes. Emit security-relevant denials through redacted structured logs without introducing cross-tenant audit access. Define retention and avoid claiming audit storage is tamper-proof.

Add liveness and local database readiness, cancellation propagation, safe errors, and database concurrency conflict behavior. Do not retry non-idempotent writes blindly.

Exit: tests cover failure paths, atomic successful-change auditing, stale update conflicts, and safe diagnostics. A short runbook explains configuration, migration, backup, and recovery limitations.

### Phase 5: public portfolio release

Create architecture and request-flow diagrams, a concise threat model, design decisions, permission matrix, quick start, synthetic demo instructions, and sample HTTP requests.

Add CI with pinned or reviewed actions, minimal workflow permissions, no secrets required for tests, and verification that test-only authentication cannot be enabled in deployment. Set a meaningful coverage target after measuring the baseline; security behavior tests remain mandatory regardless of coverage.

Exit: clean checkout instructions work, CI passes, secret review is complete, and the README accurately describes verified behavior. User approves GitHub publishing separately.

## Mandatory acceptance scenarios

1. Anonymous API callers cannot read or modify projects.
2. Viewer, Editor, and Admin permissions match every cell in the matrix.
3. Replacing organization or project IDs cannot reveal or change another organization's data.
4. A user in two organizations has distinct roles with no privilege carryover.
5. Lists, counts, membership views, audit history, and writes remain tenant-scoped.
6. Client-controlled role, tenant, and ownership fields cannot expand permissions.
7. Demotion/removal takes effect on subsequent requests even with an existing authenticated cookie.
8. Concurrent operations cannot remove all admins or bypass ownership/membership constraints.
9. Missing or invalid antiforgery tokens reject cookie-authenticated mutations.
10. Token contents, secrets, sensitive claims, and unnecessary personal data do not appear in logs or errors.
11. Valid operations still work; security fixes must not break intended authorized behavior.
12. Identity-provider outages and invalid configuration fail explicitly, never through anonymous or test-auth fallback.

## Bounded agentic engineering workflow

For each phase, choose one small task and freeze its scope, invariants, acceptance criteria, and validation commands before editing.

Loop: inspect -> plan -> implement -> focused tests -> review diff -> repair.

Allow at most three evidence-driven repair attempts per task. Stop for ambiguous permissions, privilege changes, missing credentials, repeated failures without new evidence, or exceeded scope. Report passed, failed, blocked, and not-run checks distinctly. Never weaken tests or authorization to make a loop pass.

Use fresh read-only review for identity and authorization changes. Keep project-specific task state inside this personal project's context, not in global reusable learning. No autonomous merge, cloud deployment, consent grant, or publishing.

## Deferred features

Multiple external identity issuers, enterprise federation, invitation delivery, custom roles, fine-grained sharing, separate SPA/mobile clients, distributed caches, production Azure hosting, and general-purpose policy engines.

After the MVP, choose one extension that strengthens the portfolio rather than implementing all of them.

## First implementation task

In a separate workspace rooted at the personal project folder, scaffold the .NET 10 solution, web project, unit and integration test projects, and a minimal database model. Add a failing cross-tenant read test and implement only the behavior needed to satisfy the agreed authorization contract. Do not enable real sign-in until provider configuration has been explicitly selected.

## First slice delivered

The solution, themed Razor landing page, SQLite model, resource-based membership checks, and protected read APIs are implemented. Unit and relational HTTP tests cover role-read rules, issuer/subject binding, cross-tenant access, ignored identity-provider role claims, next-request membership revocation, and owner foreign keys. Normal application authentication does not accept test identity headers.

This is a partial foundation, not completion of all Phase 1 or Phase 2 requirements. Write authorization, migrations, concurrency, audit persistence, and real sign-in remain pending. The live identity-provider choice is still required before Phase 3.
