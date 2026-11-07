# Open design decisions

Design decisions we haven't made yet, in the order we plan to tackle them. The leans are starting points, not decisions. When one is decided, record it in the relevant rule or system doc and remove it from this list.

Settled design is in [architecture.md](../.claude/rules/architecture.md).

## Can wait until the features need them

| Decision | The question | Current lean |
|---|---|---|
| **Flags and conditions** | How do story flags work, and how do NPCs and triggers change based on them, including single-use triggers? | Simple keyed flags on world state |
| **Saving** | What's saved, in what format, how is it versioned, and when can the player save? Running cutscenes hold live sequences that can't easily be saved. | A plain data format with a version number from day one. Saving probably waits until no cutscene is running |
| **Dialogue and UI** | Which UI framework do we use, and what's the exact format of a `DialogueSO`? | Undecided. Text already lives in `DialogueSO` assets, and only the text box reads them |
| **Audio** | How do music changes work across areas, and do tracks crossfade? | Per-area music, with a crossfade at borders since there's no screen transition to hide a change |
| **Reuse** | How do other games consume the framework: a Unity package, a copy, or a shared folder like LPE? | Follow how LPE is shared, unless that's been a problem |

## Checks and open items from settled design

- Measure the hitch when a large area scene activates.
- Confirm cached contexts can support a game context's second state reference. If they can't, the game's context needs its own small cache.
- Prove the game extension pattern with a small test in `Game`.
- Check how tile data authoring fits with LPE's rule tiles and tile metadata tools.
- Test tall grass as an overlay tile on the `World` layer once real tiles exist.
- Test `[SerializeReference]` feature records at scale. A bulky feature can store its data as arrays in one record.
- Measure the memory cost of always-loaded assets, since area data assets keep everything they reference in memory, including NPC sprites.
- Check in play: the idle frame on a follower that waits a tick at the start of a chain, and the one-tick delay before a ledge hop.
- Work out how an NPC with several components shares one `EntityID` through the world feature path. Starting idea: one entity behaviour gathers its components into a single record.
- Handle a failed load during a door's fade, which would otherwise hang the cutscene.
- Consider `AllAiInChunk(...)` as an optional helper if spawning sprites ends up building a per-area entity list.
- Other stuck cutscene cases: watch for them and raise them when they come up. One known case: a cutscene waiting on an absent entity whose tile stays occupied.
