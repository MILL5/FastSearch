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

GitHub currently has `main` and `develop`, but neither is protected. The prior
repository guidance uses release branches for development. The requested policy
replaces that with feature branches. Repository settings alone cannot express
which source branch may open a mergeable PR, so a required trusted check must
validate source identity. The existing website and desktop sample have no
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
Given the desktop sample, when they load data and run a query, then inputs,
activity, result count, errors, and retry behavior remain coherent.

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

Install pinned upstream runtime bundles into `.agents/skills`, plus EGM personas,
resources, harness docs, and licenses in `.agents`. Record upstream commit IDs.
Keep all repository-specific agent guidance in `AGENTS.md`. EGM drafts and reports
live in `docs/designs/`. Goldfish reviewers use isolated conversation contexts.

After the Goldfish review, write `PRODUCT.md`, an extracted `DESIGN.md`, its
`.impeccable/design.json` extension sidecar, and separate web/native surface briefs.
Document observed values and product facts rather than choosing a replacement
visual direction. Website mode is Read; desktop sample mode is Operate.

### Bounded remediation

Use independent review findings to select concrete fixes. For the website,
prioritize keyboard focus, readable overflow, semantics, reduced motion, and
preserving benchmark/proof content. For MAUI prioritize truthful status, stable
operation settings, input validity, and dataset/index consistency. Any algorithm
change requires a failing oracle-consistency regression test first and remains
out of scope unless necessary to address a confirmed review defect.

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
   preserve untracked `.vscode/`. Update `AGENTS.md` and CI feature-branch filters.
2. Install both runtime bundles with provenance and licenses. Validate local skill
   references and executable launchers; leave global installations alone.
3. Create the trusted branch-source workflow. Test its source matrix locally:
   feature -> develop and develop -> main pass; release/main/fork/empty feature
   and feature -> main fail. Test PR retarget events in GitHub after installation.
4. Bootstrap source enforcement safely: enable PR-only rules first, land this
   workflow through a reviewed feature -> develop -> main promotion, then activate
   required source and CI checks on both rulesets. Until activation, PR-only rules
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
