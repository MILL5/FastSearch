# Verification and Evaluation Commands for `egm-bootstrap`

Frontmatter and link checks run in the pinned upstream EGM checkout with `pytest tests/unit`; that upstream test suite is not installed in FastSearch (see [TESTING.md](https://github.com/google/elephant-goldfish-model/blob/403d8881d62022490637d740657b33f01e321ea8/TESTING.md)).

## Manual Verification Scenarios

1. **Small Tree Execution (Sequential)**:
   - Target a directory with 2-5 subdirectories.
   - Verify that the agent executes sequentially without spawning subagents.
   - Verify generated `README.md` files contain Interface, Invariants, Side Effects, and relative links.
2. **Safety Gate Threshold (>15 Subdirectories)**:
   - Target a directory with 16+ subdirectories.
   - Verify that discovery pauses upon reaching >15 directories and requests user confirmation in chat before proceeding.
3. **Subagent Halting Gate**:
   - Run in an environment without task delegation tools.
   - Verify that the prominent halting banner is displayed and execution stops immediately.
