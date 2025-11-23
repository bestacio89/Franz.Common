# Versioning Policy – Franz.Common

Franz.Common follows **semantic versioning (SemVer)**:

- **MAJOR**: breaking changes,
- **MINOR**: new features, backwards-compatible,
- **PATCH**: bug fixes, no API changes.

---

## 1. Breaking changes (MAJOR)

A change is considered breaking when:

- public APIs are removed or modified in incompatible ways,
- behavior changes in ways that could impact existing integrations,
- defaults are changed in a way that can alter runtime behavior.

Policy:

- Breaking changes are **batched** where possible.
- A migration guide is provided for each major version.
- Major versions are communicated ahead of time to consuming teams.

---

## 2. Minor versions

Minor updates:

- add new modules, extension methods, or behaviors,
- extend configuration options without breaking existing ones.

Policy:

- Minor releases should be safe to adopt after checking the changelog.
- If any risk exists, it must be documented explicitly.

---

## 3. Patch versions

Patch updates:

- fix bugs,
- improve performance,
- address security issues without changing public APIs.

Policy:

- Patch versions are recommended for all teams as soon as possible,
- Security-related patches may be flagged as mandatory.

---

## 4. Support Matrix

Example (adapt to reality):

- `1.x` – Active support (features + fixes),
- `0.x` – Legacy experimental versions, no active support.

Teams should align on a **minimum supported major version** per domain.
