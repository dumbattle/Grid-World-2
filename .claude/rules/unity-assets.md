---
paths:
  - "Unity Project/**"
---

# Unity project and assets

## What Claude can and can't touch

- **Claude can't operate the Unity Editor.** For scene, prefab, or Inspector setup, give me exact numbered steps, or propose an Editor script if I'll repeat the steps often.
- **Don't hand-edit `.unity`, `.prefab`, or `.asset` YAML** unless I ask. It's easy to corrupt references. Reading them to investigate is fine.
- **Never edit generated files:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln`.
- Ask before changing `ProjectSettings/` or `Packages/manifest.json`, and say why the change is needed.

## `.meta` files

- `.meta` files hold the GUIDs that references depend on. Moving or renaming an asset must move or rename its `.meta` too, or references break silently.
- Unity generates the `.meta` for a new file on import, so don't write them by hand.
- Deleting an asset means deleting its `.meta` as well. Check for references first and ask.
- Moving a `.cs` file across assemblies can break serialized references, so flag it in the plan.

## Folders

Under `Unity Project/Assets/`, top-level content folders are numbered so they sort in a consistent order:

```
1. Scenes    2. Scripts    3. Sprites    4. Animations    5. Prefabs    6. Tiles    7. Audio    8. Data
```

Create them when first needed. `Scenes/` still has its default name and should become `1. Scenes` through the Editor, because the build settings point to it. Content that belongs to the reusable framework sits in a `GridWorld/` subfolder, and test content in a `Game/` subfolder.

## Art

- The framework is art-agnostic, and each game chooses its own style. One tile is always one Unity unit, so tile size in pixels never appears in framework code.
- Pixels Per Unit and filter mode are per-game import settings.
- View size, in tiles, and optional pixel snapping are world settings. Pixel snapping is off by default.
- Test scenes in this project use simple original placeholder art.

## Input

Use actions from `InputSystem_Actions.inputactions`, never `UnityEngine.Input` or hard-coded keys. The legacy input manager is disabled in this project.

## Original assets only

Don't add ripped or traced sprites, music, maps, or fonts from existing games. Placeholder art is fine as long as it's original or properly licensed; note the source and license of anything third-party in the docs.
