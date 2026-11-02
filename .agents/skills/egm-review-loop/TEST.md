# Test Plan: Goldfish Review Loop Skill

Frontmatter and link checks run in the pinned upstream EGM checkout with `pytest tests/unit`; that upstream test suite is not installed in FastSearch (see [TESTING.md](https://github.com/google/elephant-goldfish-model/blob/403d8881d62022490637d740657b33f01e321ea8/TESTING.md)).

## Protocol Verification Checklist

When evaluating or exploring `egm-review-loop` interactively in chat, verify:

-   **Step 1 (Intake & Config):** Validates required inputs and resolves standard paths.
-   **Step 2.1 (Target Resolution):** Unconditionally re-scans line numbers before auditing.
-   **Step 2.2 (Review Swarm):** Spawns subagents (or sequentially iterates personas in degraded mode) across requested standards.
-   **Step 2.3 & 2.4 (Resolve Handoff & Turn Boundary):** Hands findings to `egm-resolve` and respects human decision gates.
-   **Step 2.4 (Round Control):** Keeps contested and accepted findings settled, re-opens fixed findings that reviewers raise again, judges clean rounds before applying fixes, records failed reviewers (and reviewers with no status in the synthesizer summary) as not run, stops after two consecutive clean rounds, and asks the user at the 5-round cap or on churn.
