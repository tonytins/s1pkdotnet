# Packaging

Scripts that build Sims1Pkg into the packages people download, one folder for
each platform, adapted from VentureOS's Roundhouse (which took them from
CityLimits). Every script runs on Linux or macOS with the .NET 10 SDK, since
.NET publishes for every platform from any of them, and writes to
`dist/release/`, which git ignores.

| Script | Builds |
|---|---|
| `linux/package.sh` | `sims1pkg-<version>-linux-x86_64.AppImage` and `.tar.gz` |
| `windows/package.sh` | `sims1pkg-<version>-windows-x64.zip`, Sims1Pkg in a folder |
| `macos/package.sh` | `sims1pkg-<version>-macos-arm64.zip` and `-macos-x64.zip`, each holding `Sims1Pkg.app` |

```sh
packaging/linux/package.sh
packaging/windows/package.sh
packaging/macos/package.sh
```

Each takes `--framework-dependent` for a smaller package that needs the .NET 10
runtime installed, and `--help`. Without it, the package carries the runtime and
needs nothing installed.

## What they share

`env.sh` sets up every script:

- It reads the version once, from GitInfo, the same major.minor.patch
  `Sims1Pkg --version` prints.
- It publishes Sims1Pkg without debug symbols and checks its interface text came
  with it, in `uitext/`, and Comic Neue's licence, in `licenses/`. The font itself
  is built into the app.
- It copies the readme, both licences, and the changelog in.
- It runs the packaged Sims1Pkg with no window: `Sims1Pkg --version` prints the
  version and fails if the interface text is missing. The display is unset first,
  so it never opens a window.
- It downloads tools only at pinned versions, refusing a file whose SHA-256 isn't
  the one published for it.

| Variable | Default | Meaning |
|---|---|---|
| `SIMS1PKG_VERSION` | GitInfo's | The version the packages are named for |
| `DIST` | `dist/release` | Where the packages go |
| `WORK` | `dist/work` | Where they are put together, and the downloaded tools kept |

## The icon

Sims1Pkg's icon is a placeholder until it has its own art: "S1" in Comic Neue on
the title bar's navy. Its one source is `src/Sims1Pkg.Gui/Assets/Sims1Pkg.png`,
square and at least 512 pixels. Replace it, then run `packaging/icons.sh`, which
needs ImageMagick 7 and python3, to remake the Windows `.ico` (in the executable)
and the macOS `.icns`, and commit all three. The Linux packages and the window
use the PNG itself.

## Opening packages from a file manager

- **Linux:** the AppImage carries `Sims1Pkg.desktop` with
  `MimeType=application/x-sims1pkg` and the type itself in
  `usr/share/mime/packages/sims1pkg.xml`. Desktop integration tools, such as
  AppImageLauncher or appimaged, register both. The tar.gz carries the same two
  files and the icon at its top, for installing by hand:

  ```sh
  xdg-mime install sims1pkg-mime.xml
  xdg-icon-resource install --size 512 Sims1Pkg.png Sims1Pkg
  desktop-file-install --dir ~/.local/share/applications Sims1Pkg.desktop
  ```

  `Exec=Sims1Pkg` assumes the folder is on your `PATH`; edit it to the full path
  otherwise.
- **macOS:** `Info.plist` declares `.s1pk` and makes Sims1Pkg its owner, so Finder
  opens packages with it once the app has been opened once.
- **Windows:** the zip has no installer, so nothing registers the type. Choose
  *Open with* on an `.s1pk`, pick `Sims1Pkg.exe`, and tick *Always*.

## Linux

The AppImage carries its own runtime, so it runs without FUSE. It is built with
appimagetool 1.9.1 and the type 2 runtime from 2025-11-08, Roundhouse's and
CityLimits' pins, downloaded on the first run. `AppRun` starts Sims1Pkg from
inside the image without changing the folder, so a package's path on the command
line still means what it did. The script runs the AppImage itself, as well as the
folder inside it.

## Windows

`Sims1Pkg.exe` carries the icon. The zip unzips anywhere. The script checks the
executable is built for x64, but doesn't run it.

## macOS

`Sims1Pkg.app` holds the published Sims1Pkg in `Contents/MacOS`, the icon in
`Contents/Resources/Sims1Pkg.icns`, and `Info.plist`, from `Info.plist.in`. It
needs macOS 14 or later, as .NET 10 does.

The app is signed ad hoc, not with a Developer ID, so Gatekeeper asks before a
downloaded copy first opens: Control-click the app and choose Open. On a Mac, the
script signs the whole bundle with `codesign`; elsewhere, .NET has already signed
the executable as it published it, which Apple silicon needs before it runs
anything. Signing with a Developer ID and notarizing need a Mac and an Apple
developer account.
