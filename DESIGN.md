---
name: FastSearch documentation
description: Dark technical documentation with green-to-blue gradients and measured comparisons
colors:
  canvas: "#07070a"
  elevated: "#0f0f14"
  panel: "#13131a"
  text: "#f5f5f7"
  text-secondary: "#e1e7ed"
  text-muted: "#acbac7"
  emerald: "#10b981"
  emerald-bright: "#34d399"
  blue: "#3b82f6"
  syntax-keyword: "#c084fc"
  syntax-type: "#38bdf8"
  syntax-string: "#fbbf24"
  chart-paper: "#fcfcfb"
  white: "#ffffff"
gradients:
  accent: "linear-gradient(135deg, {colors.emerald} 0%, {colors.blue} 100%)"
typography:
  display:
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    fontSize: "clamp(2.2rem, 5vw, 3.4rem)"
    fontWeight: 800
  headline:
    fontSize: "1.75rem"
    fontWeight: 700
  body:
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif'
    lineHeight: 1.65
  code:
    fontFamily: '"SF Mono", "Fira Code", "Cascadia Code", monospace'
    fontSize: "0.85rem"
    lineHeight: 1.7
  navigation:
    fontSize: "0.88rem"
  navigation-compact:
    fontSize: "0.82rem"
  tag:
    fontSize: "0.72rem"
  badge:
    fontSize: "0.78rem"
  intro:
    fontSize: "1.08rem"
  button:
    fontSize: "0.95rem"
  algorithm-title:
    fontSize: "1.15rem"
  supporting-text:
    fontSize: "0.92rem"
  step-title:
    fontSize: "1.05rem"
  step-description:
    fontSize: "0.9rem"
  stat:
    fontSize: "2rem"
  ecosystem-link:
    fontSize: "0.84rem"
  faq-heading:
    fontSize: "1.02rem"
  fact:
    fontSize: "0.93rem"
  author-heading:
    fontSize: "1.35rem"
  author-link:
    fontSize: "12px"
  brand:
    fontSize: "1.1rem"
rounded:
  tag: "6px"
  badge: "999px"
  control: "10px"
  figure: "12px"
  panel: "16px"
spacing:
  compact: "8px"
  normal: "16px"
  section-inline: "24px"
  section-block: "80px"
components:
  button-primary:
    backgroundColor: "{gradients.accent}"
    textColor: "{colors.canvas}"
    rounded: "{rounded.control}"
    padding: "12px 24px"
---

# Design System: FastSearch documentation

## Overview

Extracted from docs/index.html for this maintenance task. The existing composition
pairs installation with algorithm tradeoffs, benchmark tables and source material.
Dark neutral surfaces carry near-white text. The original green-to-blue gradients
color the hero heading, primary action, step markers and benchmark statistics;
emerald identifies links and the recommended algorithm.

## Colors

Frontmatter records the canonical solid colors. Existing borders are white at
8% and 14% opacity. The canvas, elevated code surface and panel separate content
without heavy shadows. The author avatar has a specific orange outline/shadow
from the existing attribution card; do not spread it through the interface.

## Typography

System sans is used for reading and controls. Monospace is reserved for code and
existing small author links. Headings use the extracted hierarchy; code retains
whitespace. Tables use compact numbers with a caption explaining measurements.

## Layout

Content max-width is 960px. Sections use 80px block/24px inline padding, reducing
to 56px/20px at 640px. Algorithm, process and ecosystem grids use auto-fit columns.
Tables and code may scroll internally; the document must fit a 320px viewport.
The top bar remains sticky, wraps below 900px, and uses explicit anchor scroll
offsets. Navigation and full-size chart links remain keyboard accessible. A skip
link reaches main; named table/code regions expose local keyboard scrolling.

## Elevation & Depth

Surfaces use tonal separation and thin borders. Preserve the incumbent emerald
hero glow and attribution treatment during scoped corrections; do not add new
layers or decorative shadows.

## Shapes

Panels are 16px, figures 12px, controls 10px. Small tags use 6px and the release
badge is a pill. Preserve recognizable components rather than standardizing all
radii during a quality fix.

## Components

Primary installation action uses a green-to-blue gradient with canvas text; secondary source action
is a bordered panel. Navigation links expose section anchors. Comparison tables
retain captions and the recommended-row highlight. Code samples remain selectable.
Focused controls and scroll regions must visibly stand apart from adjacent content.

## Do's and Don'ts

- Do preserve installation syntax, data, metadata, factual copy and attribution.
- Do keep horizontal overflow local to tables/code and expose keyboard access.
- Do honor reduced-motion preference for scroll behavior.
- Don't hide benchmark limitations or replace measured data with decorative metrics.
- Don't introduce a framework, new font download or new brand direction here.
