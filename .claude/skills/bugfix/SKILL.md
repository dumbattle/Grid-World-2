---
name: bugfix
description: Diagnose and fix a bug in Grid World 2. Use when the user reports something broken, wrong, or unexpected, such as an error, a compile failure, or a character walking through a wall.
---

# Bugfix workflow

1. **Get the failure.** Find the exact symptoms and steps to reproduce. For errors, read `%LOCALAPPDATA%\Unity\Editor\Editor.log` rather than asking me to paste them. Ask only for what you can't find yourself.

2. **Find the root cause.** Trace it through the code and confirm the cause rather than patching the symptom. If it's in logic, try to reproduce it with a failing EditMode test.

3. **Report and propose.** Give the cause (`file:line`), the proposed fix, and any related risk. Then wait for approval, unless I've already told you to fix it.

4. **Fix it** with the smallest change that addresses the cause. Keep the failing test as a regression test where possible. Report other problems you notice; don't fix them in the same change.

5. **Verify** using the `unity-verify` workflow, then tell me how to confirm it in play mode if tests can't cover it.

6. **Update docs** only if the fix changes documented behavior.

If the bug is in LPE, report it and propose the change, then wait for me to confirm. LPE is shared with my other projects (CLAUDE.md rule 6), and this applies even if I've already told you to fix the bug.
