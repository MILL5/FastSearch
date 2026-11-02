---
version: 1
slug: "mainpage-xaml"
primary_target: "MainPage.xaml"
related_targets: []
---

# Password search workbench

Mode: Operate. Target: MainPage.xaml. Preserve the incumbent native desktop UI.

## Task

Load selected rows, choose one of six algorithms, search, and read validated counts
and timings. Disable setup while busy; preserve the last complete index set after
failure. No mobile release, new navigation system or aesthetic redesign.

## Verification

Check native light/dark, wide/stacked layout, keyboard controls, valid/invalid load,
search result counts, failure and retry. Source/test/compile evidence is separate
from actual native visual/runtime acceptance; record toolchain/UI-tool limitations.
