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

## RGB HEX input group — 17 September 2026

Added a shared themed input group with a fixed decorative hash prefix, six-digit editor, separator and focus border. Compact numeric fields keep their existing native styling. Valid pasted hash-prefixed values normalize to six uppercase digits without a duplicate prefix; incomplete input preserves existing validation behavior.

- Windows demo build succeeded without warnings/errors; all 50 existing runtime/rendering checks passed (updated the hex normalization and ancestor-layout assertions for the new wrapper).
- Integrated picker Windows build and 317 layout/runtime checks passed. Visually reviewed the joined input in light/dark Square layouts; the same group is used in Wheel and HSV modes.
- Integrated Mac managed Compile succeeded with no warnings/errors. Native Mac focus/keyboard/appearance still requires hardware acceptance.
- No public API or package version change; no NuGet publication performed.

## Optional alpha — 27 September 2026

Added opt-in `IsAlphaEnabled` for the Icon Editor app. Defaults to false; the opaque path, six-digit editor and its accessible description are unchanged.

- ColourMath tests: 44 passed (new RGBA hex parse/format and rejection cases; the six-digit parser still rejects eight digits).
- Windows demo build succeeded without warnings/errors; `--self-test` passed all 61 checks (11 new alpha checks covering default opacity, enabling after binding, RGBA hex, A validation, HSV percentage, ResetEditing with transparency, strip visibility, disabling, and unaffected opaque instances).
- Library managed Mac Catalyst Compile succeeded. Native Mac rendering/interaction of the opacity strip still requires hardware acceptance.
- No package version change; no NuGet publication performed.
