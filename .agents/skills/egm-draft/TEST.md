# Test Plan: EGM Draft Skill

Frontmatter and link checks run in the pinned upstream EGM checkout with `pytest tests/unit`; that upstream test suite is not installed in FastSearch (see [TESTING.md](https://github.com/google/elephant-goldfish-model/blob/403d8881d62022490637d740657b33f01e321ea8/TESTING.md)).

## Protocol Verification Checklist

When evaluating or exploring `egm-draft` interactively in chat, verify:

-   **Step 4 (Alternatives):** Reads rejected alternatives from the brief's
    `Alternatives Weighed` section, or asks the user if it is absent.
-   **Step 6 (Finalize):** Reports contradictions between the brief and the
    draft before directory selection, and waits for the user to decide each
    one. Does not self-grade against
    the Zoom Out standard.
