# GitHub ruleset provisioning

`main.json` and `develop.json` are desired-state API payloads. GitHub does not load
these files automatically. Their `enforcement: active` describes the final state;
applying either full payload before its base branch contains branch-source.yml
would block the initial promotion. They are not evidence of live enforcement.

## Ordered bootstrap

1. Confirm develop exists. It already existed for this rollout; do not recreate it.
2. Create PR-only rulesets through GitHub settings/API with no bypass actors,
   deletion/force-push prevention, resolved conversations, and existing build-test
   and sample checks. At this stage omit Branch source from required checks.
   The develop ruleset also enables copilot_code_review, review_on_push=true,
   review_draft_pull_requests=false. Main has no Copilot rule.
3. Merge the feature PR to develop after exact-head CI/review. Until the trusted
   workflow reaches the bases, an operator verifies the same-repository source
   explicitly; PR-only settings alone do not enforce source restrictions.
4. Open develop -> main. Verify its source repository and branch, wait for CI,
   and merge by PR. Do not apply main's full payload before this bootstrap merge.
5. Fetch both protected branches and verify each contains
   `.github/workflows/branch-source.yml`, then apply the complete payloads using
   the existing live rule IDs. This rollout created main26361178 and
   develop26361179 via the repository API, with required source checks withheld
   during bootstrap. Discover IDs by name again rather than assuming them.
6. Read back effective rules for both branches; require exactly Branch source,
   build-test and sample from GitHub Actions15368, no bypass actors, PR-only
   merge commits, deletion/force-push prevention and resolved conversations.
   Only develop has the automatic Copilot rule. Test the trusted source guard
   with allowed and rejected PR metadata; close validation PRs without merging
   rejected sources.

## Apply final payloads after step4

Run from the repository root. These commands update existing rulesets; they do not
create branches, bypass checks, merge PRs or request a review.

```sh
main_rule_id=$(gh api repos/MILL5/FastSearch/rulesets --jq '.[] | select(.name == "FastSearch main governance") | .id')
develop_rule_id=$(gh api repos/MILL5/FastSearch/rulesets --jq '.[] | select(.name == "FastSearch develop governance") | .id')
gh api --method PUT "repos/MILL5/FastSearch/rulesets/$main_rule_id" --input .github/rulesets/main.json
gh api --method PUT "repos/MILL5/FastSearch/rulesets/$develop_rule_id" --input .github/rulesets/develop.json
gh api repos/MILL5/FastSearch/rules/branches/main
gh api repos/MILL5/FastSearch/rules/branches/develop
```

The source guard runs on pull_request_target using trusted base-branch workflow
code and event metadata only. It never checks out/executes the PR head. Draft PRs
still run this guard, while develop's Copilot rule excludes draft reviews.
Repository/user/organization review settings are independent; these templates
control this repository's branch-scoped automatic review rules.
