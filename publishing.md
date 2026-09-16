# Packaging and publishing

Two packages are released together at the same version:

- `BrighterTools.ColourMath`
- `BrighterTools.MauiColourChooser`

This repository uses **MIT**, not MIT-0. Source version 0.9.1 is prepared for release; no package is published by cloning, building, CI, or running the packaging script.

## Local packaging

Use Windows with the pinned SDK and workloads to build the full Windows + Mac Catalyst package. Mac source builds target Mac Catalyst only; the release script rejects Mac hosts to prevent publishing a partial package.

```powershell
pwsh ./eng/bootstrap.ps1
dotnet test tests/BrighterTools.ColourMath.Tests/BrighterTools.ColourMath.Tests.csproj -c Release
pwsh ./eng/pack.ps1
# Optional override applied to BOTH packages:
pwsh ./eng/pack.ps1 -Version 0.9.1
```

Output is `artifacts/nuget/<version>/`, containing two .nupkg and two .snupkg files. The script checks package IDs, version, MIT metadata, README/license files, repository URL, symbols and expected target assemblies. Only `src` libraries are packable; demo/tests are excluded.

Use that folder as an additional local feed when testing consumption:

Add both sources to a NuGet.config beside the consumer project:

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="local-chooser" value="C:/path/to/artifacts/nuget/0.9.1" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Then run `dotnet add YourMauiApp.csproj package BrighterTools.MauiColourChooser --version 0.9.1`.

Keep nuget.org available for third-party dependencies. A project reference does not test the packaged dependency graph; validate a consumer using the actual packages before release.

## GitHub publishing workflow

`.github/workflows/publish-tool.yml` follows the BrighterTools manual publishing convention:

- `workflow_dispatch` only, from `main`.
- Optional `version` override (otherwise Directory.Build.props).
- `publish_to_nuget` defaults **false**, producing downloadable packages only.
- `production` GitHub environment.
- `NuGet/login` obtains a short-lived API key via OIDC only when publication is requested.
- The workflow pushes the maths package first, then the chooser; reruns skip already published versions.
- Source is checked out at the selected workflow revision. No package is rebuilt after obtaining the temporary key.

CI uses read-only permissions and cannot publish packages. Pull requests never run the publishing workflow.

## Setup the maintainer must complete

Create the GitHub `production` environment, restrict deployments to `main`, and configure any required reviewers. Set its variable `NUGET_USER` to the **NuGet account username** used for token exchange. This is not an email address, API key, or necessarily the GitHub organization name.

Create a NuGet.org Trusted Publishing policy with:

| Field | Value |
| --- | --- |
| Repository owner | `brightertools` |
| Repository | `BrighterTools.MauiColourChooser` |
| Workflow file | `publish-tool.yml` (filename only) |
| Environment | `production` |
| Package scope | Both `BrighterTools.ColourMath` and `BrighterTools.MauiColourChooser` |
| Permissions | Publish new packages for the first release, and new versions for later releases |

Use the NuGet user/organization that will own the packages and confirm the account has the appropriate publishing rights. Choose a package scope covering both IDs; the `BrighterTools.*` glob covers both. Limit the scope to these two IDs if your policy configuration supports that. No long-lived NuGet API key is required.

See [NuGet's official trusted-publishing setup](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) and [NuGet/login](https://github.com/NuGet/login) for current policy fields and token-exchange behaviour.

## Release checklist

1. Choose a version not already published; update Directory.Build.props and RELEASE_NOTES.md in a reviewed change.
2. Complete [testing.md](docs/testing.md), including real Mac checks for a Mac-supported release.
3. Merge to main and ensure CI succeeds.
4. Run `publish-tool` on main with publication disabled; inspect its packages.
5. Complete the environment and NuGet policies above.
6. Run the workflow on the same main revision with `publish_to_nuget=true`.
7. Confirm both package versions are visible and install into a clean consumer. Create a matching `v<version>` tag/GitHub release describing validation and known limits.

Do not overwrite an existing version. Package publication is separate from creating a Git tag; neither is automatic on push.
