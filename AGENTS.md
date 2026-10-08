# Personal project engineering rules

This is an independent personal portfolio project.

- Use trusted public package registries, public source code, and public documentation only.
- Never use Microsoft internal or other employer package feeds, mirrors, proxies, proprietary source, confidential documentation, or corporate credentials.
- Public Microsoft packages and documentation are permitted; the restriction concerns internal sources, not the publisher.
- Restore with the repository's nuget.config. Keep its public-only source mapping, audit sources, and isolated package cache intact.
- Exception: the separate package-free Orbit.LocalDemo and Orbit.LocalDemo.Checks projects restore with src/Orbit.LocalDemo/nuget.config, which clears all sources. Never add package references or employer-cache dependencies to this offline path.
- If public registry access fails, report the blocker. Do not substitute an internal feed, reuse a corporate-populated cache, suppress vulnerability warnings, or disable TLS verification.
- Preserve security, identity and access boundaries, credential confidentiality, and operational reliability. Never commit credentials, local databases, package caches, or generated output.
- Use synthetic data and deterministic tests. Test authentication must stay in the test assembly.
- Keep the space mission-control theme accessible and explain conventional authorization terminology.
- Do not publish, deploy, or grant privileges without explicit user approval.
