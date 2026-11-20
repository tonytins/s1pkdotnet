# Sims1Pkg installer: design

> **Status:** approved 2026-11-19 · **Date:** 2026-11-19 · **Milestones:** Kodama (format, CLI install) and Hikari (GUI)

Sims1Pkg (S1PK) is a one-file installer for The Sims 1 custom content. This document says what the installer does,
why, and how it gets built, so the plan for Kodama and Hikari can be made from it.

## Why

- **The Sims 1 never had a `.sims2pack`.** The Sims 2 and 3 install downloads from a single file. The Sims 1 hands
  players loose zips, and they copy each file by hand into `Downloads`, `GameData/Skins`, `GameData/Walls`, and
  so on. Getting it wrong is easy and undoing it is guesswork.
- **For players, not just the author.** The installer is meant for anyone still playing a Complete Collection
  install, on Windows or Linux, so it follows each platform's conventions where players expect them.
- **It should feel like it belongs to the game.** The look borrows from the Maxis-era tools players remember.

Prior art: Sims2Pack Clean Installer (S2PCI) for the idea of a per-file manifest, routing files by type, and a record
of what was installed (ideas only; it's GPL); HomeCrafter, SimPaintShop, and RoofEdit for the look; VentureOS's
Roundhouse for the window structure and how to build an Avalonia title bar on every platform.

## Goals

1. Open an `.s1pk` by double-clicking it, see what it holds and where each file will go, and install it with one
   button.
2. See every installed package and uninstall any of them, leaving the game folder as it was before.
3. Never leave the game broken: check everything before writing, never write outside the game folder, and never
   leave a half-finished install.
4. Make packages from a folder of content with `s1pk pack`.
5. Run on Windows, Linux, and macOS, with the CLI and the GUI doing the same thing through one library.
6. Show interface text from `.cst` tables, so the app can be translated.

## Non-goals

- Houses, Sims, and families. Installing them means editing the neighborhood, not copying files. They're in Nozomi.
- The Legacy Collection's folder layout. The target is the Complete Collection from CD.
- Reading `.iff` files to work out what they are.
- Installing several packages at once.
- A dark look. The TS1 look is already dark navy.
- Downloading packages from anywhere.

## Anti-goals

The installer fails, even if it works, if:

- **It damages a game folder.** A stray file after a failed install, or a player's file deleted by an uninstall, even
  once.
- **It installs something it doesn't understand.** A file whose kind it doesn't know is never installed.
- **It becomes the only way to make packages.** An `.s1pk` stays a ZIP with a readable XML manifest that anyone can
  make by hand.

## Constraints

- **Layers.** `Sims1Pkg.Core` holds all package and install logic and has no UI. The CLI and the GUI only call it.
- **Dependencies:** Avalonia 12.1.5 (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`), the same version as
  Roundhouse, for the GUI only, and CST.NET 2.1.200 (`CSTNet`) for interface text. Comic Neue is bundled as a font
  asset under the SIL Open Font License. GitInfo 3.6.1 sets the version, as in the author's other projects: a base in
  `GitInfo.txt` plus the commits since it changed, loosely following the .NET SDK's `major.minor.patch`, where the
  hundreds digit of the patch is the feature band. Anything else needs approval. Each dependency lands in its own
  commit.
- **Game facts** checked against the author's Complete Collection install (French) at `TS1_GAME_DIR`: `Downloads`,
  `GameData/Skins`, `GameData/Walls`, and `GameData/Floors` exist with that casing, and `Downloads` holds one
  subfolder per object.
- **Verification:** xUnit tests for Core and for the GUI's plain classes. Screenshots of the real app come from the
  Ubuntu distrobox's Xvfb display, with `XDG_CURRENT_DESKTOP` set so the merged title bar shows (see the title bar
  section). Window manager behavior needs a check on a real desktop.
- **License:** Unlicense with BSD-3-Clause fallback. VentureOS is the author's own Unlicense code and may be adapted.
  Nothing comes from S2PCI's code.

## Approaches

The format and install behavior were settled in conversation; the choice was how to build the GUI.

| Approach | What it trades | |
|---|---|---|
| **Code-behind, like Roundhouse:** layout in AXAML, wiring in code-behind, testable logic in plain classes, TS1 look as `ControlTheme`s over Fluent | Least code, and the Linux title bar is already proven in Roundhouse; code-behind could get messy if the app grew a lot | **Chosen** |
| MVVM with CommunityToolkit.Mvvm | More testable without a window, but an extra dependency and ceremony for two screens, and a second style to maintain | |
| Custom-drawn TS1 controls | Closest to the old hand-painted tools, but rebuilds focus, keyboard, and accessibility that Fluent gives for free | |

Code-behind was chosen because it's the smallest thing that works, and it matches the author's other Avalonia app.

## Decisions made

### The package

- An `.s1pk` is a ZIP archive with `manifest.xml` at its root.
- The manifest's root element carries `formatVersion="1"`. Readers refuse a version newer than they support, with a
  message saying a newer Sims1Pkg is needed.
- It holds the package's name, author, and date (required), and its id, version, description, license, and source
  URL (optional), plus one entry per content file: its path in the archive, its kind, its size, and its SHA-256.
- The **id** names the package across versions, so the installer can tell an update from a different package. The
  author sets it once and keeps it for every version. Without one, the id is the name and author together. `s1pk
  pack` takes `--id`, and a package's id is shown by `inspect` and in the GUI.
- Kinds in format 1: `object`, `skin`, `wall`, `floor`. A file with an unknown kind is listed by `inspect` and the GUI,
  marked as unknown, and blocks installing the package.
- Unknown optional elements in the manifest are ignored.

### Packing

- `s1pk pack <folder> <package.s1pk>` takes each file's kind from where it sits in the source folder, using the game's
  own folder names: `Downloads/` gives `object`, `GameData/Skins/` gives `skin`, `GameData/Walls/` gives `wall`, and
  `GameData/Floors/` gives `floor`, matched without regard to case.
- Files outside those folders fall back to their extension: `.iff` and `.far` give `object`, `.wll` gives `wall`,
  `.flr` gives `floor`, and `.cmx`, `.skn`, `.bcf`, `.bmf`, and `.cfp` give `skin`.
- A file neither rule places, such as a loose `.bmp`, stops packing with a message naming the file and the folders
  that would place it.

### Finding the game

- In order: `TS1_GAME_DIR`; on Windows, `C:\Program Files (x86)\Maxis\The Sims Complete Collection`; then the folder
  the player chose last time.
- If none is a game folder, the GUI asks the player to pick one once and remembers it. The CLI takes `--game <folder>`
  and otherwise stops with a message naming `TS1_GAME_DIR`.
- The game shown at the bottom of the sidebar can be clicked to choose another.

### Installing

- Everything is checked first: the manifest, every hash, every destination path, every kind, and every clash. Nothing
  is written until all of it passes.
- Objects go in `Downloads/<id>/`, matching the one-folder-per-object layout already in the game. Skins, walls,
  and floors go straight into their `GameData` folder, where the game reads them.
- **Clashes** with files already in the game get one popup listing all of them, with *Cancel* and *Replace*. On
  *Replace*, the current copies are moved into the package's receipt folder as backups. The CLI asks the same
  question in the terminal, and takes `--replace` to answer yes without asking.
- **Updates:** a package whose id is already installed is an update. The popup says "Update Retro Diner Set from 1.2
  to 1.3?" Accepting removes the old version's files as an uninstall would, then installs the new one, as one
  operation: if any step fails, the old version is left installed as it was. Backups the old version made carry over
  to the new receipt, so uninstalling the new version still puts the player's original files back. Installing the
  same version again is offered as a reinstall, in the same way.
- **Windows permissions:** when the game folder isn't writable, as under `Program Files`, the GUI asks for
  administrator rights with Windows' usual prompt and installs with them. The CLI stops with a message to run it from
  an administrator terminal. Sims1Pkg doesn't write to Windows' `VirtualStore` folder.
- Progress shows in the main area, ending on a short summary of what went where.
- A failure or cancel partway through puts back what was already written, including any backups, so the game folder
  ends as it began.

### Receipts

- Each installed package gets a receipt folder in the game folder: `S1PK/<id>/`, holding `receipt.xml` and a
  `backups/` folder.
- The receipt lists every file written, with its SHA-256 at install time, and every file backed up.
- Uninstalling removes exactly the files in the receipt and puts the backups back. If a file was changed or deleted
  since install, a popup says so before anything is removed.

### The window

One window: a VentureOS structure in a HomeCrafter paint job.

```
┌────────────────────────── Sims1Pkg ──────────────── – □ ✕ ┐
│ ▌Diner Set      ▐│ Retro Diner Set                         │
│  Installed (12)  │ by Maya · 2001-03-14 · v1.2             │
│  Open package…   │ ┌ Contents ──────────────────────────┐  │
│                  │ │ DinerBooth.iff            Object   │  │
│                  │ │ CheckerFloor.flr          Floor    │  │
│                  │ └────────────────────────────────────┘  │
│ The Sims         │                     [ Cancel ][Install] │
│ Complete Coll.   │                                         │
└──────────────────┴─────────────────────────────────────────┘
```

- **Title bar:** navy, with the title centered. It's the window's own title bar on every platform, adapted from
  Roundhouse's `MergeTitleBar` and `TitleBars`. Windows and macOS keep their system buttons beside it. On Linux,
  Avalonia draws the frame (`X11PlatformOptions.EnableDrawnDecorations`) and Sims1Pkg draws Windows-style minimize,
  maximize, and close buttons on the right. Where Roundhouse falls back to the window manager's title bar (tiling
  window managers, WSL, no `XDG_CURRENT_DESKTOP`), so does Sims1Pkg.
- **Sidebar:** a blue gradient with a white pill for the selected item. It lists the opened package (when there is
  one), *Installed (n)*, and *Open package…*, with the game folder at the bottom.
- **Main area:** darker navy, with white-framed group boxes whose labels are cut into the frame, like RoofEdit's. Pale,
  bevelled buttons with navy text.
- **Text:** Comic Neue throughout.
- **Opening a package** by double-clicking it, or as the app's first argument, starts the app with that package
  selected. *Open package…* opens a file picker filtered to `.s1pk`.
- **Installed** lists each package; selecting one shows its files, where they went, and an *Uninstall* button.
- **Resizing:** the window resizes and maximizes; macOS shows all three traffic lights.

### Interface text

- Every string the GUI shows comes from a `.cst` table shipped beside the app. English is the base layer, and a
  translation is laid over it with CST.NET's `CSTLayers`, so a missing translated string falls back to English.
- A string missing from every table shows its key, and a test fails for any key the code uses that the English table
  lacks.

## Left to the builder

- Names and layout of classes, files, and the `.cst` table and its keys.
- How an id is turned into a safe folder name, and what characters an id may hold.
- How the GUI restarts itself with administrator rights on Windows.
- Exact colors, gradients, spacing, and font sizes, judged against the screenshots in `stash.local/screenshots`.
- Where the GUI remembers the chosen game folder.
- How the receipt's XML is laid out.
- How the GUI registers itself to open `.s1pk` files on each platform.
