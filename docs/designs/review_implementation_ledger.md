# Review implementation ledger

## Goldfish

| Finding | Implemented change | Evidence |
| --- | --- | --- |
| Contribution guide still uses release branches | README/AGENTS/CI use feature branches | Branch matrix, Actions lint |
| Tooling/context/rollout unclear | Pins, definitions, file operations, phases, exact acceptance in design_draft | Five isolated stages + single final readiness re-audit |
| LINQ multifield duplicates | Conditional PLINQ grouping by input-occurrence ID | Regression before failures; after 5 library tests pass |
| Exact character tree crashes on empty field | Skip empty/null fields | Regression reproduced crash, now passes |
| Failed reload mixes indexes | Build locals, publish complete successful warmup pass | Original reference-retention failure reproduced; fixed logic test passes |
| Search mismatch reports success/count hidden | Actual/oracle counts outside timing, mismatch throws into failure path | Native logic tests; actual preview valid counts 11=11 |
| Inputs change mid-operation/invalid input | UI-thread snapshots, IsIdle disables setup, typed text/bounds validation | Twenty native tests; invalid count preview |
| Dataset allocation/count misleading | Bounded nonblank read, actual row-count log | Cutoff/blank/snapshot tests; actual load100 |
| Test plan ambiguous | Existing project, linked production source, serialized test-only adapters | Full56 tests pass -warnaserror; no native-runtime claim from adapters |

## Impeccable

| Finding | Implemented change | Evidence |
| --- | --- | --- |
| Narrow nav overflow risk | Wrapping navigation, growing sticky bar | Four tested widths, no document overflow |
| Reading/keyboard route gaps | Skip link, visible focus, named/tabbable scroll regions, anchor offset | Actual Tab/Enter/ArrowRight and settled offsets |
| Hover contrast hidden by waiver | Dark hover foreground; remove wildcard waivers | 7.93:1 computed default+hover |
| Narrow chart text | Original-size chart/image+caption links | Five resources200 and retained alt/captions |
| Implementation route delayed | Quick Start beside exact command; specs after semantic pre/code | Exact actual clipboard text; DOM/source review |
| Prose/motion/wrap issues | 75ch prose, reduced-motion auto, balance/pretty | Detector/browser checks |
| Redundant section kickers/faux interaction | Remove redundant labels and noninteractive card hover | Source/finish review |
| Remaining release/tag eyebrows | Move below corresponding description/headings | Finish-review correction and updated captures |
| Native range/name differences | Visible range and consistent Character Sequence log | Source + final native preview |
| Native unnamed algorithm switches | Six semantic descriptions | Final actual app AX exposes all six names |
| Implicit favicon404 | SVG reuses nav magnifier | Fresh isolated requests200; console clean |
| Extracted context incomplete | Product/design/surface docs and sidecars, actual type/theme values | Runtime link checks; Impeccable docs |

## Automatic cloud review

First real pushed head received Copilot review (no manual request). Accepted the
missing bootstrap contract required fields, unresolved upstream test/tool links,
upstream icon-consumer clarification and README branch mismatch. Fixed installed
runtime docs with pinned upstream links and recorded local patches in provenance.
Remaining automatic exact-head reviews/CI are checked before merge.

## Verification boundaries

Strict Release library/test/benchmark builds: zero warnings/errors. Unit tests:
56 passed. Pinned upstream EGM packaging:165 passed. Local installed runtime:
114 real links resolve (template examples excluded); bootstrap schema rejects
missing contract fields and accepts complete ones. Actions lint/source matrix pass.
Native preview rebuilt and exercised on Mac; strict local full solution build is
blocked by installed Xcode27.2 vs MacCatalyst-required27.0 and deprecated Xamarin
settings. The preview uses ValidateXcodeVersion=false; this does not establish
supported-toolchain acceptance. macOS CI is a separate compile gate.

Web validation is isolated Chromium, not a cross-browser or physical-touch audit.
Historical benchmark numbers, chart PNGs and workbooks are unchanged. No version
bump, release tag, NuGet publication, new raster or global toolchain changes.

Final native preview additionally confirmed Character Sequence selection/log name
and actual11=oracle11 after load100; all six names appear in native AX.

Second cloud review identified an ambiguous provisioning contract in desired-state
JSON. Added `.github/rulesets/README.md` with exact API application and safe
bootstrap ordering, clarified AGENTS, and retained full active payloads only as
final desired state. The older-head missing-script finding was fixed in f0c1882;
that head passed Linux and macOS CI. No manual review request was made.
