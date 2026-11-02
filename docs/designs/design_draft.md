# FastSearch governance and existing-product quality

- Author: Rich Crane / MILL5
- Date: 2026-11-02
- Status: Proposed; undergoing independent Goldfish review

## Overview

FastSearch is a .NET 10 library for case-insensitive substring search over
in-memory objects. Six algorithms implement `ISearch<T>`; a command-line
benchmark measures indexing/search costs, a .NET MAUI desktop sample lets
people compare algorithms, and a static website explains installation and tradeoffs.
This work installs the Google Elephant-Goldfish Model (EGM) design/review skills
and Impeccable interface-review skill, enforces branch promotion in GitHub,
and repairs issues found by independent design and interface reviews.

## The Problem

At initial inspection GitHub had `main` and `develop`, with neither protected.
PR-only rulesets were enabled during setup; final required-check enforcement
remains a rollout checkpoint. The prior
repository guidance uses release branches for development. The requested policy
replaces that with feature branches. Repository settings alone cannot express
which source branch may open a mergeable PR, so a required trusted check must
validate source identity. At initial inspection, the existing website and desktop sample had no
checked-in product context or extracted design-system documentation.

## Goals

1. Only same-repository `develop` PRs can merge to `main`.
2. Only nonempty same-repository `feature/*` PRs can merge to `develop`.
3. Both branches require a PR and resolved conversations, reject force pushes
   and deletion, and have no bypass actors, including administrators.
4. Automatic GitHub Copilot cloud reviews target `develop` only and run again
   on real code pushes. Do not manually request reviews.
5. Record the existing architecture, contracts, interface tokens, and review
   findings; implement bounded, evidence-backed corrections.
6. Keep library compatibility, recorded benchmark measurements, and existing
   website identity intact; validate with tests and rendered inspection.

## Non-Goals

No new search algorithm, API break, benchmark rerun, rebrand, paid infrastructure,
NuGet release, version bump, or broad refactor. Installing review skills does not
prove that the library is Unicode-correct or that every interface is accessible.

## Current Architecture and Contracts

- `FastSearch/ISearch.cs`: `ICollection<T> Search(string? search)` for class T.
- `FastSearch/Algorithms/`: StringSearch is the linear oracle; LinqSearch,
  HashSearch, CharSequenceSearch, SuffixArraySearch, and NGramSearch share the
  interface. Existing tests cover oracle consistency, empty queries, contains
  semantics, index collisions, suffix arrays, and n-grams.
- `PasswordSearch.Benchmark/`: indexing/search benchmarks and a separate heap
  harness. Fully optimized heap comparisons use one algorithm per process and
  `DOTNET_TieredCompilation=0`.
- `PasswordSearch/`: MAUI Mac Catalyst/Windows app. `MainViewModel` loads the
  packaged password dataset, constructs six indexes, measures the selected
  algorithm, and validates its count outside the timed loop. Busy state gates
  commands and a status label reports completion/failure. Dataset/index work
  runs in a background task; observable UI updates must reach the UI thread.
- `data/` and `Directory.Build.props`: one linked data source, selected by
  `<UsesPasswordData>true</UsesPasswordData>`.
- `docs/index.html`: a single static page with inline CSS, metadata, structured
  data, algorithm comparisons, code samples, benchmark tables/charts, FAQ, and
  author attribution. There is no JavaScript application or backend.
- `.github/workflows/ci.yml`: Linux build/test/package and macOS MAUI build.
  `release.yml` publishes existing version tags only after ancestry/version checks.

## Critical User Journeys

### Contributor promotion

Given a feature branch in MILL5/FastSearch, when its PR targets develop,
then source validation and CI run, automatic Copilot review is scoped to that
base, and unresolved review conversations block merge. A wrong source or fork
must fail source validation even when the branch name matches.
Given develop, when a PR targets main, then the same source guard admits it,
CI runs, and no repository Copilot review rule targets main.

### Developer evaluation

Given a .NET developer comparing substring search options, when they read the
website, then install syntax, recommended algorithm, measured tradeoffs, and
benchmark limitations are understandable by keyboard and at narrow widths.
Given the desktop sample, when they load data and run a query, then setup inputs disable while busy, activity/status explain the operation,
success shows result and oracle counts, and failure names recovery. A failed
reload retains the last complete successful warmup pass (possibly from the new
reload), so retry/search never mix datasets. Before the first successful pass,
search remains disabled.

