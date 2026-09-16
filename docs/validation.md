# Public project preparation - 16 September 2026

Prepared version 0.9.1 under standard MIT. No NuGet publication has been performed.

Local Windows validation:

- 33 portable colour-maths tests passed.
- Windows demonstration app built with no warnings/errors.
- 50 source-referenced demo runtime/rendering checks passed.
- Mac managed demo compilation passed with no warnings/errors.
- Both NuGet packages and symbol packages built; archive verification checked IDs/version, MIT license metadata, repository link, README/LICENSE/notices and target assemblies.
- A separate demo restored both BrighterTools dependencies as NuGet packages (no source project references), built, and passed the same 50 runtime/rendering checks.
- GitHub workflow syntax passed actionlint 1.7.12. Actions are pinned to verified commit IDs.
- Staged-file review excluded build outputs, credentials, signing materials and host font/icon assets.

The GitHub-hosted CI run is a separate check after upload. Actual Mac interaction, Retina behaviour and final release acceptance remain outstanding. NuGet trusted-publishing policies, the production environment and NUGET_USER must be configured by the maintainer before publication.
