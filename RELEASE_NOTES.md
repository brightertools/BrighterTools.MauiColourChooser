# Release notes

## Unreleased

- Added optional alpha: `IsAlphaEnabled` (default false), an opacity strip with checkerboard, an A editor and eight-digit RRGGBBAA hex. Opaque behaviour and the six-digit editor are unchanged when disabled.
- `ColourMath.TryHexWithAlpha` and `RgbColour.HexWithAlpha` for portable RGBA hex parsing and formatting.

## 0.9.1 - public release candidate (not yet published)

- Prepared both packages for public distribution under standard MIT.
- Added source/repository metadata, package README/license, XML documentation output and symbol packages.
- Added standalone build instructions, usage documentation and CI/trusted-publishing workflows.
- Wheel and square selection with RGB/HSV/hex editing and input validation.
- Compact/comfortable layouts, expandable editors and host-owned icon support.
- Brightness-adjusted wheel with documented new-colour reset behaviour.
- Retained density-aware pointer mapping, hue through greys/black and independent instances.
- Reduced the compact Brightness label-to-slider gap.

Windows runtime checks and managed Mac compilation are available. Complete real-Mac interaction and display-density acceptance before declaring native Mac parity.

## Earlier local iterations

Versions 0.1.0-0.9.0 were local development packages. They were not published by this project setup. The public history starts with the MIT-licensed source preparation.
