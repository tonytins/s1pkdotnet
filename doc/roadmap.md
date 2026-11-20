# Roadmap

Milestones are named after Shinkansen services, from the all-stops train to the
fastest. The [installer design](design/2026-11-19-installer-design.md) describes
Kodama in full.

## Kodama: the installer app

The first usable release: one Avalonia window that installs, manages, and makes
`.s1pk` packages on a Complete Collection install.

- [x] Scaffolding: solution layout, format model, game layout
- [x] Package format: the manifest with format version and package id, reading and
      verifying packages, and packing a folder by its layout with an extension fallback
- [ ] Install: find the game, route files by kind, back up clashes, update in place,
      keep receipts in the game folder, uninstall, and list
- [ ] The app: a TS1-styled window with a Roundhouse-style title bar, interface text
      in `.cst` tables read with [CST.NET](https://github.com/tonytins/cstdotnet),
      install, manage, and pack

## Hikari: getting it to players

- [ ] Format specification in `doc/spec.md`
- [ ] Open `.s1pk` files from the file manager on Linux and Windows

## Nozomi: beyond copying files

Ideas that need more research before they're planned.

- Houses, Sims, and families, which need neighborhood editing rather than copying
- Reading `.iff` files to suggest each file's kind while packing
- Declaring which expansion packs a package needs