## Technical Plan

### Governance

Use two active GitHub branch rulesets with empty bypass lists, PR-only merges,
resolved conversations, deletion prevention, and force-push prevention. Required
checks will be `Branch source`, `build-test`, and `sample`, pinned to GitHub
Actions. Use merge commits so promotion retains ancestry; never rebase.

The `branch-source.yml` workflow uses `pull_request_target`, checks only GitHub
event metadata, and never checks out or executes PR code. It accepts
`develop -> main` or `feature/<nonempty-name> -> develop` only when head and base
repository identities match. Handle opened, synchronize, reopened, edited,
and ready_for_review events so retargeting rechecks policy. Other sources fail.

A `copilot_code_review` rule exists only in the develop ruleset, excludes drafts,
and enables review_on_push. Copilot access/credits remain GitHub prerequisites.
External organization or user-level auto-review settings must be checked before
claiming that reviews outside develop are impossible.

### Skills and documentation

Install Google EGM from https://github.com/google/elephant-goldfish-model,
commit `403d8881d62022490637d740657b33f01e321ea8`, and Impeccable from
https://github.com/pbakaus/impeccable, commit
`e89e3ada3e875a89ac2c5d33d11e65ebef6fe285`.
Install runtime bundles into `.agents/skills`, plus EGM personas,
resources, harness docs, and licenses in `.agents`. Record upstream commit IDs.
Keep all repository-specific agent guidance in `AGENTS.md`. EGM drafts and reports
live in `docs/designs/`. Goldfish reviewers use isolated conversation contexts.

After the Goldfish review, write `PRODUCT.md`, an extracted `DESIGN.md`, its
`.impeccable/design.json` extension sidecar, and separate web/native surface briefs.
Document observed values and product facts rather than choosing a replacement
visual direction. Website mode is Read (help visitors understand documentation); desktop sample
mode is Operate (help people complete a task). The five Goldfish stages inspect
simplicity, reader comprehension, design/structure/style, simplicity again, then
implementation readiness. A detector is Impeccable's automated markup checker,
separate from the human-like design judgment. See the installed
[EGM review](../../.agents/skills/egm-review/SKILL.md) and
[Impeccable modes and commands](../../.agents/skills/impeccable/SKILL.md).

### Bounded remediation

Use independent review findings to select concrete fixes. For the website,
prioritize keyboard focus, readable overflow, semantics, reduced motion, and
preserving benchmark/proof content. For MAUI prioritize truthful status, stable
operation settings, input validity, and dataset/index consistency. Any algorithm
change requires a failing regression first. Stage 7 confirmed two bounded defects:
LINQ emits an occurrence multiple times when several fields match; exact-mode
character-tree construction crashes on an empty indexed field. Fix those without
changing public signatures, result ordering promises, or benchmark numbers.

## Alternatives

Classic branch protection alone cannot restrict PR head branches. A check in a
normal pull_request workflow can be rewritten by a contributor, so use the
trusted base workflow. A custom GitHub App would add service operation and tokens
without a need. Requiring an extra human approval would block this solo-owner
workflow; require CI and resolved conversations instead. A full interface redesign
would replace the existing identity without a requested product benefit.

## Security, Cost, and Risks

No credentials in source or documentation. Metadata passes through environment
variables rather than interpolation into executable code. GitHub-hosted CI and
Copilot consume existing account allocations; no new paid service is introduced.
Rulesets protect ordinary pushes/merges, but repository administrators can edit
rules and Actions workflows; absolute protection from an administrator is not a
claim. Failed or missing checks block merging; restoring service requires repairing
checks through a feature PR, not adding a standing bypass.

## Detailed Implementation Plan

1. Reuse existing develop. Create `feature/governance-design` from origin/develop;
   preserve untracked `.vscode/`. Update `AGENTS.md`, the existing README contributing section, and CI feature-branch filters.
2. Install both runtime bundles with provenance and licenses. Validate local skill
   references and executable launchers; leave global installations alone.
3. Create the trusted branch-source workflow. Test its source matrix locally:
   feature -> develop and develop -> main pass; release/main/fork/empty feature
   and feature -> main fail. Test PR retarget events in GitHub after installation.
