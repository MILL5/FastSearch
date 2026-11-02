# Stage 7: Design, structure, and style review

Method: isolated read-only reviewers /root/goldfish_design_critic and
/root/goldfish_structure_style (`fork_turns: none`); each read its full persona,
feature-spec-standard, and target draft. No missing mandatory reads.
Design critic examined six algorithms, tests, benchmark harness, MAUI, web and CI.

## Design critic findings

1. **Major — Dataset publication is not atomic.** MainViewModel.cs:180–211
   replaces indexes one field at a time. Failed reload can leave old/new datasets
   mixed while search becomes enabled. Build all replacements locally; publish
   only after all succeed. Retain the last complete dataset on failed reload.
   Verify a failure after at least one replacement has been constructed.
2. **Major — Invalid results report success; valid results omit count.**
   MainViewModel.cs:39,65–66,129–139 only logs a mismatch before reporting Search
   complete. Display actual/oracle counts outside timing; propagate mismatch to
   failure status and expose successful result count.
3. **Major — Empty exact-match character-tree field crashes construction.**
   CharSequenceSearch.cs:122–133 sets endIndex=1 for an empty field with
   enableExactMatch=true then indexes value[0]. Skip empty fields, with a
   regression mixing empty and valid fields against the oracle.
4. **Major — LINQ duplicates objects when multiple fields match.**
   LinqSearch.cs:65–72,87–94 projects every matching field; the oracle emits an
   input occurrence once. Regress distinct objects, multiple matching fields,
   and repeated input occurrences, then retain each matching occurrence once
   without collapsing distinct objects that compare equal.

No other confirmed architecture defect in benchmark timing boundaries, static
page architecture, metadata-only guard, or release ancestry/version checks.

## Structural and style findings

Structural: no findings; feature scope covers applicable requirements and has
sequential headings.
- Style Nit, draft:117: define Read and Operate. Accepted; definitions and links
  added to the draft.
- Style Nit, draft:85: replace remain coherent with observable loading, failure,
  and retry behavior. Accepted; explicit states and preservation contract added.

## Disposition

PROPOSE FIX for all six findings; the two library changes are confirmed defect
repairs with regression-first verification, not algorithm redesign. Live ruleset
configuration and native visuals were not validated by these source reviewers.
