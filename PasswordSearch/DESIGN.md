---
name: FastSearch Password Search
description: Existing native MAUI desktop benchmark controls and readable results
colors:
  primary-light: "#512BD4"
  primary-dark: "#ac99ea"
  primary-dark-text: "#242424"
  result-light: "#F7F7FA"
  result-dark: "#18181C"
  text-light: "#202026"
  text-dark: "#EFEFF4"
  border-light: "#D8D8E0"
  border-dark: "#42424A"
typography:
  title:
    fontSize: "26"
    fontWeight: 700
  section:
    fontSize: "16"
    fontWeight: 700
  result:
    fontFamily: "Menlo, Consolas"
    fontSize: "13"
rounded:
  results: "8"
spacing:
  window: "24"
  result-padding: "12"
---

# Design System: FastSearch Password Search

## Overview

Extracted from MainPage.xaml; this is the incumbent desktop interface, not a
mobile redesign. Familiar entries, radio buttons and commands keep the measured
results central. Do not apply web decoration or replace native platform controls.

## Colors

The results surface follows AppThemeBinding pairs recorded above. Secondary text
and placeholder colors also adapt to light/dark appearance. Primary actions use the existing purple light/dark theme, confirmed in the
native preview; the green app icon does not define button color. Global resources are
in Resources/Styles; retain their platform-specific control behavior.

## Typography

Title 26, section headings 16, results 13 with Menlo on Mac and Consolas on Windows.
Semantic heading levels identify the page and groups. Native labels/entries remain
readable with platform scaling; test that in the actual app before acceptance.

## Layout

Window padding and gaps are 24. Setup is 280 wide beside results. Below workspace
width 820 the code-behind stacks setup/results in equal star rows. Setup scrolls;
results use a read-only Editor. Busy status lives beside an ActivityIndicator.

## Elevation & Depth

Flat layout with a thin results border; no new shadows or layered cards.

## Shapes

The results border has radius 8. Native controls retain platform-provided shapes.

## Components

Dataset count entry/load action; six radio choices; query entry/run action;
activity/status; selectable read-only timing log. Setup disables during an
operation. Validation failures must be shown as failures, not successful searches.

## Do's and Don'ts

- Do retain native controls and complete datasets on failed reloads.
- Do show actual counts and recover commands after failure.
- Don't confuse compilation or test adapters with native visual acceptance.
- Don't introduce mobile navigation or a new component system for this desktop sample.
