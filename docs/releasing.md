# Releasing the NuGet package

GitHub Releases are the authoritative changelog and the release tag is the single source of package versions. The `0.1.0` version in the project file is a local-build fallback only.

## One-time setup

1. Create the `nuget` GitHub environment.
2. Add a `NUGET_USER` environment secret containing the nuget.org username `the_builder`.
3. Add required reviewers to the environment if publishing should require explicit approval.
4. Configure a trusted publishing policy on nuget.org with:
   - Package owner: `the_builder`
   - Publisher: GitHub Actions
   - Repository owner: `thebuilder`
   - Repository: `blur-placeholder`
   - Workflow: `publish-nuget.yml`
   - Environment: `nuget`

The trusted publishing policy must be active before the first release. It exchanges GitHub's short-lived OIDC token for a temporary NuGet API key, so no long-lived NuGet API key is stored in GitHub.

## Pre-release verification

Before creating a release:

1. Confirm all CI jobs pass for the commit to publish, including Umbraco 17.1, the latest 17.x, and the latest 18.x.
2. Run `dotnet list src/TheBuilder.BlurPlaceholder/TheBuilder.BlurPlaceholder.csproj package --vulnerable --include-transitive` and resolve any runtime high or critical findings.
3. Review `pnpm audit --prod`. The current `image-size` advisories affect only Blume's documentation build dependency, have no patched release, and reach neither the NuGet package nor the backoffice runtime. Re-evaluate that exception for every release.
4. Inspect the package produced by CI before approving the `nuget` environment deployment.

## Publish a prerelease

1. Create a GitHub Release from the commit to publish.
2. Give it a unique prerelease tag such as `v0.1.0-preview.1`.
3. Select **Set as a pre-release**.
4. Add the release notes.
5. Publish the release.

The workflow validates that the tag is SemVer, applies it to both the NuGet package and the generated Umbraco package manifest, links the NuGet release notes to the matching GitHub Release, rebuilds the backoffice assets, runs the test suite, packs `TheBuilder.BlurPlaceholder`, uploads the `.nupkg` as a workflow artifact, and publishes it to nuget.org after any configured environment approval.

NuGet package versions are immutable. Increment the prerelease number for every publish, even when a previous prerelease is unlisted.

## Publish a stable release

1. Create a GitHub Release from the commit to publish.
2. Use a new stable SemVer tag such as `v0.1.0`.
3. Ensure **Set as a pre-release** is not selected.
4. Add the release notes.
5. Publish the release.

The GitHub Release type and version must agree: prereleases require a prerelease version, and stable releases require a stable version.

## Rollback

NuGet packages cannot be overwritten or deleted. If a release is faulty:

1. Unlist the affected version on nuget.org.
2. Fix the issue on `main` and let CI complete.
3. Publish a new version. Never reuse the affected version number.
4. Document the replacement version in the affected GitHub Release.