4. After completing steps 5–8, bootstrap source enforcement safely: enable PR-only rules first, land this
   workflow through a reviewed feature -> develop -> main promotion, then activate
   required source and CI checks on both rulesets. This is the same feature PR/promotion sequence as step 9, not an earlier merge.
   Until activation, PR-only rules
   do not enforce source restrictions; record that temporary limitation explicitly.
   Never directly push to develop or main. Do not bypass a failing existing check.
5. Complete five Goldfish stages (zoom-out, comprehension, design/structure/style,
   intermediate zoom-out, implementation readiness), preserve reports, and
   resolve findings in the draft and implementation. Each reviewer reads its
   bundled persona and standards without conversation history.
6. Create the Impeccable context documents, then run independent design and
   detector/browser assessments against docs/index.html. Review the MAUI source
   separately because a web detector cannot validate native XAML. Record native
   visual verification separately from compile evidence.
7. Apply review corrections with a finding-to-change ledger. Preserve measured
   benchmark values so charts/specs/spreadsheets do not need regeneration.
8. Run warning-as-error Release build and unit tests; check workflow syntax/source
   matrix, skill paths, HTML, browser widths/keyboard, and Impeccable document
   consistency. Run the sample when the available Mac toolchain permits it.
9. Push a real feature change, open a develop PR, monitor automatic review/CI,
   fix substantive findings, and merge only after required gates pass. Promote
   develop -> main by PR, activate/check final rules, and verify both branch rule
   evaluations and cloud review scope. Do not create a release tag.

## Acceptance and Rollback

Acceptance requires live ruleset evidence; positive and negative source cases;
completed isolated reviewer reports with dispositions; successful applicable CI;
and rendered website checks at desktop/narrow widths. A compile is not native
visual acceptance. Pending checks or unavailable tools must stay explicit.
Rollback code by a revert committed to a feature branch and promoted through the
same PR route. Repair faulty protection through a documented ruleset edit only
if it prevents the repair PR; restore protection and verify it immediately.

## Open Questions

No new product direction is assumed. Organization/user auto-review settings and
native visual/runtime support are external verification points, not clean passes.

## Concrete Verification Contract

- Source matrix (base, head, repository): develop/feature/fix/MILL5 passes;
  main/develop/MILL5 passes; main/feature/fix, develop/release/1.2,
  develop/main, develop/feature/ and both matching-name fork cases fail.
  Execute the exact embedded workflow Python with these event environment values.
- Run `dotnet build FastSearch.slnx -c Release -warnaserror` and
  `dotnet test FastSearch.UnitTests -c Release`. Record any local toolchain
  failures independently from Linux library/benchmark builds and macOS CI.
- Inspect the web surface at 1440, 768, 390, and 320 CSS px. No document-wide
  horizontal overflow; tables/code may scroll inside named focusable regions.
  Keyboard: first Tab reveals a skip link, Enter focuses main, Tab reaches
  navigation/install actions and scroll regions with visible focus.
  Reduced-motion preference disables smooth scrolling while preserving state.
- Validate JSON-LD parsing, unique IDs, fragment targets, image alt text, HTML
  landmarks, and workflow YAML. Use `actionlint` for Actions and the Impeccable
  detector for web markup; separate false positives from verified findings.
- Native runtime acceptance requires launching the sample and exercising dataset
  load/search/error/retry in the actual app. Build failure from the installed
  MAUI/Xcode mismatch means native visual/runtime acceptance remains incomplete,
  even if source review and CI compile succeed. Do not call it fully accepted.
- Product output paths: `PRODUCT.md`, `DESIGN.md`, `.impeccable/design.json`,
  `.impeccable/surfaces/docs-index-html.md`; native context and surface brief live in
  `PasswordSearch/PRODUCT.md`, `PasswordSearch/DESIGN.md`, and
  `PasswordSearch/.impeccable/surfaces/mainpage-xaml.md`.
- Runtime checks: seven EGM SKILL.md files, nine persona files, all relative
  markdown references, both licenses/provenance JSONs; run pinned upstream
  `uv run --with pytest --with pyyaml pytest tests/unit` and
  `.agents/skills/impeccable/scripts/impeccable engine-probe`.
  Vendor markdown whitespace is normalized; Windows launcher CRLF is preserved.

## Accepted Review Corrections (Implementation Decisions)

