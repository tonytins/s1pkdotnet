# The S1PK format

This document defines format version 1 of `.s1pk`, a one-file package of The
Sims 1 custom content, for anyone writing a tool that makes, reads, or installs
packages. It describes what Sims1Pkg does today; where a rule is Sims1Pkg's own
choice rather than part of the format, it says so.

The words **must**, **must not**, **should**, and **may** are used in their
usual sense for specifications.

## The container

An `.s1pk` file is a ZIP archive.

- Its manifest **must** be at the root, named exactly `manifest.xml`.
- Every other file in the archive **must** be listed in the manifest, and every
  listed file **must** be in the archive. A reader **must** refuse an archive
  that holds an unlisted file, or lacks a listed one.
- Folder entries (names ending in `/`) are ignored.
- An entry name **must not** appear twice.
- Files may use any compression method ZIP readers commonly support. Sims1Pkg
  writes Deflate.

## The manifest

`manifest.xml` is UTF-8 XML. A DTD **must not** be used, and readers **should**
refuse one, since entity expansion can reach outside the document.

```xml
<?xml version="1.0" encoding="utf-8"?>
<S1Package formatVersion="1">
  <Id>maya.retro-diner</Id>
  <Name>Retro Diner Set</Name>
  <Author>Maya</Author>
  <Date>2001-03-14</Date>
  <Version>1.2</Version>
  <Description>Booths, a counter, and a checkered floor.</Description>
  <License>CC-BY-4.0</License>
  <SourceUrl>https://example.com/diner</SourceUrl>
  <Files>
    <File path="Downloads/Diner/DinerBooth.iff" kind="object" size="52114" sha256="…" />
    <File path="GameData/Floors/Checker.flr" kind="floor" size="8320" sha256="…" />
  </Files>
</S1Package>
```

### The root

`<S1Package>` **must** carry `formatVersion`, a whole number from 1. A reader
**must** refuse a version newer than the newest it supports, and should say that
a newer tool is needed. This document defines version 1.

### Details

Each of these elements appears at most once. A reader **must** refuse a manifest
with two of the same.

