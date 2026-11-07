---
name: feature
description: Plan, build, and hand off a new system or a change to an existing one in Grid World 2. Use when the user wants to add or change gameplay, framework, or tooling code, such as "add NPC movement" or "let's build map transitions".
---

# Feature workflow

A statement of intent starts the plan, not the work (CLAUDE.md rule 1).

1. **Orient.** Read `Docs/INDEX.md`, the docs for any system involved, and the relevant LPE docs. Check whether LPE or existing code already covers part of this.

2. **Propose a plan in chat,** then stop for approval. Include:
   - What the player or developer will be able to do afterward.
   - Which assembly each piece goes in: `GridWorld`, or `Game` if it's test-only.
   - Any LPE change or addition, called out separately so I can confirm it (CLAUDE.md rule 6).
   - New or changed types and how they connect to the tick loop and the grid.
   - Anything I must do in the Editor (scenes, prefabs, Inspector).
   - How it will be tested.
   - Open questions, each with your recommendation.

   Build the smallest version that can be played and judged. Note later ideas separately instead of building them.

3. **Wait for approval** ("Implement", "Proceed", "Go ahead", "Do it", "Yes").

4. **Implement.**
   - Follow the rules in `.claude/rules/`.
   - Write EditMode tests for plain C# logic, such as movement rules, occupancy, and state.
   - Stick to the approved plan. If it turns out to be wrong, stop and say so rather than improvising a different design.

5. **Verify** using the `unity-verify` workflow. Report the actual results.

6. **Hand off for review:**
   - List what was added, changed, or removed.
   - Give numbered Editor setup steps, if any.
   - Give a short play-test checklist of what to try and what should happen.
   - State anything that wasn't verified.

7. **After approval,** update `Docs/INDEX.md` and the system's doc under `Docs/`, describing the current state only. See CLAUDE.md rule 3 for what counts as approval.

8. **Commit only if I say to** (CLAUDE.md rule 4).

Small, clearly scoped tasks can skip steps 2 and 3 (CLAUDE.md rule 2).
