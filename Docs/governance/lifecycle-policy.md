# Lifecycle Policy – Franz.Common

This document describes how Franz.Common is maintained over time.

---

## 1. Release cadence

- Regular releases: every 4–8 weeks (feature & improvements).
- Hotfix releases: as needed for critical bugs or security issues.

---

## 2. Deprecation Policy

- APIs scheduled for removal are marked `[Obsolete]` with clear messages.
- A deprecation period of **at least one minor version** is observed before removal.
- Deprecation notes appear in:
  - changelog,
  - migration docs,
  - XML comments where appropriate.

---

## 3. Branching Strategy

- `main` – stable releases (always releasable).
- `develop` – integration branch for next minor / feature set.
- `feature/*` – short-lived branches for specific work.

---

## 4. Backward Compatibility Guarantees

- Within the same major version:
  - no breaking changes without strong justification,
  - behavioral changes are documented and tested.

---

## 5. Adoption Guidelines

For new projects:

- Use the latest stable major + minor version.

For existing projects:

- Plan upgrades on a regular schedule (e.g., quarterly),
- Validate in non-production first,
- Follow migration guides where provided.
