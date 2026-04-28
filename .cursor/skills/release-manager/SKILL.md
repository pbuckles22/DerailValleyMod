---
name: release-manager
description: Establish lightweight release discipline: definition of merge-ready, versioning/changelog habits, and safe rollout/rollback practices.
---

# Release Manager — Ship Without Chaos

Use this skill when:
- You’re preparing to merge a feature branch
- You want to declare work “merge-ready” or “release-ready”
- You need consistent changelogs / release notes

Goal: make shipping predictable with minimal ceremony.

---

## Merge-ready checklist (generic)

1. **Scope**: change matches the intended branch/feature scope.
2. **Green**: evals/tests required by the repo are passing (Tier 1 minimum; Tier 2 if behavior demands it).
3. **Docs**: runbook/README/TEST_PLAN updates are in place if behavior changed.
4. **Rollback**: a safe revert path exists (single revert commit is ideal).

---

## Release notes template (short)

- **Summary**: 1–3 bullets of what changed and why
- **Risk**: 0–2 bullets (what could go wrong, how to detect)
- **Rollback**: how to undo (revert commit / restore previous version)

---

## Versioning (choose one and stick to it)

- **SemVer** (recommended for libraries/tools): MAJOR.MINOR.PATCH
- **Date-based** (recommended for internal ops scripts): YYYY.MM.DD[.N]

Document the chosen convention in a tracked release doc (e.g. `RELEASE.md`) and keep it consistent.

