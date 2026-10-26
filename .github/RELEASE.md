# Release workflow

## Release source of truth

- `common.targets` `<Version>` is the NuGet package version.
- Release promotion is controlled by merging the validated `develop` branch into `main` with a squash merge.
- The main-release workflow only creates a release when the package version is newer than the latest stable `vMAJOR.MINOR.PATCH` tag.
- The release tag points to the exact commit on `main`; GitHub Release notes are generated from merged pull requests.
- A main commit with no version increase is validated but does not create another release.

## Safe rollout and testing

The repository variable `RELEASE_DRY_RUN` defaults to safe mode when it is absent. In safe mode, the workflow restores and audits dependencies, builds, runs tests, packs all packages, and validates package versions, but does not create a tag, GitHub Release, or publish to NuGet.org.

1. Merge the GitHub workflow changes into `develop`.
2. Open and squash-merge the planned `develop` → `main` promotion PR.
3. Verify the `Main release` workflow run succeeds in dry-run mode and inspect its package artifact.
4. To perform the real release, run `Main release` manually against `main` with `dry_run=false`, or set repository Actions variable `RELEASE_DRY_RUN=false` to enable automatic releases on subsequent `main` pushes.
5. Configure NuGet.org Trusted Publishing for owner `bestacio89`, repository `Franz.Common`, workflow `nuget-release.yml`, with no environment restriction. Configure repository Actions variable `NUGET_USER` with the NuGet.org profile username, not the email address.

A failed NuGet publish can be retried by manually dispatching the workflow from the already-created release tag with `dry_run=false`; the workflow validates that the tag points to a commit reachable from `main` and uses `--skip-duplicate` for packages already accepted by NuGet.org.

## Azure DevOps mirror

The Azure DevOps mirror is a separate workflow triggered only by pushes to `develop`. It never merges or pushes to GitHub branches and never force-pushes the mirror. Configure `AZURE_REPO_URL`, `AZURE_REPO_USERNAME`, and `AZURE_REPO_PAT` as GitHub Actions secrets.