- `FastSearch/Algorithms/LinqSearch.cs`: retain the existing per-field lowercase wrappers
  and PLINQ path. Give each wrapper its input-occurrence integer ID. When the
  constructor observes multiple non-null fields for an input occurrence, filter
  matching wrappers with PLINQ GroupBy(occurrence ID) and first wrapper per group before projecting instances;
  keep the existing single-field path. Never deduplicate by object equality.
  Keep public constructor/API/empty-query behavior.
- `FastSearch/Algorithms/CharSequenceSearch.cs`: skip null or empty index fields
  before suffix/exact indexing. Test exact-mode construction with empty fields
  mixed with a valid field, a one-field item, and a null selector field.
- Add `FastSearch.UnitTests/ReviewRegressionTests.cs` for these library defects.
  Verify regression failure against pre-change source, then pass with fixes.
- `PasswordSearch/MainViewModel.cs`: construct each of six indexes into locals;
  assign the six live fields only after every constructor completes. No await
  during publication. Commands exclude concurrent operations; failed reload
  retains the last complete successful pass, which may be from the new reload.
  Publish one complete set per warmup pass; before any successful pass, search
  remains disabled. Failure never leaves partially replaced fields.
- Snapshot entry count before Task.Run, and search phrase/repetition/algorithm
  on the UI thread before Task.Run. Disable the setup panel while IsBusy so
  visible settings cannot diverge from the operation. Add IsIdle change
  notification; bind SetupPanel.IsEnabled to it in MainPage.xaml.
- Reject a dataset size outside 1..1,000,000 before starting work; reject empty
  search text and nonpositive repetitions. Read at most the selected number of
  nonblank packaged rows with an appropriately sized list; log actual loaded
  count, not the old Load 10M label. This reduces avoidable allocation and makes
  the displayed dataset match the measured indexes.
- In the search log, append actual and oracle result counts after timing. A
  mismatch raises an InvalidOperationException with both counts, so the existing
  wrapper reports failure and restores commands rather than reporting success.