| Element       | Required | Meaning |
|---------------|----------|---------|
| `Id`          | no       | Names the package across its versions. See [Ids](#ids). |
| `Name`        | yes      | The name shown to players. Not blank. |
| `Author`      | yes      | Who made the content. Not blank. |
| `Date`        | yes      | When the package was made, written `YYYY-MM-DD`. |
| `Version`     | no       | The content's own version, as its author writes it. It isn't the format version, and it isn't compared beyond being equal or not. |
| `Description` | no       | A longer description. May hold line breaks. |
| `License`     | no       | The license the content is shared under, as free text. |
| `SourceUrl`   | no       | Where the content was published: an absolute `http` or `https` address. |

An empty optional element is the same as an absent one.

### Files

`<Files>` **must** appear once and hold at least one `<File>`. Each `<File>` has
four required attributes:

| Attribute | Meaning |
|-----------|---------|
| `path`    | The file's path in the archive. See [Paths](#paths). |
| `kind`    | What the file is. See [Kinds](#kinds). |
| `size`    | Its size in bytes, a whole number. It **must** match the archive entry's uncompressed size. |
| `sha256`  | The SHA-256 of its contents, as 64 hex digits. Readers accept either case; Sims1Pkg writes lowercase. |

Two files **must not** have paths that are equal when case is ignored, and no
file may be `manifest.xml`.

### Unknown content

So that later versions can add to the format, a reader **must** ignore elements
and attributes it doesn't know, including other children of `<Files>`. Anything
that would change what an older reader installs needs a new format version
instead.

## Paths

A path is relative, with folders separated by `/`. It **must not**:

- be empty, or start with `/`;
- have an empty folder name (`a//b`) or a name of `.` or `..`;
- hold `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`, or a control character;
- have a name that ends with a dot or a space, which Windows drops; or
- have a name Windows reserves for a device: `CON`, `PRN`, `AUX`, `NUL`,
  `COM1`–`COM9`, or `LPT1`–`LPT9`, with or without an extension.

These rules make every valid path safe to write on Windows, macOS, and Linux,
and keep it inside the folder it's written to.

## Ids

An id is 1 to 64 characters from `A`–`Z`, `a`–`z`, `0`–`9`, `.`, `_`, and `-`.
It **must not** start or end with `.`, and its part before the first `.` **must
not** be a name Windows reserves (see [Paths](#paths)).

Ids that differ only in case name the same package, so an installer **should**
compare them ignoring case. Sims1Pkg lowercases them.

When a package has no `Id`, Sims1Pkg makes one from the author and name:
accents are dropped, both are lowercased, any run of other characters becomes
one `-`, and the two are joined with `.`, such as `maya.retro-diner-set`. The
result is cut to 64 characters, and `-pkg` is added to a name Windows reserves. When
nothing usable is left, as with a name written only in Japanese, the id is
`pkg-` and the first 12 hex digits of the SHA-256 of the author, a line feed,
and the name, in UTF-8. Authors who want updates to be recognized **should**
set an `Id` and keep it for every version.

## Kinds

A file's kind says where it's installed. Format version 1 has four:

| Kind     | Typical files                             | Installed to, in the game folder |
|----------|-------------------------------------------|----------------------------------|
| `object` | `.iff`, `.far`                            | `Downloads/<id>/`, keeping the file's folders |
| `skin`   | `.cmx`, `.skn`, `.bcf`, `.bmf`, `.cfp`, and their `.bmp` textures | `GameData/Skins/` |
| `wall`   | `.wll`                                    | `GameData/Walls/` |
| `floor`  | `.flr`                                    | `GameData/Floors/` |

Kinds are matched ignoring case. A reader **must** keep a kind it doesn't know,
so the package can still be shown, but **must not** install that file, and
**should not** install the package at all.

### Where files go

The folders are those of The Sims Complete Collection, relative to the game
folder (the one holding `GameData` and `UserData`):

- **Objects** go in a folder of their own, `Downloads/<id>/`. A leading
  `Downloads/` in the file's path is dropped, and the rest of the path is kept,
  since the game reads subfolders of `Downloads`. So `Downloads/Diner/Booth.iff`
  in a package with id `maya.diner` goes to
  `Downloads/maya.diner/Diner/Booth.iff`.
- **Skins, walls, and floors** go straight into their `GameData` folder, by file
  name alone, since the game doesn't read subfolders there. Two such files with
  the same name can't be in one package.

An installer **should** match folder names to the ones already on disk ignoring
case, since a Wine install's folders may not use the casing above.

## Making a package

The format doesn't say how a tool decides each file's kind. Sims1Pkg's packer
works from a folder of content:

1. A file under one of the game's own folders takes that folder's kind:
   `Downloads/` gives `object`, and `GameData/Skins/`, `GameData/Walls/`, and
   `GameData/Floors/` give `skin`, `wall`, and `floor`. Folder names are matched
   ignoring case.
2. Any other file takes its kind from its extension, as in the table under
   [Kinds](#kinds). A `.bmp` outside a skins folder has no kind, since it could
   be anything.
3. A file neither rule places can't be packed.

It leaves out files whose names start with a dot, `Thumbs.db`, `desktop.ini`,
and other `.s1pk` files.

## How Sims1Pkg installs

This section isn't part of the format. It describes Sims1Pkg's own installer, so
other tools can avoid fighting it.

- Each installed package has a folder in the game folder, `S1PK/<id>/`, holding
  `receipt.xml` (what was written, with each file's SHA-256) and `backups/` (the
  player's files the install replaced).
- Installing a package whose id is already installed is an update, or a
  reinstall when the `Version` is the same. The old version's files are removed
  first.
- Uninstalling removes exactly the files in the receipt and puts the backups
  back.
- Every change is undone if the install, update, or uninstall fails partway.

## Version history

| Format version | Changes |
|----------------|---------|
| 1              | The first version. |
