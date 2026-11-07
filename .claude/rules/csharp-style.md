---
paths:
  - "**/*.cs"
---

# C# style

```csharp
namespace GridWorld {
    public class TileOccupancy {
        // Private: no modifier, camelCase, no underscore prefix. Dictionaries are named key2value.
        Dictionary<Vector2Int, EntityID> tile2entity = new Dictionary<Vector2Int, EntityID>();

        // Public properties and fields: camelCase. Expression bodies are fine on properties.
        public int count => tile2entity.Count;

        // Methods: PascalCase. Opening brace on the same line. Always a block body.
        public bool IsFree(Vector2Int tile) {
            return !tile2entity.ContainsKey(tile);
        }

        // Always use braces, even for one line.
        public void Release(Vector2Int tile) {
            if (tile2entity.ContainsKey(tile)) {
                tile2entity.Remove(tile);
            }
        }
    }
}
```

- **Braces:** always, on the same line. Fix any brace-less code you touch outside LPE, but don't refactor just for this.
- **No expression-bodied methods.** Expression-bodied properties are fine.
- **Namespaces:** `GridWorld` for the framework, `Game` for test code, with sub-namespaces only when a folder grows large enough to need them.
- **Suffixes:** `SO` for ScriptableObjects, `Behaviour` for MonoBehaviours (British spelling, matching Unity and LPE), and an `I` prefix for interfaces.
- **Inspector fields:** `[SerializeField]` on a private field, not a public field.
- **Gameplay loops:** no per-tick allocations in code that runs every tick. Use `LPE.ObjectPool` where it helps.
- **Unbounded loops:** guard `while` loops that depend on data with `LPE.LoopSafety`.
- **Comments:** explain why, not what. Use `<summary>` only on public framework API where the contract isn't obvious.
- **One public type per file**, with the file named after the type. Partial classes use `Type.Part.cs`.
- **Tests:** NUnit, in a test assembly next to the code it covers. Name them `Method_Condition_Result`.
