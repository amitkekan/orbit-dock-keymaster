# Orbit Dock Keymaster: Multi-Tenant Authorization in .NET 10

Different fleets. Different missions. Precisely scoped access.

Orbit Dock Keymaster is a personal .NET 10 multi-tenant authorization lab: a mission-control hub for fleets, missions, and access clearances. Fleet means organization (application tenant), and mission means project. It is independent of employer projects.

The repository folder is `orbit-dock-keymaster`; existing solution and namespace names remain `Orbit` to keep this branding change separate from technical refactoring.

## Run locally

### Offline work-machine simulator

The separate package-free simulator builds with the installed .NET SDK and shared framework.
It does not restore the SQLite application or download packages. No cloud subscription or identity provider is needed.

From the repository root:

```powershell
dotnet restore tests\Orbit.LocalDemo.Checks\Orbit.LocalDemo.Checks.csproj --configfile src\Orbit.LocalDemo\nuget.config
dotnet build tests\Orbit.LocalDemo.Checks\Orbit.LocalDemo.Checks.csproj --no-restore
dotnet run --project tests\Orbit.LocalDemo.Checks --no-build --no-launch-profile
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src\Orbit.LocalDemo --no-build --no-launch-profile
```

Open **http://127.0.0.1:5081**, not `localhost`. The listener and Host guard deliberately accept
only this address. Stop the foreground server with Ctrl+C. Startup fails outside Development;
configuration cannot add other listening endpoints. Other local processes can access the simulator.

With the simulator running, use a second terminal to check the live HTTP boundaries:

```powershell
dotnet run --project tests\Orbit.LocalDemo.Checks --no-build --no-launch-profile -- --http
```

Choose Alex, Blair, Casey, or Visitor to explore fleet membership and ownership decisions.
Alex is Admin in Aurora but Viewer in Nebula; Blair is Editor in Aurora.
Update/delete labels show policy decisions only, not implemented mutations.

**This is a synthetic authorization simulation, not authentication.** Persona selection is
publicly available to local callers. There are no credentials, cookies, real data, writes,
database persistence, or external calls. The original application's protected APIs are unchanged.
The package-free executable checks are separate from the existing MSTest suite.

Read-only examples:

```text
GET http://127.0.0.1:5081/simulation/alex/fleets
GET http://127.0.0.1:5081/simulation/blair/fleets/aurora/missions
GET http://127.0.0.1:5081/simulation/blair/fleets/nebula/missions  (404)
GET http://127.0.0.1:5081/healthz
```

Offline verification: source-free restore succeeded, build reported zero warnings/errors,
26 deterministic policy checks and 17 localhost HTTP checks passed. Browser interaction
verified persona selection, fleet navigation, Viewer denial, and the no-membership page.
Production startup was rejected, and listener configuration overrides still produced only
127.0.0.1:5081. A scoped read-only security review found no supported vulnerabilities in
the simulator changes. These results do not validate real authentication or the original
SQLite application's public-source build provenance.

### Original SQLite application (requires public NuGet access)

Install .NET SDK 10.0.401 or a compatible patch.

```powershell
# From the directory containing your clone:
Set-Location .\orbit-dock-keymaster
dotnet restore Orbit.slnx --configfile nuget.config
dotnet test Orbit.slnx
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src\Orbit.Web --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080 for the mission-control landing page. Development OpenAPI is at `/openapi/v1.json`; liveness and database connectivity checks are at `/healthz/live` and `/healthz/ready`.

Development creates an ignored SQLite database under `src\Orbit.Web\App_Data` with synthetic fixtures. This foundation uses `EnsureCreated`, not versioned migrations; migrations must be introduced before schema evolution or deployment. No production provisioning is performed.

## Current scope

- Space-themed, responsive, server-rendered landing page.
- Authenticated organization-scoped mission list/detail APIs.
- Database-owned membership checks with resource-based authorization.
- External identities matched by issuer and subject, not email or client role claims.
- Composite foreign key preventing ownership across organizations.
- Relational SQLite integration tests; no Azure, live identity provider, or secrets required.

**Real sign-in is not configured.** Normal API requests return 401; there is no login impersonation switch. Test identity headers work only in the test assembly and are not recognized by the running app. Synthetic fixtures are not credentials. Cookie configuration alone is not a working identity integration.

Nonmembers and cross-tenant resources return 404. Valid members may read their own organization's missions. Lists are ordered and capped at 100; pagination is deferred.

## Not implemented yet

OpenID Connect sign-in, project mutations and ownership authorization, memberships administration, concurrency handling, antiforgery tests for write APIs, persisted audit events, migrations, CI, and deployment. No production-readiness claim is made. Readiness currently verifies database connectivity, not schema correctness or identity-provider health.

Next: choose a personal Entra tenant or maintained local OpenID Connect provider. Do not add custom token validation or a development login bypass. Use .NET user secrets for local confidential configuration; never commit credentials.

Dependencies use explicit .NET 10 patch versions. The direct SQLite bundle reference updates the native dependency because the initial transitive version was flagged by NuGet vulnerability auditing. Keep auditing enabled and recheck dependencies before publishing.

## Personal-project source policy

Use trusted public sources only for packages, code examples, and research. Never use employer/internal feeds, proxies, proprietary code, confidential documentation, or corporate credentials for this project.

The repository's `nuget.config` clears inherited package and audit sources, enables only public NuGet.org, and maps all package IDs to that source. A Git-ignored project-local package cache avoids reuse of the machine-wide cache previously populated through other feeds. Do not override sources or use an internal mirror when public access fails; report the connectivity blocker instead. Microsoft-authored public packages and public documentation are allowed.

## Verified first slice

Release build and all 14 tests passed (4 unit tests and 10 integration tests). Live Development HTTP checks verified the landing page, liveness, database readiness, both OpenAPI paths, and a 401 response even when test identity headers are supplied. The verification server was stopped afterward.

NuGet's direct and transitive vulnerability check reported no vulnerable packages using the configured sources at verification time. This is not a complete security assessment. Real-provider authentication, write-path security, visual browser validation, and deployment have not been verified.

The initial build used machine-inherited feeds, including a corporate package proxy. Its results do not establish public-source package provenance. The new public-only configuration requires a fresh successful restore and verification before claiming that the application was built exclusively from public sources.

The fresh public-only restore attempt failed with NU1301 due to a TLS handshake failure reaching NuGet.org. Only NuGet.org was enabled; no internal fallback was used. Resolve public registry connectivity on an authorized personal environment, then rerun restore, tests, and vulnerability auditing. Existing build outputs are from the earlier configuration and must not be represented as a public-only verified build.

See [the project plan](docs/project-plan.md) for the permission matrix, delivery phases, and security acceptance criteria.

## License

This project's original source is licensed under the [MIT License](LICENSE).
Third-party dependencies retain their own licenses.