- Add linked production MainViewModel.cs and TimerScope.cs to the existing
  FastSearch.UnitTests project, reference CommunityToolkit.Mvvm 8.4.2 (the
  sample's existing version), and add test-only PasswordSearch.MainThread and
  FileSystem adapters. These supply dispatch and deterministic streams without
  depending on native workloads. Exercise command completion through busy-state
  property notifications. To verify failed reload retention, make private Index
  accept IEnumerable<string> (callers still pass List<string>); in a reflection
  test supply an enumerable that throws on its third enumeration, then assert
  all six live index fields still reference their prior complete set.
  These tests validate logic only, not actual native controls/thread dispatch.
- Web changes are now specified in Impeccable Amendment below and the completed
  `impeccable_review.md`. Single final readiness re-audit passed before editing.

## File Operations and Phases

Create: `.github/workflows/branch-source.yml`, `.agents/` runtime bundles and
provenance/licenses, `docs/designs/` draft/reports/ledger, root `PRODUCT.md`,
`DESIGN.md`, `.impeccable/design.json`, `.impeccable/surfaces/docs-index-html.md`, native
`PasswordSearch/PRODUCT.md`, `PasswordSearch/DESIGN.md`,
`PasswordSearch/.impeccable/design.json`,
`PasswordSearch/.impeccable/surfaces/mainpage-xaml.md`,
`FastSearch.UnitTests/ReviewRegressionTests.cs`,
`FastSearch.UnitTests/MainViewModelTests.cs`,
`FastSearch.UnitTests/MauiTestAdapters.cs`.
Modify: `AGENTS.md`, `README.md`, `.github/workflows/ci.yml`,
`FastSearch/Algorithms/LinqSearch.cs`,
`FastSearch/Algorithms/CharSequenceSearch.cs`,
`FastSearch.UnitTests/FastSearch.UnitTests.csproj`,
`PasswordSearch/MainViewModel.cs`, `PasswordSearch/MainPage.xaml`,
`docs/index.html` (only accepted Impeccable fixes),
`.impeccable/config.json` only if review proves an existing ignore invalid.
Delete: none. Preserve unrelated `.vscode/`.

Phase A: install/document/review on the feature branch; no protected-branch merge.
Phase B: run Impeccable, amend accepted web changes, implement regression-backed
library/native corrections and interface fixes; validate final feature head.
Phase C: resolve automatic Copilot findings, pass CI, merge feature -> develop,
then develop -> main; activate required checks only after both contain the trusted
workflow. Validate source policy using real allowed/rejected PRs without merging
rejected ones. Never disable a failing existing check to promote code.

## Concrete Regression Assertions

Create library regressions: two matching fields return one occurrence; one object
supplied twice returns twice in LINQ; distinct equal-valued objects retain their
separate reference identities; compare query multiplicities with StringSearch.
Exact-mode CharSequenceSearch accepts empty/null fields and still finds the
valid exact field; empty input query returns zero.

Create native logic regressions using linked source and test-only adapters:
initial search is disabled; entry counts 0, -1, 1,000,001 and invalid typed text
do not start a dataset operation; empty/whitespace query and repetitions <=0 do
not start timing; a stream containing blank rows stops after the selected nonblank
count and reports actual count. Gate a test stream so controls' values can change
after command execution: loaded count/query/repetition/algorithm must use their
captured values. Inject a later-constructor enumeration failure and compare all
six index field references to the prior complete set. Throw from the test stream
and assert IsBusy=false and commands recover. Supply a fake ISearch via reflection
that returns a mismatching count and assert failure status/log include actual and
oracle counts, with no Search complete status.

Test adapter contract: `PasswordSearch.MainThread.InvokeOnMainThreadAsync(Action)`
executes the action synchronously and returns Task.CompletedTask;
`PasswordSearch.FileSystem.OpenAppPackageFileAsync(string)` returns a fresh Stream
from a per-test async factory. Adapters are test-only. Mark the native test class
DoNotParallelize, reset factory in TestCleanup, register a PropertyChanged handler
and TaskCompletionSource before executing the RelayCommand, wait for the IsBusy
true->false transition with a 10-second timeout, and always remove the handler.
Assert an initial stream failure leaves search disabled; a subsequent valid stream
load enables search and restores the expected dataset/count.

Create `scripts/test_branch_source_policy.py`: execute the exact embedded workflow Python against ten source/repository combinations; run it in the existing CI build-test job.

## Impeccable Amendment: Concrete Web/Native Changes

Modify docs/index.html only: `.top-bar-inner` min-height 56px/height:auto, wrapping
at max-width 900px with nav links flex-wrap and minimum 24px height; preserve all
six links. At <=640px use 12px/16px top-bar padding and 8px/14px link gaps.
Add first-in-body skip link to main#main-content tabindex=-1, explicit emerald
3px focus-visible outlines and 4px offsets, 176px compact/80px desktop anchor
scroll offsets. Name and tabindex both table wrappers and the code region.
Add `.btn-primary:hover` canvas foreground; remove blanket contrast waivers from
.impeccable/config.json. Add reduced-motion scroll:auto and heading/paragraph
balance/pretty. Cap .faq-a/p.note at 75ch, keeping tables/figures full width.
Remove duplicate .section-label elements/style and noninteractive .algo-card:hover.
Link the five benchmark chart images and add full-size links in existing captions.
Make the code sample pre/code with preserved whitespace, same highlighted text,
then move existing specification paragraph after it. Add a Quick Start anchor
beside the unchanged install command. No larger section reorder or copy claims.

Modify MainPage.xaml: add visible 1–1,000,000 dataset range and semantic names on
six radio controls; MainViewModel.cs log uses Character Sequence consistently.
Update extracted documents to actual source values, including native purple
platform primary buttons (emerald is the app icon, not the observed native button).

Acceptance: source HTML/JSON-LD/IDs/links validate; repeated/named scroll regions
are keyboard-focusable with visible outlines, skip link reaches main, anchor targets
clear sticky nav, no page-wide overflow at 1440/768/390/320. Hovered install CTA
contrast >=4.5:1; default already dark on emerald. Preserve benchmark table text and
JSON-LD numerical claims byte-for-byte. Rendered detector checks may retain the
explicit incumbent-glow exception and low-priority wraps; do not claim a clean scan.
Native preview names all six algorithms, accepts/rejects count and retries, and
shows validated result counts. Test logic remains separate from native visuals.

Create `docs/favicon.svg` from the existing navigation magnifier geometry and reference it in index metadata; browser acceptance exposed an implicit favicon 404. No raster assets generated.

Finish review: place release metadata below hero description and all six algorithm tags below their h3 headings; preserve text while removing the remaining eyebrow pattern.
