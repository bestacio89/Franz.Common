# Contributing to Franz.Common

Thanks for helping keep Franz clean, consistent and professional.  
We follow a lightweight, disciplined workflow to keep history readable and releases predictable.

## Quick overview

- Repo owner / maintainer: Bernardo (primary)
- Keep commits small and focused.
- Use the commit tag convention below to make logs readable and release automation simple.

---

## Commit message conventions

Start commit messages with a **scope tag** in square brackets, followed by a short title and optional body.

Examples:
- `[Feat] Add ArasInnovator DI bootstrapper`
- `[Patch] Fix domain events dispatch to PublishAsync`
- `[Fix] NullRef in MapperFactory when X is null`
- `[Doc] Upgrade README + CHANGELOG for v1.5.0`
- `[Test] Add integration tests for Kafka publisher`
- `[Chore] Update CI pipeline node version`

### Common tags
- `[Feat]` — new features (minor/major)
- `[Patch]` — backward-compatible bug fixes/refactors
- `[Fix]` — bug fixes (use when user-facing bug)
- `[Doc]` — documentation only
- `[Test]` — tests or test infra
- `[Chore]` — maintenance tasks: CI, deps, renames
- `[Release]` — release commit / tag preparation

### Format
```

\[TAG] Short summary (imperative)

* Optional bullet list explaining changes
* Link to issue/PR if applicable

```

---

## Branching & Workflow

We use a simple Git Flow variant:

- `main` — stable production branch (always releasable)
- `develop` — integration branch for the next release
- `feature/<short-desc>` — feature work merged into `develop`
- `hotfix/<short-desc>` — critical fixes applied to `main` and `develop` as needed
- `release/<version>` — used only for release prep (optional)

Workflow:
1. Create branch from `develop` (or `main` for hotfix).
2. Make focused commits with tags.
3. Open PR against `develop`. Use PR template.
4. Merge after review + green CI.
5. When ready to cut release, create `release/X.Y.Z` or tag `vX.Y.Z` on `main`.

---

## Pull Request checklist

- [ ] Title uses tag convention (e.g. `[Patch] Fix ...`)
- [ ] Body explains rationale & scope (not only implementation)
- [ ] Tests added/updated for behavior changes
- [ ] README/CHANGELOG updated if public API or UX changed
- [ ] CI passes
- [ ] No hard-coded secrets / credentials in code

---

## Tagging & Releases

- Tag format: `v<major>.<minor>.<patch>` (e.g. `v1.5.0`)
- Annotated tag recommended:
```

git tag -a v1.5.0 -m "Franz v1.5.0 — When Aras Becomes Simple"
git push origin main --tags

```
- Release notes:
- Keep `CHANGELOG.md` updated for `1.4.5` + `1.5.0` entries (we use a concise structured format).
- Use the release tag message as the short summary on GitHub/NuGet.

---

## Docs & READMEs

- Docs-only changes should be committed with `[Doc]` tag.
- Update package README under the subpackage folder (for `Franz.Common.Business`, `...EntityFramework`, etc.)
- Consolidated / root README updates go to the repo root.

---

## PR & Issue templates (suggestion)

### PR title
`[Patch] Short summary`

### PR body guidance
- Summary (1–3 lines)
- Changes (bullet list)
- Tests (what you ran)
- Migration/Upgrade notes (if any)
- Release note entry (one-liner for CHANGELOG)

---

## What to do if you’re the only contributor (solo mode)
- Keep `develop` as the active branch for WIP, then merge cleanly into `main` for release.
- Tag releases on `main` after merging `develop`.
- Keep a small changelog entry for each patch/release.

---

## Contact / Code of Conduct
- If anyone else contributes, be civil and helpful. Follow common open-source etiquette.

```

---



