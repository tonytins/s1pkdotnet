# Change Log

## Kodama (0.1)

The first release: a one-window app that installs, manages, and makes `.s1pk`
packages for The Sims Complete Collection, on Windows, Linux, and macOS.

- Open an `.s1pk`, see what each file is and where it goes, and install it.
  Files already in the game are replaced only when you agree, and kept so
  uninstalling puts them back.
- Installing a newer version of an installed package updates it in place.
- See every installed package, and uninstall any of them. Sims1Pkg warns first
  if you've changed one of its files since.
- Make a package from a folder of content, with a preview of where each file
  will go.
- Every install, update, and uninstall is undone if it fails partway.
- Interface text comes from `.cst` tables, so it can be translated.
- Skin textures (`.bmp`) are packed as skins wherever they sit in the
  folder, not only under `GameData/Skins`.
- [Format version 1](doc/spec.md) of `.s1pk`.
