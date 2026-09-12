# Releasing GeoCore

Releases are driven by git tags. Pushing a tag matching `v*` runs
[`.github/workflows/release.yml`](.github/workflows/release.yml), which builds,
tests, packs, publishes to nuget.org and opens a GitHub release.

Authentication uses **NuGet Trusted Publishing**. The workflow exchanges a
GitHub OIDC token for an API key valid for one hour, so there is no long-lived
key stored in this repository to leak, rotate or forget about. NuGet.org
verifies the token against a policy that names this exact repository and
workflow file, so only a run of *this* workflow in *this* repository can publish.

## One-time setup

Two things, both in a browser. Neither involves a secret key.

### 1. Create the Trusted Publishing policy

On <https://www.nuget.org>: sign in, click your username, choose
**Trusted Publishing**, then create a policy with:

| Field | Value |
| --- | --- |
| **Repository Owner** | `RwnRchrds` |
| **Repository** | `GeoCore` |
| **Workflow File** | `release.yml` |
| **Environment** | `nuget` |

Two things that catch people out:

- **Workflow File is the file name only** — `release.yml`, *not*
  `.github/workflows/release.yml`.
- **Environment must be `nuget`**, because the job declares
  `environment: nuget`. If you leave it blank the policy still works, but
  filling it in is the more restrictive option and therefore the better one.

For **Scopes**, the policy must allow **publishing new packages** as well as new
versions, with a glob pattern of `GeoCore`. The `GeoCore` ID has never been
published, so the first release is a *new package* push — a policy scoped only
to new versions of existing packages will reject it. Scoping the glob to
`GeoCore` also means this policy cannot be used to push anything else you own.

Choose yourself as the **policy owner** unless GeoCore belongs to a nuget.org
organization. A policy owned by an organization goes inactive if you are later
removed from it.

### 2. Set your nuget.org username

The login action needs your nuget.org **profile name** — not your email address.
Add it under *Settings → Secrets and variables → Actions*:

- As a **variable** named `NUGET_USER` (recommended: it is not sensitive, and
  variables are visible in the UI, which makes a misconfiguration obvious), or
- as a **secret** named `NUGET_USER` if you prefer. The workflow accepts either.

The workflow fails early, before building, with a pointed message if this is
missing.

### 3. Optional: require approval before publishing

The job runs in an environment called `nuget`, created automatically on first
run. To add a manual gate, go to *Settings → Environments → nuget* and add
yourself as a required reviewer. Publishing then waits for your approval.

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
2. Checks `NUGET_USER` is set, before spending time on a build.
3. Builds and **runs the full test suite**. A failing test stops the release.
4. Packs the library (both target frameworks, README, XML docs, Source Link)
   plus a `.snupkg` symbol package.
5. Lists the package contents into the log, so you can see exactly what shipped.
6. Exchanges the OIDC token for a one-hour NuGet key. This happens immediately
   before the push, because the key is short-lived and single-use.
7. Pushes to nuget.org with `--skip-duplicate`, so re-running a partially failed
   release is safe.
8. Creates a GitHub release with generated notes and the `.nupkg` attached.

A version containing a hyphen (`v1.0.0-beta.1`) is published as a prerelease and
marked as such on both NuGet and the GitHub release.

## Publishing manually

Use *Actions → Release → Run workflow* and enter a version (a leading `v` is
accepted but not required). This tags the current commit for you. Useful for a
re-run after a transient failure, but prefer tagging for real releases.

## If publishing fails

- **`Unable to get an access token`, or the login step fails.** The policy on
  nuget.org does not match the run. Check the repository owner, the repository
  name, that **Workflow File** is `release.yml` with no path, and that
  **Environment** is either `nuget` or blank.
- **The push is rejected on the very first release.** The policy's scopes
  probably do not permit publishing *new* packages. See step 1.
- **It worked before and now fails after a rename.** The policy is bound to the
  workflow file name. Renaming or moving `release.yml` breaks it until the policy
  is updated.
- **The policy shows as pending or inactive.** Policies on private repositories
  start out active for only 7 days, because NuGet needs the repository and owner
  IDs from a real publish to pin the policy against repo-recreation attacks.
  GeoCore is public, so this should not apply; if it appears, restart the 7-day
  window from the policy page. A policy also goes inactive if it is owned by an
  organization you have left.

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
