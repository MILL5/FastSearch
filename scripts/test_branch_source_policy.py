"""Exercise the exact trusted workflow code against allowed/rejected PR metadata."""
import os
from pathlib import Path
import subprocess
import textwrap
import unittest

WORKFLOW = Path(__file__).resolve().parents[1] / ".github/workflows/branch-source.yml"
SOURCE = textwrap.dedent(
    WORKFLOW.read_text().split("python3 - <<'PYTHON'\n", 1)[1].split("          PYTHON", 1)[0]
)


class BranchSourcePolicyTests(unittest.TestCase):
    def test_promotion_matrix(self):
        for base, head, repository, allowed in (
            ("develop", "feature/fix", "MILL5/FastSearch", True),
            ("main", "develop", "MILL5/FastSearch", True),
            ("main", "feature/fix", "MILL5/FastSearch", False),
            ("develop", "release/1.2", "MILL5/FastSearch", False),
            ("develop", "main", "MILL5/FastSearch", False),
            ("develop", "feature/", "MILL5/FastSearch", False),
            ("develop", "feature/fix", "fork/FastSearch", False),
            ("main", "develop", "fork/FastSearch", False),
            ("develop", "feature/fix", "", False),
            ("other", "feature/fix", "MILL5/FastSearch", False),
        ):
            with self.subTest(base=base, head=head, repository=repository):
                result = subprocess.run(
                    ["python3", "-c", SOURCE],
                    env=dict(os.environ, BASE_REF=base, HEAD_REF=head,
                             HEAD_REPO=repository, BASE_REPO="MILL5/FastSearch"),
                    capture_output=True, text=True, check=False,
                )
                self.assertEqual(result.returncode == 0, allowed, result.stderr)


if __name__ == "__main__":
    unittest.main()
