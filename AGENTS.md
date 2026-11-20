# Project Guidelines

## Project Structure

Sims1Pkg (S1PK) is a one-file installer format for The Sims 1 (TS1) custom content, in
the spirit of `.sims2pack`. A package is a ZIP archive with a `manifest.xml` at its root
that lists each file's kind, size, and SHA-256 hash. The installer uses the kind to put
each file in the right game folder, and records what it installed so it can be removed
later.

- **`Sims1Pkg.Core`** is the library. It owns the format (`PackageFormat`,
  `PackageManifest`, `PackageFile`, `ContentKind`) and the game's folder layout
  (`GameInstall`). It must stay free of `Console`, dialogs, and other UI, so the GUI and other
  programs can both use it. It reports results through return values and
  exceptions.
- **The GUI** (`Sims1Pkg.Gui`, coming in Kodama) is the only front end: an Avalonia app that installs, manages, and
  makes packages. Sims1Pkg has no command-line tool, and won't get one in .NET.
- **`Sims1Pkg.Core.Tests`** covers Core with xUnit v3.

[doc/roadmap.md](doc/roadmap.md) lists the named milestones. Never commit build output
(`bin/`, `obj/`, `*.nupkg`).

## Build and Test

```sh
dotnet build   # warnings are errors
dotnet test    # all tests must pass
```

- Everything targets .NET 10, set in [Directory.Build.props](Directory.Build.props).
- The version comes from [GitInfo](https://github.com/devlooped/GitInfo): the base in
  [GitInfo.txt](GitInfo.txt) plus every commit since it changed, loosely following the .NET SDK's
  `major.minor.patch`. The hundreds digit of the patch is the feature band. Start a new band for a new feature; a
  new major or minor version needs the owner's word.
- Passing tests alone don't show that a package installs. Until the GUI exists, also call Core from a small scratch
  project outside the repository against a real package. Once it does, run the app, and take screenshots on Xvfb
  as the visual checks rule describes.
- Report what you could not run locally, such as Windows from Linux.

## C# and S1PK

- Follow the [.NET guidelines skill](.agents/skills/dotnet-guidelines/SKILL.md) for
  general conventions, including SDK choices, language versions, and platform-specific
  APIs. Where it disagrees with this file, this file wins.
- Match the project's existing naming and formatting. The C# version follows the target
  framework; don't set `LangVersion`.
- Keep public APIs clear at the call site, and document all public behavior with XML
  comments.

## Golden Rules

### Design Process

- **Iterate**. A first design or name is a draft. Rebuild clumsy parts as understanding
  improves.
- **Ask when a decision is open**, and explain the reasoning behind what you suggest.
- **Keep it simple**. The simplest approach that matches how the game actually loads
  content wins.
- **Say what you're unsure of**: assumptions, unverified behavior, and what needs testing.

### Never Leave the Game Broken (Outranks Features)

Players run the installer against a game folder they care about, so a failed install
that leaves stray files is worse than one that refuses to start.

- **Check before writing.** Find every conflict and every unsafe path before the first
  file is written.
- **Never write outside the game folder.** Reject archive paths that escape it, including
  `..` segments, absolute paths, and drive letters.
- **Leave no half-finished install.** If an install fails or is cancelled, the game
  folder ends up as it was.
- **Record everything installed.** Every file the installer writes goes in the package's
  receipt, so uninstalling removes exactly that and nothing else.
- **Never install what you don't understand.** A file whose kind the reader doesn't
  know may be listed, but never installed.
- **Add a focused regression test** for every install, uninstall, or malformed-package
  fix.

### Game Facts

- **Facts about TS1 need a source.** Folder names, file extensions, and registry keys must
  come from a real install or a cited reference. Say what they were checked against.
- **The target install is the Complete Collection from CD**, including under Wine. The
  Legacy Collection isn't a target yet.
- **Paths are case-sensitive on Linux.** A Wine install may not use the casing written in
  `GameInstall`.
- **Don't commit files from a game install.**

### Format Rules

- **Readers refuse a `FormatVersion` newer than `PackageFormat.CurrentVersion`.** Bump it
  only when older readers can't safely read the new data.
- **Prefer additions an older reader can safely ignore**, and say what an older reader
  does with each one.
- **Change the docs along with the format.** The manifest and its rules live in the
  roadmap until `doc/spec.md` is written.

### Reference Material and Licensing

S1PK is released under the Unlicense, with BSD-3-Clause as the fallback. Contributions
keep that licensing.

`stash.local/` is git-ignored personal material. `stash.local/Sims2Pack` holds the
source of Sims2Pack Clean Installer, which is GPL-2.0-or-later. Use it for ideas only, and
never copy or port its code.

## Keeping Docs Current

Describe only what works today.

- [README.md](README.md) when features, prerequisites, or setup change.
- [doc/roadmap.md](doc/roadmap.md) when a milestone item lands or the plan changes.
- This file when the layout or rules change.

State breaking changes, dependency bumps, and anything you couldn't verify in the summary
or commit message. If you notice a risk outside your task, report it rather than fixing it
quietly.

See [REVIEW.md](REVIEW.md) for the project's review checklist.

## Personal Configuration

Keep personal preferences and machine-specific guidance in git-ignored `*.local` files or
directories (such as `AGENTS.local.md`), rather than in shared project instructions. Check
for relevant local guidance before environment-specific work. You can create or update
these files for details such as:

- Coding preferences
- Local build and development environment
- Personal shortcuts or workflows
