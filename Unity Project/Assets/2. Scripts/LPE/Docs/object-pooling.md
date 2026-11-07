# Object Pooling

Namespace: `LPE`

---

## Overview

Two layers of pooling API:

| Layer | When to use |
|-------|-------------|
| `ObjectPool<T>` (class) | Pool a specific type you control; need lifecycle hooks |
| `ObjectPool` (static) | Quick pooling of standard collections (`List`, `HashSet`, `Dictionary`, `Stack`, `Queue`) and any `new()`-able type |

RAII scoped cleanup is available for both layers via `PooledItemScope<T>`.

---

## `ObjectPool<T>` (class)

Generic instance pool. `T` must be a reference type.

### Construction
```csharp
var pool = new ObjectPool<MyClass>(() => new MyClass(), warningCount: 1000);
// warningCount: logs a warning each time createdCount hits a multiple of this value
```

### Core Methods
```csharp
T   Get()           // Returns a pooled item (creates one if pool is empty)
void Return(T item) // Returns item to pool

// Throws InvalidOperationException on:
//   - Double-return (same item returned twice)
//   - Item not from this pool
```

### Scoped (auto-return on Dispose)
```csharp
using var scoped = pool.GetScoped();
MyClass item = scoped.item;
// item is returned to pool when the using block exits
```

With lifecycle actions:
```csharp
// PooledItemScope constructor (used internally by GetScoped):
new PooledItemScope<T>(pool, onGet: t => t.Reset(), onReturn: t => t.Clear())
```

### Diagnostics
```csharp
int createdCount    // Total created (in use + available)
int availableCount  // Currently in the pool
int inUseCount      // Currently checked out
```

---

## `ObjectPool` (static helper class)

Singleton pools for common .NET collection types, plus any `new()`-able type.

### Generic type (requires `new()`)
```csharp
T   ObjectPool.Get<T>()         where T : class, new()
void ObjectPool.Return<T>(T item) where T : class, new()
```

### `List<T>`
```csharp
List<T>   ObjectPool.GetList<T>()
void      ObjectPool.ReturnList<T>(List<T> item)   // clears before returning
```

### `HashSet<T>`
```csharp
HashSet<T>  ObjectPool.GetHashSet<T>()
void        ObjectPool.ReturnHashSet<T>(HashSet<T> item)   // clears before returning
```

### `Dictionary<TKey, TVal>`
```csharp
Dictionary<TKey, TVal>  ObjectPool.GetDictionary<TKey, TVal>()
void                    ObjectPool.ReturnDictionary<TKey, TVal>(Dictionary<TKey, TVal> item)   // clears
```

### `Stack<T>`
```csharp
Stack<T>  ObjectPool.GetStack<T>()
void      ObjectPool.ReturnStack<T>(Stack<T> item)   // clears
```

### `Queue<T>`
```csharp
Queue<T>  ObjectPool.GetQueue<T>()
void      ObjectPool.ReturnQueue<T>(Queue<T> item)   // clears
```

> All `Return*` methods **clear the collection** before putting it back — do not hold references to the collection after returning it.

---

## Scoped Static Collections

Prefer scoped versions when the collection is only needed inside one block — they guarantee return even if an exception is thrown.

```csharp
using var s = ObjectPool.GetScopedList<int>();
s.item.Add(42);
// s.item is returned automatically
```

Available scoped variants:
```csharp
ObjectPool.GetScopedList<T>()
ObjectPool.GetScopedHashSet<T>()
ObjectPool.GetScopedStack<T>()
ObjectPool.GetScopedDictionary<T, U>()
```

---

## `PooledItemScope<T>` (struct, IDisposable)

The backing type for all scoped pool usage.

```csharp
T item { get; }   // The pooled item
void Dispose()    // Runs onReturn action (if any), then returns to pool
```

Constructor (rarely called directly):
```csharp
new PooledItemScope<T>(ObjectPool<T> pool, Action<T> onGet = null, Action<T> onReturn = null)
```

---

## `ObjectPool.SetReminder(string id)` (static)

Development helper. Logs a one-time warning reminding you to implement a pool for `id`.

```csharp
ObjectPool.SetReminder("MyExpensiveClass");
// Logs once per session — won't spam
```

---

## Usage Patterns

### Pattern 1 — Temporary collection, no escaping
```csharp
var nearby = ObjectPool.GetHashSet<Enemy>();
grid.QueryItems(queryMin, queryMax, nearby);
foreach (var e in nearby) ProcessEnemy(e);
ObjectPool.ReturnHashSet(nearby);
```

### Pattern 2 — Scoped collection (exception-safe)
```csharp
using var scoped = ObjectPool.GetScopedList<Vector2>();
BuildPath(scoped.item);
// scoped.item is returned at end of using block
```

### Pattern 3 — Custom pooled class
```csharp
static readonly ObjectPool<DesireStorage> _pool = new(() => new DesireStorage());

// Acquire
var storage = _pool.Get();
storage.Init(param);

// Release
storage.Clear();
_pool.Return(storage);
```

### Pattern 4 — Scoped custom object
```csharp
using var scoped = myPool.GetScoped();
scoped.item.DoWork();
// Returned automatically
```

---

## Notes

- `ObjectPool<T>` detects double-returns and foreign items — exceptions are intentional to surface bugs early.
- The static `ObjectPool` pools are keyed by type, shared across the whole application.
- All `Return*` methods in the static class clear the collection — **never read from a returned collection**.
- `PooledItemScope<T>` is a `struct` — allocating it on the heap (e.g. boxing) defeats its purpose; always use `using var`.
- Pools log a warning when `createdCount` hits multiples of `warningCount` — if you see these warnings, something is leaking (not returning items).
