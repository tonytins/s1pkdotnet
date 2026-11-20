# Sims1Pkg roadmap

> **Last updated:** 2026-11-19 · **Latest release:** none yet · **Next:** 0.1, Kodama, in development

Where Sims1Pkg is going: the release in development, the ones after it, and
ideas not yet given a release. What each release changes for players is in the
[changelog](../changelog.md). The [installer design](design/2026-11-19-installer-design.md)
describes the app 0.1 starts from.

## How Sims1Pkg releases

Sims1Pkg is a small app, so it's planned in releases, and each release in
feature bands.

- **No schedule.** A release ships when its bands land, not on a date.
- **Versions** loosely follow the .NET SDK's feature bands: `0.1` is the
  release, each band in it takes a hundred (`0.1.1xx`, `0.1.2xx`, ...), and
  the last two digits count commits within the band. A build's version comes
  from [`GitInfo.txt`](../GitInfo.txt) plus every commit since it changed.
  `GitInfo.txt` moves to a band's base (`0.1.200`) when work on that band
  begins. A new release (`0.2.100`) waits for the owner's word.
- **Stages.** A release is *considered* (a theme and candidate features),
  then *in development* (bands with checklists), then *released*: tagged,
  with its notes in the changelog.
- **Names.** Each release has a code name, after a Shinkansen service, from
  the all-stops Kodama up to the fastest. Bands get plain names. Nozomi is
  saved for a later release.
- **Format versions** are separate from app versions. A release changes
  `formatVersion` only when older readers can't safely read the new data, and
  says so here and in [the spec](spec.md).

Keep it current: tick a band's items as they land, move a release from one
stage to the next, and add a line to the revision history.

## Releases

| Release | Code name | Stage | Holds |
|---|---|---|---|
| 0.1 | Kodama | in development | The installer app, the format spec, and packages for each platform; then loose skin textures and an About page |
| 0.2 | Hikari | considered | Not yet chosen |

## 0.1, Kodama

The first release: one Avalonia window that installs, manages, and makes
`.s1pk` packages on a Complete Collection install, for Linux, Windows, and
macOS.

**0.1.1xx, the installer app**

- [x] Scaffolding: solution layout, format model, game layout.
- [x] Package format: the manifest with format version and package id,
  reading and verifying packages, and packing a folder by its layout with an
  extension fallback.
- [x] Install: find the game, route files by kind, back up clashes, update
  in place, keep receipts in the game folder, uninstall, and list.
- [x] The app: a TS1-styled window with a Roundhouse-style title bar,
  interface text in `.cst` tables read with
  [CST.NET](https://github.com/tonytins/cstdotnet), install, manage, and pack.
- [x] Format specification in [`doc/spec.md`](spec.md).
- [x] Packages for Linux (AppImage and tar.gz), Windows (zip), and macOS
  (app), from [packaging/](../packaging/README.md).
- [x] Open `.s1pk` files from the file manager on Linux (through the desktop
  entry and MIME type) and macOS (through `Info.plist`). On Windows, use
  *Open with* for now.

**0.1.2xx, skins and About**

- [ ] Loose `.bmp` files pack as skins, since skins need their textures. A
  `.bmp` anywhere outside the game's own folders takes `skin` by its
  extension; the pack preview shows where each one will go. `skin` is
  already a kind, so `formatVersion` stays 1 and older readers install these
  packages as they are.
- [ ] An About page in the sidebar: the version, the license, a link to the
  repository, and credits for Avalonia, CST.NET, and Comic Neue, with its
  text in the `.cst` tables.

## 0.2, Hikari

Considered. Its theme isn't chosen yet; candidates come from Ideas.

## Ideas

Worth doing someday, in no order, and not yet given a release. Most need more
research before they're planned.

- **Houses, Sims, and families,** which need neighborhood editing rather than
  copying files.
- **Reading `.iff` files** to suggest each file's kind while packing.
- **Declaring which expansion packs** a package needs.
- **Registering `.s1pk` on Windows,** which needs an installer.
- **The Legacy Collection's** folder layout.

## Risks

- A creator's folder that holds a `.bmp` that isn't a skin texture, such as a
  preview picture or a readme image, packs it as a skin once 0.1.2xx lands.
  The pack preview shows its destination, so it's caught there or not at all.
- Only the Complete Collection from CD has been checked; a Wine install may
  use different casing than `GameInstall` expects.
- Windows and macOS builds can't be run from Linux, so they're checked less
  than Linux builds.

## Revision history

| Date | Change | Summary |
|---|---|---|
| 2026-11-19 | major | The roadmap is retooled around feature bands, after VentureOS's: releases with code names, bands with checklists, and an Ideas list. Kodama and Hikari's finished work becomes 0.1.1xx. 0.1.2xx, skins and About, is next. Hikari is kept for 0.2, and Nozomi is saved for a later release; its ideas move to Ideas, with registering `.s1pk` on Windows. |
