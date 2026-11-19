# Review Guide

Use this checklist when reviewing a change to Sims1Pkg. It is adapted from the
contributor and review practices in [NVIDIA FLARE's contribution guide](https://github.com/NVIDIA/NVFlare/blob/main/CONTRIBUTING.md),
by way of [Hudini](https://github.com/tonytins/Hudini) and
[CST.NET](https://github.com/tonytins/cstdotnet), with checks tailored to installing
Sims 1 content. The rules it checks against are in [AGENTS.md](AGENTS.md).

## Before Reviewing

- Read the change description and the relevant code, tests, and docs.
- Identify the behavior players or callers would notice that the change is meant to
  address. If it refers to a roadmap item, an issue, or a design discussion, check that
  the change addresses that context.
- Check the available build and test results. Run `dotnet build` and `dotnet test` when
  the change or its verification warrants it. For changes to packing or installing, run
  `s1pk` against a real package and a game folder. Report checks that couldn't be run,
  such as Windows from Linux.

## Review Checklist

- Does the behavior match the stated intent, including boundary cases such as an empty
  package, a missing or malformed manifest, mixed-case file names, nested folders, and
  files with the same name?
- **Game folder safety:** Could the change write outside the game folder, overwrite a file
  without asking, or leave files behind after a failed or cancelled install? Are
  conflicts found before anything is written?
- **Receipts:** Is every file the installer writes recorded, and does uninstalling remove
  exactly those files?
- **Game facts:** Do new folder names, extensions, or registry keys come from a real
  install or a cited source, and does the change say which? Do paths still resolve when
  the install's folder casing differs, as under Wine?
- **Format:** Does an older reader refuse or safely ignore the new data? Does an unknown
  content kind stay uninstallable? Are the docs for the manifest updated with it?
- **Layers:** Does `Sims1Pkg.Core` stay free of `Console`, dialogs, and other UI?
- **Public API:** Are names, defaults, and behavior clear and consistent with the existing
  APIs? Could the change break existing callers? Is every public member documented with
  XML comments, and are `null` arguments rejected?
- **Platforms:** Does the change need a newer runtime than .NET 10, or behave differently
  on Windows, such as with path separators or case-insensitive file names?
- **Dependencies:** Are new packages or version bumps approved, called out, and in a
  change of their own?
- **Licensing:** Is anything copied or ported from GPL sources such as Sims2Pack Clean
  Installer?
- **Tests:** Do focused tests cover the changed behavior, and would a new test fail
  without the fix? Was the tool run beyond the tests?
- **Docs:** Are the README, the roadmap, and `AGENTS.md` accurate after the change?

## Writing Review Comments

- Report concrete correctness, compatibility, reliability, or missing-test concerns. Don't
  block changes on personal style preferences.
- Make each comment specific, constructive, and actionable. Point to the smallest relevant
  location and explain the impact and a useful next step.
- Prioritize findings by impact:
  - **Blocking**: prevents the project from building, writes outside the game folder, or
    leaves a game folder broken in normal use.
  - **High**: overwrites or deletes a player's files without asking, installs a file with
    an unknown kind, or breaks an existing public behavior.
  - **Medium**: causes a limited but meaningful behavior or reliability issue, or leaves
    docs describing something that isn't true.
  - **Low**: a small edge case or non-blocking improvement.
- If no actionable issues remain, say so and note any important checks that were not run.
