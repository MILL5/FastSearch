# FastSearch Password Search

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Users

Developers locally comparing the six search algorithms against the same packaged
password dataset. This is a benchmark demonstration, not a password manager.

## Product Purpose

Load a selected dataset size, inspect indexing timings, choose an algorithm and
repeat a query. Show honest operation state and validate the count against the
StringSearch oracle outside the measured search loop.

## Operating Context

.NET MAUI desktop sample on Mac Catalyst and Windows. One responsive setup/results
screen, local packaged data, no authentication, service or upload. Adaptive here
means desktop platform controls; it does not declare an iOS/Android release.

## Capabilities and Constraints

Six algorithms; default N-Gram; default 10,000 rows and repetitions. Complete
index sets must share one dataset. Long indexing/search work runs off the UI
thread. Native visual/runtime acceptance requires the real app; web detectors
and source-linked test adapters cannot establish it.

## Brand Commitments

Keep the existing title, native controls, light/dark resources, purple themed primary
actions and green app icon, and side-by-side/stacked setup/results structure.

## Evidence on Hand

MainPage.xaml, MainViewModel.cs, TimerScope.cs, Resources/Styles and the committed
website app screenshot. Toolchain failures must be recorded rather than called a
successful native acceptance run.

## Product Principles

- Measure only the selected algorithm in its timed loop.
- Keep the UI responsive and prevent overlapping operations.
- Explain failures and allow retry with a complete consistent index set.
- Show actual dataset and result counts, including validation outcomes.
