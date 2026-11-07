# Grid World 2: Docs Index

Orientation map for the project. Keep it short and current; add one line per system as it's built and approved.

## Status

The first slice is being built, following the [first slice plan](first-slice-plan.md). Phase 1 is done: world state, contexts, and entity stores, with no scenes or gameplay yet. Remaining design questions are in [open decisions](open-decisions.md).

## Systems

System docs come after the first slice. Until then, the design is in [architecture.md](../.claude/rules/architecture.md).

| System | Doc | Key types |
|---|---|---|
| World state and contexts | [architecture.md](../.claude/rules/architecture.md#state-and-contexts) | `WorldInstance`, `IWorldFunctionContext`, `WorldFunctionContext` |
| Entities | [architecture.md](../.claude/rules/architecture.md#entities) | `EntityIDManager`, `EntityComponent<T>`, `EntityComponentManager`, `EntityDefinitionSO` |

## Related

- [LPE library docs](../Unity%20Project/Assets/2.%20Scripts/LPE/Docs/README.md)
- The previous project, `../../Grid World` (Unity 2020.3), is a reference for ideas only. Don't port from it unless asked.
