## What this proposes

The proposal installs project-local EGM and Impeccable review tooling, documents the existing product and interface design, and uses independent reviews to select bounded corrections while preserving APIs, benchmark measurements, and visual identity. It establishes GitHub rulesets that require same-repository `feature/<nonempty-name>` → `develop` → `main` promotion through PRs, resolved conversations, trusted source validation, and CI, with no standing bypasses. A metadata-only `pull_request_target` workflow enforces source identity; automatic Copilot review applies to non-draft develop PRs and real pushes, subject to external account settings. Enforcement is bootstrapped through PR promotion before required checks activate, and acceptance combines isolated review reports, CI, live governance evidence, and rendered interface checks; rollback uses the same promotion route unless a faulty rule prevents its repair.

## How the existing system works, per this document

FastSearch is a .NET 10 in-memory, case-insensitive substring-search library with six implementations of `ISearch<T>`; StringSearch supplies the consistency oracle, and existing tests cover several search and indexing behaviors. A command-line benchmark measures indexing, search, and heap costs, with optimized heap comparisons isolated by algorithm process. The MAUI sample loads shared packaged data, constructs six indexes, runs and measures searches, validates counts outside the timed loop, and exposes busy/completion/failure state while background work and UI updates require thread coordination. The website is a static HTML page containing inline styling, installation and algorithm guidance, benchmark evidence, metadata, FAQ, and attribution; GitHub Actions builds/tests/packages the library, builds the sample, and publishes qualifying version tags. The repository currently has unprotected main/develop branches, older release-branch guidance, and no checked-in product context or extracted design-system documentation.

## Findings

1. **Major — Feature scope, Skills and documentation, lines 109–117; steps 2, 5–6.** Read, Operate, Goldfish stages, and detector are tooling-specific concepts without definitions or links to local instructions. Add definitions and local links so readers can distinguish the assessments and locate the governing workflow.
2. **Major — Implementation steps 2 and 6, lines 151–152, 165–168.** Identify upstream repositories and commit pins, installed inventory, exact surface-brief paths, commands, and expected results for runtime validation. Both runtime bundles otherwise requires choosing installation inputs and outputs.
3. **Major — Implementation steps 4–5 and 9, lines 156–177.** Step 4 appears to land the workflow before design review, while step 9 describes promotion later. Define explicit phases, PR contents, review completion checkpoints, and protection activation.
4. **Major — Implementation steps 6–7, lines 165–170.** Apply review corrections leaves website/native behavior changes to future judgment. Finish the assessments and amend the plan with accepted findings, files, chosen changes and regression expectations before declaring implementation readiness.
5. **Major — Implementation steps 3 and 8; acceptance, lines 153–155, 171–184.** Specify source-matrix payloads/assertions, browser widths, keyboard journey, workflow/HTML validation and native-runtime disposition. Say which missing checks prevent full acceptance.

## Mandatory reads declaration

Reviewer /root/goldfish_comprehension, isolated `fork_turns: none`.
Read in full: `.agents/agents/comprehension.md`,
`.agents/resources/document_standards/feature-spec-standard.md`,
`.agents/resources/document_standards/implementation-readiness-standard.md`,
and `docs/designs/design_draft.md`.
Applied feature standard to lines 1–146, implementation readiness to 147–192.
No missing reads, memory/history, edits, builds, or code-level feature requirements.
