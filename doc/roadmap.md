# Roadmap

Milestones are named after Shinkansen services, from the all-stops train to the
fastest.

## Kodama: command-line installer

The first usable release. Pack custom content into an `.s1pk`, then install and
uninstall it from the command line on a Complete Collection install.

- [x] Scaffolding: solution layout, format model, game layout, CLI commands
- [ ] Package format: read and write `manifest.xml`, validate it, `pack` and `inspect`.
      `pack` takes each file's kind from the source folder's layout (`Downloads/`,
      `GameData/Skins/`, and so on) and falls back to its extension.
- [ ] Install: find the game, route files by kind, check conflicts, write a receipt in
      the game folder, `install`, `uninstall`, and `list`
- [ ] Format specification in `doc/spec.md`

## Hikari: graphical installer

Open an `.s1pk` from a file manager, preview it, and install it with one click.

- [ ] Avalonia app that previews and installs a package, designed from scratch
- [ ] Interface text in `.cst` tables, read with [CST.NET](https://github.com/tonytins/cstdotnet)
- [ ] File association for `.s1pk`

## Nozomi: beyond copying files

Ideas that need more research before they're planned.

- Houses, Sims, and families, which need neighborhood editing rather than copying
- Reading `.iff` files to suggest each file's kind while packing
- Declaring which expansion packs a package needs
