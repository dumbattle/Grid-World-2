---
name: review-document
description: Review code and then document it once understanding is confirmed. Use when the user says "review these", "look at X", "document X", or otherwise asks for existing code to be examined or written up in Grid World 2.
---

# Review and document workflow

This is loose on purpose: any reasonable phrasing triggers it. Fill in missing steps by asking rather than guessing.

1. **Read it.** Use git (`status`, `diff`, `show`) to see what changed when possible, rather than rescanning everything.

2. **Focus on these, in order:**
   1. Bugs, including potential ones.
   2. Usage: how it's meant to be called, and whether callers do so correctly.
   3. Architecture and purpose, including whether it respects the `GridWorld`/`Game` boundary.

3. **Report in chat first.** Give a summary and your questions. Don't write any doc files yet.

4. **Wait for me to react.** I may correct a misreading, answer questions, or make fixes myself.

5. **Then document** into `Docs/`, once understanding is confirmed, and add a line to `Docs/INDEX.md`.

Flagging a bug is not permission to fix it. Propose fixes, and apply them only if I say so.
