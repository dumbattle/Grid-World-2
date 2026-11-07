# Delaunay Triangulation

There are two implementations. Use the modern one (`OrthogonalDelaunay`) for new work.

| | Legacy (`Delaunay`) | Modern (`OrthogonalDelaunay`) |
|---|---|---|
| Coordinates | `Vector2` (float) | `Vector2Int` (integer) |
| Storage | Object graph (classes per edge/vertex/triangle) | Packed arrays, reusable slots |
| GC pressure | High | Low (uses `ObjectPool` internally) |
| Pathfinder | `DelaunayPathfinder` (legacy) | `DelaunayPathfinder` (modern) |
| Path simplification | `DelaunayAlgorithms.Funnel` | `List<AStarTriangle>.Funnel()` extension |
| Status | Legacy — do not use for new code | Current |

---

## Modern: `OrthogonalDelaunay`

Namespace: `LPE` (inferred from files in `Triangulation/Delaunay/`)

### Construction
```csharp
var d = new OrthogonalDelaunay();
// Automatically creates a super-triangle large enough to contain any reasonable point set
```

Implements `IReadOnly_OrthogonalDelaunay` (read/query only interface, for passing to pathfinders).

---

### Mutating the Triangulation

```csharp
void AddPoint(Vector2Int point)
void RemovePoint(Vector2Int point)

void AddConstraint(Vector2Int p1, Vector2Int p2)
void RemoveConstraint(Vector2Int p1, Vector2Int p2)
```

- `AddPoint` is idempotent — adding a duplicate does nothing.
- `RemovePoint` re-triangulates the cavity left behind.
- Constraints are edges that the triangulation will never flip. Used to encode walls, boundaries, etc.
- Adding a constraint automatically adds the two endpoint vertices if they don't exist.
- Throws `InvalidOperationException` if you try to add a constraint that overlaps an existing one.
- Constraints that cross existing constrained edges are split automatically.

---

### Querying

```csharp
// Get the TriangleID(s) that contain or touch a point
void GetTriangles(Vector2 point, HashSet<TriangleID> results)

// Get geometry and neighbor data for a triangle
DelaunayTriangleData GetTriangleData(TriangleID id, bool allowConstraintCrossing = true)

// Get centroid
Vector2 GetCenter(TriangleID id)

// Check if a handle is still valid (triangles can be invalidated by mutations)
bool IsValidTriangle(TriangleID id)
```

`allowConstraintCrossing = false` treats constrained edges as walls: neighbors across a constraint are returned as `TriangleID.INVALID`.

---

### `TriangleID` (struct)

A **versioned handle** to a triangle. Triangles are stored in reused slots; the version prevents stale handles from pointing to a reallocated triangle.

```csharp
TriangleID.INVALID   // Sentinel for "no triangle"
bool isValid         // Check against INVALID sentinel (not the same as IsValidTriangle())

// IEquatable<TriangleID>
```

Always verify with `d.IsValidTriangle(id)` before using a stored `TriangleID` after any mutation.

---

### `DelaunayTriangleData` (struct)

Read-only snapshot returned by `GetTriangleData`.

```csharp
// Neighbors (CCW order, same winding as vertices)
TriangleID n1, n2, n3
TriangleID GetNeighbor(int i)              // i = 0, 1, 2

// Vertices (CCW order)
Vector2Int v1, v2, v3
Vector2Int GetVertex(int i)

// Centroid
Vector2 center

// Portals: the shared edge between this triangle and neighbor i
void GetPortal(int i, out Vector2Int a, out Vector2Int b)
// Properties: p1, p2, p3 — each returns (Vector2Int, Vector2Int) portal tuple
```

Vertex index `i` and neighbor index `i` are aligned: `GetPortal(i)` returns the edge shared with `GetNeighbor(i)`.

---

## Modern Pathfinder: `DelaunayPathfinder`

```csharp
var pathfinder = new DelaunayPathfinder(IReadOnly_OrthogonalDelaunay delaunay);
```

### A* Search
```csharp
void AStar(Vector2 start, Vector2 end, List<AStarTriangle> result, float radius = 0)
```

- Appends to `result` — clear it first if you want a fresh path.
- `radius`: Minimum clearance. Portals narrower than `2 * radius` are skipped.
- If the end is unreachable, backtracks to the closest reached triangle.
- Uses `ObjectPool` internally — no allocations in hot paths.

### `AStarTriangle` (struct)
```csharp
TriangleID triangleID
Vector2Int exitPortalV1
Vector2Int exitPortalV2
```

Each entry is a triangle in the path plus the portal (shared edge) used to leave it. The last entry's portal vertices are both `Vector2Int.zero` (end triangle has no exit portal).

---

## Path Simplification: Funnel Algorithm

Converts the triangle channel from A* into a list of turn waypoints.

```csharp
// Extension method on List<AStarTriangle>
path.Funnel(Vector2 start, Vector2 end, List<Vector2> result, float radius = 0)
```

- Modifies `result` in-place (appends).
- `radius`: Pulls waypoints away from portal endpoints by this amount (agent clearance).
- Always includes `start` (or close to it) as the first entry and `end` as the last.
- Note: Corner accuracy with `radius > 0` is approximate.

---

### Full Example (Modern)

```csharp
var d = new OrthogonalDelaunay();

// Build nav mesh
foreach (var pt in navMeshPoints)
    d.AddPoint(pt);

// Add walls as constraints
d.AddConstraint(wallStart, wallEnd);

// Pathfind
var pathfinder = new DelaunayPathfinder(d);
var trianglePath = new List<AStarTriangle>();
pathfinder.AStar(agentPos, targetPos, trianglePath, radius: 5);

// Extract waypoints
var waypoints = new List<Vector2>();
trianglePath.Funnel(agentPos, targetPos, waypoints, radius: 5);
// waypoints[0] ≈ agentPos, waypoints[^1] ≈ targetPos
```

---

## Key Concepts

### Delaunay Property
The triangulation guarantees that no point lies inside the circumcircle of any triangle. This maximizes the minimum angle, producing well-shaped triangles.

### Constraints
Constrained edges are never flipped, even if doing so would improve the Delaunay property. They represent hard boundaries (walls, edges of a nav mesh region).

### Super-Triangle
Implementation creates a large "super-triangle" enclosing all points. Triangles touching super-triangle vertices should be excluded from game logic. The modern implementation handles this automatically in queries.

### Validity
Triangle handles (`TriangleID`) become invalid after mutations (add/remove point or constraint). Always call `IsValidTriangle()` before using a stored handle.
