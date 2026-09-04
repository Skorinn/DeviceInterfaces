# Releasing

Releases are cut manually, only from `master`, and only after an explicit approval.

## Cutting a release

1. **Bump the version.** Edit `Properties/AssemblyInfo.cs` and set both `AssemblyVersion`
   and `AssemblyFileVersion` to the new `X.Y.Z.0`. This file is the single source of truth
   for the release version — the workflow reads it rather than taking a version input, so
   the tag, the release, and the shipped assembly can never disagree.
2. **Merge to `master`.** The workflow refuses to run from any other branch.
3. **Run the workflow.** Actions → *Release* → *Run workflow*, with `master` selected.
4. **Approve the deployment.** After the build succeeds the run pauses on the `release`
   environment and waits for your review. Nothing is tagged or published until you approve.
   Rejecting it leaves the repository untouched.

## What the workflow does

| Job | Runs on | Purpose |
| --- | --- | --- |
| `verify` | ubuntu | Fails if the run is not on `master`; reads and validates the version from `AssemblyInfo.cs`; fails if that version was already released. |
| `build` | windows | Rebuilds the solution in `Release` with MSBuild and checks the compiled DLL reports the expected version, then packages the DLL, PDB, `LICENSE`, and `README.md`. |
| `publish` | ubuntu | **Gated on approval.** Creates tag `vX.Y.Z` at the released commit and publishes a GitHub Release with auto-generated notes, the zip, and the bare DLL. |

## The two guards on "master only"

Both are needed, and they cover different things:

- The `verify` job fails the run outright if `github.ref` is not `refs/heads/master`. This
  stops a run started from the wrong branch before anything is built.
- The `release` environment has a deployment branch policy limiting it to `master`. This is
  enforced by GitHub itself rather than by workflow code, so it still holds if the guard job
  is ever edited or bypassed.

## Approval gate configuration

The `release` environment (Settings → Environments → release) is configured with:

- **Required reviewers:** `@Skorinn`. Self-review is permitted, so you can approve your own
  runs — necessary while you are the only maintainer.
- **Deployment branches:** `master` only.

Required reviewers are a paid feature on private repositories but free on public ones; this
repository is public, so the gate is available at no cost. If it is ever made private again on
a plan without deployment protection rules, **the gate silently stops applying** — the
`publish` job would run unattended. In that case, either restore a plan that supports it or
remove the `environment: release` line and treat the manual trigger as the only gate.
