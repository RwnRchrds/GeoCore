# Releasing GeoCore

Releases are driven by git tags. Pushing a tag matching `v*` runs
[`.github/workflows/release.yml`](.github/workflows/release.yml), which builds,
tests, packs, publishes to nuget.org and opens a GitHub release.

## One-time setup

The workflow cannot run until an API key exists. This has to be done by hand —
it is the only manual step.

1. **Create a NuGet API key.** Sign in at <https://www.nuget.org>, then
   *Account → API Keys → Create*.
   - **Key name:** `GeoCore CI`
   - **Glob pattern:** `GeoCore` — scope it to this package only, so a leaked
     key cannot be used to push anything else you own.
   - **Scopes:** *Push new packages and package versions*.
   - Set an expiry you will actually notice (365 days is the maximum).

   The `GeoCore` package ID is currently unclaimed. The first successful push
   claims it, and NuGet then reserves it to your account.

2. **Add it to the repository.** *Settings → Secrets and variables → Actions →
   New repository secret*:
   - **Name:** `NUGET_API_KEY`
   - **Value:** the key from step 1

   The workflow fails with a clear message if this secret is missing, rather
   than getting as far as a half-finished publish.

3. **Optional: require approval before publishing.** The job runs in an
   environment called `nuget`, created automatically on first run. To add a
   manual gate, go to *Settings → Environments → nuget* and add yourself as a
   required reviewer. Publishing then waits for your approval.

## Cutting a release

The tag is the single source of truth for the version. `<Version>` in
`GeoCore.csproj` is only a default for local builds — the workflow overrides it
with `-p:Version=<tag>`, so there is no second place to remember to update and
no way for the package number to disagree with the release.

```bash
# Make sure master is green first.
git checkout master && git pull

# Tag and push. Note the leading v on the tag.
git tag v0.2.0
git push origin v0.2.0
```

Then watch the run:

```bash
gh run watch
```

## What the workflow does

1. Resolves the version from the tag, rejecting anything that is not valid
   semver — a mistyped tag fails before anything is published.
2. Builds and **runs the full test suite**. A failing test stops the release.
3. Packs the library (both target frameworks, README, XML docs, Source Link)
   plus a `.snupkg` symbol package.
4. Lists the package contents into the log, so you can see exactly what shipped.
5. Pushes to nuget.org with `--skip-duplicate`, so re-running a partially failed
   release is safe.
6. Creates a GitHub release with generated notes and the `.nupkg` attached.

A version containing a hyphen (`v1.0.0-beta.1`) is published as a prerelease and
marked as such on both NuGet and the GitHub release.

## Publishing manually

Use *Actions → Release → Run workflow* and enter a version (a leading `v` is
accepted but not required). This tags the current commit for you. Useful for a
re-run after a transient failure, but prefer tagging for real releases.

## Versioning

GeoCore follows [semantic versioning](https://semver.org). While the major
version is `0`, the public API may still change between minor versions.

Things to bear in mind when bumping:

- Anything that changes a documented result — a formula, a rounding rule, a
  default unit — is a **breaking** change to callers even though it compiles,
  because their stored data will no longer match.
- `GeoPoint` validates on construction. Loosening or tightening that range is
  breaking in both directions.
- Adding a member to `DistanceUnit` or `AreaUnit` is safe; reordering them is
  not, because the numeric values are what get persisted.
