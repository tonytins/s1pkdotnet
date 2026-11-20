# Sims1Pkg

Sims1Pkg (S1PK) is a one-file installer format for The Sims 1 custom content.

The Sims 2 and 3 let players install downloads by opening a single `.sims2pack`
or `.sims3pack` file. The Sims 1 never had that. Downloads arrive as loose zips,
and players copy each file by hand into `Downloads`, `GameData/Skins`,
`GameData/Walls`, and so on. An `.s1pk` file carries the content plus a manifest
that says what each file is, so the installer can put it in the right place, and
take it out again later.

> **Status:** the first release, Kodama, is feature-complete but not yet
> released. See the [roadmap](doc/roadmap.md) and the
> [design](doc/design/2026-11-19-installer-design.md).

## The format at a glance

An `.s1pk` file is a standard ZIP archive with a `manifest.xml` at its root. The
manifest lists every file, its kind, its size, and its SHA-256 hash. The kind
decides where the file is installed:

| Kind     | Typical files                   | Installed to      |
| -------- | ------------------------------- | ----------------- |
| `object` | `.iff`, `.far`                  | `Downloads`       |
| `skin`   | `.cmx`, `.skn`, `.bmp`          | `GameData/Skins`  |
| `wall`   | `.wll`                          | `GameData/Walls`  |
| `floor`  | `.flr`                          | `GameData/Floors` |

The folders are relative to the game folder of a Complete Collection CD install.
The [format specification](doc/spec.md) has the details, for anyone writing
their own tools.
Houses, Sims, and families aren't supported yet, since installing them means
editing the neighborhood rather than copying files.

## Using the app

Sims1Pkg is one window with a sidebar.

- **Install a package:** open an `.s1pk` (pass it to the app, or use
  *Open package…*), check where each file will go, and press *Install*. If a
  file is already in your game, Sims1Pkg asks before replacing it and keeps your
  copy, so uninstalling puts it back. Installing a newer version of an installed
  package updates it in place.
- **See and remove what's installed:** *Installed* lists every package with the
  files it added. *Uninstall* removes exactly those files and restores anything
  they replaced.
- **Make a package:** *New package…* takes a folder of content. Put files in
  `Downloads`, `GameData/Skins`, `GameData/Walls`, or `GameData/Floors` folders
  to say where they go, or let Sims1Pkg place them by type. Fill in the details,
  then *Save package…*.

Sims1Pkg finds the game through the `TS1_GAME_DIR` environment variable, the
folder you chose last time, or on Windows the Complete Collection's default
folder. Otherwise it asks. Installs keep their records in an `S1PK` folder inside
the game folder.

## Building

Sims1Pkg needs the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet build
dotnet test
dotnet run --project src/Sims1Pkg.Gui
```

## Project layout

| Path                     | What it is                                            |
| ------------------------ | ----------------------------------------------------- |
| `src/Sims1Pkg.Core`      | Library that reads, writes, and installs packages     |
| `src/Sims1Pkg.Gui`       | The Avalonia app                                      |
| `tests/Sims1Pkg.Core.Tests` | xUnit tests for the library                        |
| `tests/Sims1Pkg.Gui.Tests`  | xUnit tests for the app's logic and text           |
| `doc/roadmap.md`         | What's done and what's next                           |
| `doc/design/`            | Approved designs                                      |

## Contributing

Read [AGENTS.md](AGENTS.md) for the project's conventions and
[REVIEW.md](REVIEW.md) for what a change should be checked against. Facts about
how The Sims 1 lays out its files are the easiest thing to get wrong here, so
please mention the game version and install type you tested against.

## License

In jurisdictions that recognize copyright waivers, I've
[waived all copyright](UNLICENSE) and related or neighboring rights to this
project. Where these waivers aren't recognized, [BSD-3-Clause](COPYING) applies.

The app bundles [Comic Neue](https://github.com/crozynski/comicneue), copyright
The Comic Neue Project Authors, under the
[SIL Open Font License 1.1](src/Sims1Pkg.Gui/Assets/Fonts/OFL.txt).
