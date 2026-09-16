# Contributing

Use GitHub issues for bugs and feature proposals. Include the package version, OS version, display scale, MAUI/SDK versions and a small reproduction. Discuss changes to colour semantics or public APIs before implementing them.

## Development

Install the SDK in global.json and run `pwsh ./eng/bootstrap.ps1`. The maths project needs only the SDK. See [testing.md](docs/testing.md) for commands and [usage.md](usage.md) for intended behaviour.

Keep platform-independent colour maths in BrighterTools.ColourMath. UI controls belong in BrighterTools.MauiColourChooser; host-specific capture, clipboard, history and persistence do not.

Preserve keyboard editing, incomplete-input state, hue through grey/black, theme handling and density-aware pointer mapping. Test behavioural changes with the maths tests and demo smoke checks; visually inspect layout changes.

Use focused branches and pull requests to main. Explain the problem, resulting behaviour and relevant validation. Document public API changes and update release notes. Do not commit credentials, signing files, build outputs or licensed host assets. Keep pinned dependency/lock-file updates intentional.

Contributions are accepted under this repository's standard MIT license. Treat contributors respectfully; review the code and ideas, not the person.
