# FastSearch

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

.NET developers evaluating in-memory substring search, reading algorithm tradeoffs,
and installing the FastSearch NuGet package. Contributors maintain the library,
benchmarks and MAUI demonstration in this repository.

## Product Purpose

Help developers choose and use a case-insensitive contains-search implementation
for their in-memory objects. The website makes installation, algorithm selection,
measurement context and source documentation findable.

## Positioning

Six implementations share ISearch<T>. The n-gram index compresses posting lists;
the site compares build cost, retained memory and query latency rather than
claiming one implementation wins every workload.

## Operating Context

Static HTML/CSS at docs/index.html, published by GitHub Pages. No sign-in,
application backend, purchase flow or interactive search runs on the website.
Native sample context is scoped separately under PasswordSearch/PRODUCT.md.

## Capabilities and Constraints

Library targets net10.0; the MAUI sample targets Mac Catalyst and Windows.
StringSearch supplies the test oracle. Recorded benchmark numbers describe the
committed dataset/harness; they are evidence, not guarantees for arbitrary input.
See README.md and docs/*-Spec.md. Do not invent new performance claims.

## Brand Commitments

Existing FastSearch name, MILL5 ownership, Inventing Fire with AI author identity,
logos, dark/emerald website and factual copy are incumbent evidence. This task is
quality refinement, not a rebrand. No new visual direction is assumed.

## Evidence on Hand

README benchmark tables, docs/images charts and sample screenshot, archive
workbooks, algorithm specifications, source code and unit tests. No new benchmark
run or customer testimonial was supplied for this task.

## Product Principles

- Explain the cost of indexing alongside search speed and retained memory.
- Keep installation and the shared interface easy to locate.
- Preserve recorded measurement context and source attribution.
- Make documentation navigable by keyboard and readable on narrow displays.
