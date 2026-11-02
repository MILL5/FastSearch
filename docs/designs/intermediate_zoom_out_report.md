# Stage 8: Intermediate zoom-out

Reviewer /root/goldfish_zoom_intermediate; isolated fresh context, read-only.
Mandatory full reads: zoom_out persona, zoom-out-standard, current draft.
Targeted reuse searches completed; no missing reads or builds.

- Major, draft:269–273: choose the existing test project and smallest test-only
  platform adapters instead of leaving two test architectures open. Avoid a
  general production service/factory abstraction.
- Major, draft:88,253–256: define failed-reload retention across warmup passes.
  The minimal per-pass publication retains the last complete successful pass,
  possibly from the new dataset, rather than the pre-reload set in every case.
- Nit, draft:243–247: LINQ has one PLINQ query, not sequential/parallel branches.

Disposition: all PROPOSE FIX. Existing test project, test-only platform adapters,
private IEnumerable failure seam, last-complete-pass retention, and occurrence-ID
LINQ deduplication are now explicit in the draft. No general service layer.
