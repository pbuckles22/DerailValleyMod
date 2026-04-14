# Test plan (TEST_PLAN.md)

Define **Tier 1** (fast feedback: unit, headless, or local) and **Tier 2** (integration, browser, device, or E2E) for your stack. Replace the placeholders below.

---

## Tier 1: Fast feedback

```bash
# Example: npm test
# Example: pytest -q
```

---

## Tier 2: Integration / E2E

Use when behavior spans a real runtime (browser, device, network, or native APIs).

```bash
# Example: npm run test:e2e
# Example: playwright test
```

---

**Handoff:** Document the exact commands you use for coverage in AGENT_HANDOFF.md so agents can run them consistently.
