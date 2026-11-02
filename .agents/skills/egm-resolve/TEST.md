# Test Plan: Goldfish Review Resolve Skill

Frontmatter and link checks run in the pinned upstream EGM checkout with `pytest tests/unit`; that upstream test suite is not installed in FastSearch (see [TESTING.md](https://github.com/google/elephant-goldfish-model/blob/403d8881d62022490637d740657b33f01e321ea8/TESTING.md)).

## Protocol Verification Checklist

When evaluating or exploring `egm-resolve` interactively in chat, verify:

-   **Step 1 (Intake & Context):** Ingests `TargetFile`, `LineRange`, and `FindingsList`.
-   **Step 2 (Evaluate & Categorize):** Defends valid design choices with checkable evidence (`CONTEST`), gives the user refutations that no reviewed file supports (`ESCALATE`), drafts concrete repairs (`PROPOSE FIX`), and records Nits without a fix (`DECLINE`).
-   **Step 3 (Report Fixes):** Generates `goldfish_review_report.md` artifact.
-   **Step 4 (Resolution Gate):** Prompts the user with clear decision choices (Auto accept, Continue, Interactive breakdown) without modal popups, and executes approved file modifications.
