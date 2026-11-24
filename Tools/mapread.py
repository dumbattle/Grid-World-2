"""Views of Grid World areas, for drafting and editing map specs without the Unity Editor.

Reads the project's text-serialized assets and scenes, and never writes to them. Only 'spec'
writes files, and only under Maps/.

    py Tools/mapread.py overview
    py Tools/mapread.py area <id> [--legend NAME]
    py Tools/mapread.py edges --bounds X Y W H [--depth N] [--legend NAME]
    py Tools/mapread.py tiles <folder under Assets>
    py Tools/mapread.py spec <id> [--legend NAME] [--write-legend] [--region X Y W H]
    py Tools/mapread.py features <id>

Grids print top row first (highest y), matching map specs. '.' is an empty cell.
"""

import argparse
import json
import math
import os
import re
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(REPO, "Unity Project")
ASSETS = os.path.join(PROJECT, "Assets")
LEGENDS = os.path.join(REPO, "Maps", "legends")
AREAS = os.path.join(REPO, "Maps", "areas")

TILEMAP_CLASS = "1839735485"
GAMEOBJECT_CLASS = "1"
TRANSFORM_CLASS = "4"
MONOBEHAVIOUR_CLASS = "114"
TILEMAP_RENDERER_CLASS = "483693784"
LOGIC_LAYER_SCRIPT_GUID = "1b62e50ec3617fe4aacaa81e9efdd0ec"
SINGLE_SPRITE_FILE_ID = "21300000"
UNITY_TILE_GUID = "0000000000000000e000000000000000"
DOC_HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)", re.M)
GUID = re.compile(r"guid: ([0-9a-f]{32})")
# Symbols for tiles the legend doesn't cover, avoiding '.', '#' and '['.
AUTO_SYMBOLS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!$%&*+-=?@^~"
# Spec symbols: '.' is empty, '?' keeps a cell, '#' starts a comment and '[' a layer header.
RESERVED_SYMBOLS = ".?#["
SPEC_SYMBOLS = [c for c in AUTO_SYMBOLS if c not in RESERVED_SYMBOLS]
# A tile gets a legend logic tile when more than this share of its cells sit on that one logic
# tile. Applying keeps the cells that differ, as hand-painted logic.
LOGIC_AGREEMENT = 0.5
# Words skipped when picking a symbol from a tile's name.
FILLER_WORDS = {"on", "of", "the", "and", "a", "tile"}


# ---------- Asset index ----------

_guid2path = None


def guid_to_path():
    """Maps every asset GUID to its project-relative path, from the .meta files."""
    global _guid2path
    if _guid2path is None:
        _guid2path = {}
        for folder, _, files in os.walk(ASSETS):
            for name in files:
                if not name.endswith(".meta"):
                    continue
                meta = os.path.join(folder, name)
                with open(meta, encoding="utf-8", errors="replace") as f:
                    for line in f:
                        if line.startswith("guid: "):
                            asset = os.path.relpath(meta[:-5], PROJECT).replace("\\", "/")
                            _guid2path[line[6:].strip()] = asset
                            break
    return _guid2path


def read(path):
    with open(os.path.join(PROJECT, path), encoding="utf-8", errors="replace") as f:
        return f.read()


def field(text, name):
    match = re.search(r"^  " + re.escape(name) + r": ?(.*)$", text, re.M)
    return match.group(1).strip() if match else None


def class_id(text):
    return field(text, "m_EditorClassIdentifier") or ""


# ---------- Worlds and areas ----------

class Area:
    def __init__(self, path, text):
        self.path = path
        self.id = field(text, "id")
        self.scene = field(text, "sceneFilePath")
        bounds = re.search(r"localTileBounds:\s+x: (-?\d+)\s+y: (-?\d+)\s+width: (\d+)\s+height: (\d+)", text)
        offset = re.search(r"worldOffset: \{x: (-?\d+), y: (-?\d+)\}", text)
        x, y, w, h = (int(v) for v in bounds.groups()) if bounds else (0, 0, 0, 0)
        ox, oy = (int(v) for v in offset.groups()) if offset else (0, 0)
        self.x, self.y, self.w, self.h = x + ox, y + oy, w, h

    @property
    def x_max(self):
        return self.x + self.w

    @property
    def y_max(self):
        return self.y + self.h


def find_assets(class_suffix):
    for guid, path in guid_to_path().items():
        if not path.endswith(".asset"):
            continue
        text = read(path)
        if class_id(text).endswith(class_suffix):
            yield path, text


def worlds():
    """(world path, [Area]) for every WorldDataSO, areas in list order."""
    result = []
    for path, text in find_assets("GridWorld.WorldDataSO"):
        areas = []
        section = text.split("areaList:", 1)
        if len(section) == 2:
            for line in section[1].splitlines()[1:]:
                if not line.startswith("  - "):
                    break
                match = GUID.search(line)
                area_path = guid_to_path().get(match.group(1)) if match else None
                if area_path:
                    areas.append(Area(area_path, read(area_path)))
        result.append((path, areas))
    return sorted(result)


def all_areas():
    seen = {}
    for _, areas in worlds():
        for area in areas:
            seen.setdefault(area.path, area)
    return list(seen.values())


# ---------- Scenes ----------

class Tilemap:
    def __init__(self, name):
        self.name = name
        self.cells = {}  # (x, y) -> tile asset path, or None for a missing asset
        self.sorting_layer_id = 0
        self.sorting_order = 0
        self.is_logic = False
        self.at_origin = True


def scene_tilemaps(scene_path):
    """Every tilemap in a scene, named after its GameObject. Assumes tilemaps sit at the origin."""
    text = read(scene_path)
    starts = [(m.start(), m.group(1), m.group(2)) for m in DOC_HEADER.finditer(text)]
    docs = []
    for i, (start, cls, file_id) in enumerate(starts):
        end = starts[i + 1][0] if i + 1 < len(starts) else len(text)
        docs.append((cls, file_id, text[start:end]))

    names = {}
    object2renderer = {}
    off_origin = set()
    logic_objects = set()
    for cls, file_id, body in docs:
        owner = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
        owner = owner.group(1) if owner else None
        if cls == GAMEOBJECT_CLASS:
            names[file_id] = field(body, "m_Name") or ""
        elif cls == TILEMAP_RENDERER_CLASS:
            object2renderer[owner] = (int(field(body, "m_SortingLayerID") or 0), int(field(body, "m_SortingOrder") or 0))
        elif cls == TRANSFORM_CLASS:
            position = re.search(r"m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", body)
            if position and any(float(v) != 0 for v in position.groups()):
                off_origin.add(owner)
        elif cls == MONOBEHAVIOUR_CLASS and LOGIC_LAYER_SCRIPT_GUID in (field(body, "m_Script") or ""):
            logic_objects.add(owner)

    tilemaps = []
    for cls, file_id, body in docs:
        if cls != TILEMAP_CLASS:
            continue
        owner = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
        owner = owner.group(1) if owner else None
        tilemap = Tilemap(names.get(owner, "?"))
        tilemap.sorting_layer_id, tilemap.sorting_order = object2renderer.get(owner, (0, 0))
        tilemap.is_logic = owner in logic_objects
        tilemap.at_origin = owner not in off_origin

        assets = []
        if "m_TileAssetArray:" in body:
            block = body.split("m_TileAssetArray:", 1)[1].split("\n  m_", 1)[0]
            for match in re.finditer(r"m_Data: \{fileID: -?\d+(?:, guid: ([0-9a-f]{32}))?", block):
                guid = match.group(1)
                assets.append(guid_to_path().get(guid, "missing:" + guid) if guid else None)

        if "m_Tiles:" in body:
            block = body.split("m_Tiles:", 1)[1].split("\n  m_", 1)[0]
            for match in re.finditer(r"first: \{x: (-?\d+), y: (-?\d+), z: -?\d+\}\s+second:\s+serializedVersion: \d+\s+m_TileIndex: (\d+)", block):
                x, y, index = int(match.group(1)), int(match.group(2)), int(match.group(3))
                tilemap.cells[(x, y)] = assets[index] if index < len(assets) else None
        tilemaps.append(tilemap)
    return tilemaps


# ---------- Legends and symbols ----------

class Symbols:
    """Tile path -> one character, from a legend first, then made up as tiles appear."""

    def __init__(self, legend_name):
        self.path2symbol = {}
        self.auto = {}
        if legend_name:
            with open(os.path.join(LEGENDS, legend_name + ".json"), encoding="utf-8") as f:
                legend = json.load(f)
            for entry in legend.get("entries", []):
                self.path2symbol.setdefault(entry["tile"], entry["symbol"])
        self.free = [c for c in AUTO_SYMBOLS if c not in self.path2symbol.values()]

    def get(self, path):
        if path in self.path2symbol:
            return self.path2symbol[path]
        if path not in self.auto:
            self.auto[path] = self.free.pop(0) if self.free else "?"
        return self.auto[path]

    def print_key(self):
        if self.auto:
            print("Not in the legend:")
            for path, symbol in sorted(self.auto.items(), key=lambda p: p[1]):
                print(f"  {symbol} = {path}")


def print_grid(tilemap, x, y, w, h, symbols):
    """Prints the cells of one tilemap inside a rectangle, with y labels on the left."""
    label = max(len(str(y)), len(str(y + h - 1)))
    print(f"{'':>{label}}   x {x}..{x + w - 1}")
    for row_y in range(y + h - 1, y - 1, -1):
        row = []
        for col_x in range(x, x + w):
            path = tilemap.cells.get((col_x, row_y))
            row.append("." if (col_x, row_y) not in tilemap.cells else symbols.get(path or "missing"))
        print(f"{row_y:>{label}} | {''.join(row)}")


# ---------- Commands ----------

def cmd_overview(_args):
    for world_path, areas in worlds():
        print(f"World: {world_path}")
        if not areas:
            print("  (no areas)\n")
            continue
        labels = {}
        for i, area in enumerate(areas):
            labels[area.path] = AUTO_SYMBOLS[26 + i] if i < 26 else "?"
            print(f"  {labels[area.path]}  {area.id:<16} bounds {area.x} {area.y} {area.w} {area.h}   {area.scene}")

        x_min = min(a.x for a in areas)
        y_min = min(a.y for a in areas)
        x_max = max(a.x_max for a in areas)
        y_max = max(a.y_max for a in areas)
        # Characters are about twice as tall as wide, so rows cover twice as many tiles.
        scale = max(1, math.ceil((x_max - x_min) / 100))
        print(f"\n  Map: one column = {scale} tiles, one row = {scale * 2} tiles, top is y {y_max}")
        for row_y in range(y_max - 1, y_min - 1, -scale * 2):
            row = []
            for col_x in range(x_min, x_max, scale):
                inside = [a for a in areas if a.x <= col_x < a.x_max and a.y <= row_y < a.y_max]
                row.append(labels[inside[0].path] if len(inside) == 1 else "#" if inside else " ")
            print("  " + "".join(row).rstrip())
        print()


def find_area(area_id):
    for area in all_areas():
        if area.id == area_id:
            return area
    sys.exit(f"No area with ID '{area_id}'. Run 'overview' for the list.")


def cmd_area(args):
    area = find_area(args.id)
    symbols = Symbols(args.legend)
    print(f"Area {area.id}: bounds {area.x} {area.y} {area.w} {area.h}, scene {area.scene}\n")
    for tilemap in scene_tilemaps(area.scene):
        outside = sum(1 for (x, y) in tilemap.cells if not (area.x <= x < area.x_max and area.y <= y < area.y_max))
        note = f", {outside} outside the bounds" if outside else ""
        print(f"[{tilemap.name}] {len(tilemap.cells)} tiles{note}")
        print_grid(tilemap, area.x, area.y, area.w, area.h, symbols)
        print()
    symbols.print_key()


def cmd_edges(args):
    x, y, w, h = args.bounds
    depth = args.depth
    symbols = Symbols(args.legend)
    found = False

    for area in all_areas():
        # The strip of the neighbour that lies along the shared edge.
        if area.x_max == x or area.x == x + w:
            y0, y1 = max(y, area.y), min(y + h, area.y_max)
            if y0 >= y1:
                continue
            side = "left" if area.x_max == x else "right"
            sx = area.x_max - depth if side == "left" else area.x
            strip = (sx, y0, depth, y1 - y0)
        elif area.y_max == y or area.y == y + h:
            x0, x1 = max(x, area.x), min(x + w, area.x_max)
            if x0 >= x1:
                continue
            side = "below" if area.y_max == y else "above"
            sy = area.y_max - depth if side == "below" else area.y
            strip = (x0, sy, x1 - x0, depth)
        else:
            continue

        found = True
        print(f"== {area.id}, {side} the new bounds: strip {strip[0]} {strip[1]} {strip[2]} {strip[3]}")
        for tilemap in scene_tilemaps(area.scene):
            if any(strip[0] <= cx < strip[0] + strip[2] and strip[1] <= cy < strip[1] + strip[3] for cx, cy in tilemap.cells):
                print(f"[{tilemap.name}]")
                print_grid(tilemap, *strip, symbols)
        print()

    if not found:
        print("No areas touch those bounds.")
    for area in all_areas():
        if area.x < x + w and x < area.x_max and area.y < y + h and y < area.y_max:
            print(f"Warning: the bounds overlap area '{area.id}'.")
    symbols.print_key()


def cmd_tiles(args):
    folder = os.path.join(ASSETS, args.folder.replace("Assets/", "", 1) if args.folder.startswith("Assets/") else args.folder)
    if not os.path.isdir(folder):
        sys.exit(f"No folder at {folder}")
    rows = []
    for current, _, files in os.walk(folder):
        for name in sorted(files):
            if not name.endswith(".asset"):
                continue
            path = os.path.relpath(os.path.join(current, name), PROJECT).replace("\\", "/")
            text = read(path)
            script = re.search(r"m_Script: \{fileID: (-?\d+), guid: ([0-9a-f]{32})", text)
            if not script:
                continue
            if script.group(2) == UNITY_TILE_GUID:
                kind = "Tile"
            else:
                kind = class_id(text).split(".")[-1] or os.path.splitext(os.path.basename(guid_to_path().get(script.group(2), "?")))[0]
            if "Tile" not in kind and "m_DefaultSprite" not in text and "m_Sprite" not in text:
                continue
            rows.append((path, kind))
    for path, kind in sorted(rows):
        print(f"{kind:<28} {path}")
    print(f"\n{len(rows)} tiles")


# ---------- Spec export ----------

def sorting_layers():
    """Sorting layer unique ID -> (index from the bottom, name), from the project's tag manager."""
    with open(os.path.join(PROJECT, "ProjectSettings", "TagManager.asset"), encoding="utf-8") as f:
        text = f.read()
    block = text.split("m_SortingLayers:", 1)[1] if "m_SortingLayers:" in text else ""
    layers = {}
    for i, match in enumerate(re.finditer(r"- name: (.*)\n\s+uniqueID: (-?\d+)", block)):
        layers[int(match.group(2))] = (i, match.group(1).strip())
    return layers


def png_size(path):
    with open(os.path.join(PROJECT, path), "rb") as f:
        header = f.read(24)
    if header[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return int.from_bytes(header[16:20], "big"), int.from_bytes(header[20:24], "big")


def sprite_size_in_tiles(tile_path):
    """(width, height) of a tile's sprite in tiles, or None when it can't be read from text assets."""
    try:
        text = read(tile_path)
    except OSError:
        return None
    ref = re.search(r"m_(?:Default)?Sprite: \{fileID: (-?\d+), guid: ([0-9a-f]{32})", text)
    if not ref:
        return None
    file_id, guid = ref.groups()
    texture = guid_to_path().get(guid)
    if not texture:
        return None
    try:
        meta = read(texture + ".meta")
    except OSError:
        return None
    ppu = re.search(r"spritePixelsToUnits: ([\d.]+)", meta)
    if not ppu or float(ppu.group(1)) <= 0:
        return None
    ppu = float(ppu.group(1))

    if file_id == SINGLE_SPRITE_FILE_ID:
        size = png_size(texture) if texture.lower().endswith(".png") else None
    else:
        size = None
        for chunk in meta.split("\n    - serializedVersion: ")[1:]:
            if re.search(r"internalID: " + re.escape(file_id) + r"\b", chunk):
                rect = re.search(r"rect:\s+serializedVersion: \d+\s+x: \S+\s+y: \S+\s+width: ([\d.]+)\s+height: ([\d.]+)", chunk)
                if rect:
                    size = (float(rect.group(1)), float(rect.group(2)))
                break
    if not size:
        return None
    return size[0] / ppu, size[1] / ppu


def pick_symbol(tile_path, used):
    """A free symbol, from the tile's name where possible: 'g' for Grass, then 'G', then other letters."""
    name = os.path.splitext(os.path.basename(tile_path))[0]
    candidates = []
    for word in re.findall(r"[A-Za-z0-9]+", name):
        if word.lower() not in FILLER_WORDS:
            candidates += [word[0].lower(), word[0].upper()]
    for c in name:
        if c.isalnum():
            candidates += [c.lower(), c.upper()]
    for c in candidates + SPEC_SYMBOLS:
        if c not in used and c not in RESERVED_SYMBOLS:
            used.add(c)
            return c
    return None


def footprint_cells(entry, cell):
    footprint = entry.get("footprint") or {}
    w, h = footprint.get("width", 0), footprint.get("height", 0)
    if not (w or h or footprint.get("x", 0) or footprint.get("y", 0)):
        return [cell]
    x0, y0 = cell[0] + footprint.get("x", 0), cell[1] + footprint.get("y", 0)
    return [(x, y) for x in range(x0, x0 + w) for y in range(y0, y0 + h)]


def short(path):
    return os.path.splitext(os.path.basename(path))[0] if path else "(none)"


def read_spec_headers(path):
    """The leading comment lines and the 'key: value' headers of an existing spec, or ([], {})."""
    comments, headers = [], {}
    if not os.path.exists(path):
        return comments, headers
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if line.startswith("["):
                break
            if line.startswith("#"):
                if not headers:
                    comments.append(line)
            elif ":" in line:
                key, value = line.split(":", 1)
                headers[key.strip().lower()] = value.strip()
    return comments, headers


def load_legend(name):
    path = os.path.join(LEGENDS, name + ".json")
    if not os.path.exists(path):
        sys.exit(f"No legend at {path}")
    with open(path, encoding="utf-8") as f:
        legend = json.load(f)
    legend.setdefault("layers", [])
    legend.setdefault("entries", [])
    return legend


def format_legend(legend):
    """JSON in the hand-written legends' style: one layer or entry per line, grouped by layer."""
    def line(item):
        return "{ " + ", ".join(f"{json.dumps(k)}: {json.dumps(v)}" for k, v in item.items()) + " }"

    layers = ["        " + line(layer) for layer in legend["layers"]]
    entries = []
    previous = None
    for entry in legend["entries"]:
        if previous is not None and entry["layer"] != previous:
            entries.append("")
        entries.append("        " + line(entry))
        previous = entry["layer"]
    # Commas go on entry lines only, so the blank lines between layers stay valid JSON.
    last = max((i for i, e in enumerate(entries) if e), default=-1)
    entries = [e + ("," if e and i != last else "") for i, e in enumerate(entries)]

    out = ["{", '    "layers": [', ",\n".join(layers), "    ],", '    "entries": [']
    out += entries
    out += ["    ]", "}"]
    return "\n".join(out) + "\n"


def cmd_spec(args):
    area = find_area(args.id)
    world = next((w for w, areas in worlds() if any(a.path == area.path for a in areas)), None)
    tilemaps = scene_tilemaps(area.scene)
    sorting = sorting_layers()
    warnings = []

    def in_bounds(cell):
        return area.x <= cell[0] < area.x_max and area.y <= cell[1] < area.y_max

    # Tilemaps the spec can address.
    logic_maps = [t for t in tilemaps if t.is_logic]
    art = [t for t in tilemaps if not t.is_logic]
    problems = [f"tilemap '{t.name}' isn't at the origin, so its cells aren't world tiles." for t in tilemaps if not t.at_origin]
    names = [t.name for t in art]
    problems += [f"two tilemaps are named '{n}'. Rename one, since specs find tilemaps by name." for n in sorted(set(names)) if names.count(n) > 1]
    if not logic_maps:
        problems.append("the scene has no tilemap with a LogicLayerBehaviour.")
    if problems:
        sys.exit("Can't export:\n  " + "\n  ".join(problems))
    if len(logic_maps) > 1:
        warnings.append(f"{len(logic_maps)} logic layers. The spec shows '{logic_maps[0].name}', and applying writes to it.")
    logic = logic_maps[0]
    # Bottom to top, which is also the legend's layer order and so which logic wins.
    art.sort(key=lambda t: (sorting.get(t.sorting_layer_id, (0, ""))[0], t.sorting_order))

    if args.region:
        gx, gy, gw, gh = args.region
        if gw <= 0 or gh <= 0 or gx < area.x or gy < area.y or gx + gw > area.x_max or gy + gh > area.y_max:
            sys.exit(f"The region must be inside the bounds {area.x} {area.y} {area.w} {area.h}.")
    else:
        gx, gy, gw, gh = area.x, area.y, area.w, area.h

    # Which legend to build on: --legend, then the one the existing spec names, then a new one.
    spec_path = os.path.join(AREAS, area.id + ".txt")
    comments, old_headers = read_spec_headers(spec_path)
    if args.write_legend and not args.legend:
        sys.exit("--write-legend needs --legend NAME, the legend to add entries to.")
    base_name = args.legend or old_headers.get("legend")
    if base_name and not os.path.exists(os.path.join(LEGENDS, base_name + ".json")):
        if args.legend:
            sys.exit(f"No legend '{base_name}' in {LEGENDS}")
        base_name = None
    legend = load_legend(base_name) if base_name else {"layers": [], "entries": []}
    base_layer_names = [layer["name"] for layer in legend["layers"]]

    # Layers: the scene's art tilemaps in sorting order, then any the legend has that the scene doesn't.
    base_layers = {layer["name"]: layer for layer in legend["layers"]}
    layers = []
    for t in art:
        sorting_name = sorting.get(t.sorting_layer_id, (0, "Default"))[1]
        layer = dict(base_layers.get(t.name, {"name": t.name, "sortingLayer": sorting_name}))
        if layer["sortingLayer"] != sorting_name:
            warnings.append(f"layer '{t.name}' is on sorting layer '{sorting_name}' in the scene, but '{layer['sortingLayer']}' in the legend.")
        layers.append(layer)
    scene_order = [layer["name"] for layer in layers]
    shared = [n for n in base_layer_names if n in scene_order]
    if shared != [n for n in scene_order if n in shared]:
        warnings.append("the legend lists layers in a different order from the scene's sorting, so logic may resolve differently.")
    layers += [layer for layer in legend["layers"] if layer["name"] not in scene_order]

    entries = list(legend["entries"])
    layer_tile2entry = {}
    for entry in entries:
        layer_tile2entry.setdefault((entry["layer"], entry["tile"]), entry)
    used = {entry["symbol"] for entry in entries}
    added = []

    # Symbols go to the bottom layer first, most used tiles first, so ground gets the obvious letters.
    tile_counts = {}
    for t in art:
        counts = {}
        for cell, tile in t.cells.items():
            if tile and in_bounds(cell) and not tile.startswith("missing:"):
                counts[tile] = counts.get(tile, 0) + 1
        tile_counts[t.name] = counts
    new_symbols = {}
    for t in art:
        counts = tile_counts[t.name]
        for tile in sorted(counts, key=lambda p: (-counts[p], p)):
            if (t.name, tile) not in layer_tile2entry:
                new_symbols[(t.name, tile)] = pick_symbol(tile, used)

    # Logic is inferred top layer first: a cell already explained by a higher layer's logic is skipped.
    covered = set()
    for t in reversed(art):
        counts = tile_counts[t.name]
        for tile in sorted(counts, key=lambda p: (-counts[p], p)):
            if (t.name, tile) in layer_tile2entry:
                continue
            symbol = new_symbols[(t.name, tile)]
            if symbol is None:
                warnings.append(f"no symbols left for {tile} on [{t.name}]; its cells export as '?'.")
                continue
            entry = {"symbol": symbol, "layer": t.name, "tile": tile}

            under = {}
            for cell, cell_tile in t.cells.items():
                if cell_tile == tile and in_bounds(cell) and cell not in covered:
                    under_tile = logic.cells.get(cell)
                    under[under_tile] = under.get(under_tile, 0) + 1
            total = sum(under.values())
            if total:
                best = max(under, key=lambda p: (under[p], p is not None))
                share = under[best] / total
                if best is not None and share > LOGIC_AGREEMENT:
                    entry["logic"] = best
                    if share < 1:
                        warnings.append(f"'{symbol}' ({short(tile)}): logic {short(best)} on {share:.0%} of its cells. The rest are hand-painted.")
                else:
                    mix = ", ".join(f"{short(p)} {n}" for p, n in sorted(under.items(), key=lambda i: -i[1]))
                    warnings.append(f"'{symbol}' ({short(tile)}): no logic, since its cells disagree ({mix}).")

            size = sprite_size_in_tiles(tile)
            if size and (size[0] > 1.01 or size[1] > 1.01):
                warnings.append(f"'{symbol}' ({short(tile)}): its sprite is {size[0]:g}x{size[1]:g} tiles. Add a footprint if its logic should cover more than its cell.")

            entries.append(entry)
            layer_tile2entry[(t.name, tile)] = entry
            added.append(entry)

        for cell, tile in t.cells.items():
            entry = layer_tile2entry.get((t.name, tile))
            if entry and entry.get("logic"):
                covered.update(footprint_cells(entry, cell))

    # Entries grouped by layer, in layer order, keeping each layer's existing order.
    layer_index = {layer["name"]: i for i, layer in enumerate(layers)}
    entries.sort(key=lambda e: layer_index.get(e["layer"], len(layers)))
    legend = {"layers": layers, "entries": entries}

    if args.write_legend:
        legend_name = base_name
    elif base_name and not added and [l["name"] for l in layers] == base_layer_names:
        legend_name = base_name
    else:
        legend_name = area.id
    legend_written = legend_name != base_name or args.write_legend
    if legend_written:
        os.makedirs(LEGENDS, exist_ok=True)
        with open(os.path.join(LEGENDS, legend_name + ".json"), "w", encoding="utf-8", newline="\n") as f:
            f.write(format_legend(legend))

    # The spec.
    lines = comments or [f"# Exported from {area.scene}."]
    lines.append("# Edit, then apply with Grid World > Apply Spec to Area. '?' keeps a cell as it is, and '.' clears it.")
    lines += [f"id: {area.id}", f"world: {world}", f"bounds: {area.x} {area.y} {area.w} {area.h}"]
    if args.region:
        lines.append(f"region: {gx} {gy} {gw} {gh}")
    lines += [f"legend: {legend_name}", ""]

    lines.append("# Key:")
    for entry in entries:
        logic_note = f"   logic {short(entry['logic'])}" if entry.get("logic") else ""
        lines.append(f"#   [{entry['layer']}] {entry['symbol']} = {entry['tile']}{logic_note}")

    logic_symbols = {}
    for tile in sorted({p for p in logic.cells.values() if p}):
        logic_symbols[tile] = SPEC_SYMBOLS[len(logic_symbols)] if len(logic_symbols) < len(SPEC_SYMBOLS) else "?"
    lines.append("#")
    lines.append("# Logic, for reference only. Applying works it out from the legend. '.' is unpainted, so blocked.")
    for tile, symbol in logic_symbols.items():
        lines.append(f"#   {symbol} = {tile}")
    for row_y in range(gy + gh - 1, gy - 1, -1):
        row = "".join("." if (x, row_y) not in logic.cells else logic_symbols.get(logic.cells[(x, row_y)], "?") for x in range(gx, gx + gw))
        lines.append("#   " + row)

    for t in art:
        outside = sum(1 for cell in t.cells if not in_bounds(cell))
        lines.append("")
        if outside:
            lines.append(f"# {outside} tiles outside the bounds aren't shown, and applying never touches them.")
        lines.append(f"[{t.name}]")
        for row_y in range(gy + gh - 1, gy - 1, -1):
            row = []
            for x in range(gx, gx + gw):
                if (x, row_y) not in t.cells:
                    row.append(".")
                    continue
                entry = layer_tile2entry.get((t.name, t.cells[(x, row_y)]))
                row.append(entry["symbol"] if entry else "?")
            lines.append("".join(row))

    os.makedirs(AREAS, exist_ok=True)
    existed = os.path.exists(spec_path)
    with open(spec_path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")

    def rel(path):
        return os.path.relpath(path, REPO).replace("\\", "/")

    print(f"{'Overwrote' if existed else 'Wrote'} {rel(spec_path)}")
    if legend_written:
        note = f", {len(added)} new entries" if added else ""
        print(f"Wrote {rel(os.path.join(LEGENDS, legend_name + '.json'))}{note}")
    else:
        print(f"Using legend '{legend_name}', unchanged")
    for entry in added:
        logic_note = f"   logic {short(entry['logic'])}" if entry.get("logic") else "   no logic"
        print(f"  [{entry['layer']}] {entry['symbol']} = {entry['tile']}{logic_note}")
    if warnings:
        print("Warnings:")
        for warning in warnings:
            print("  " + warning)


# ---------- Features ----------

SCRIPTS = os.path.join(ASSETS, "2. Scripts")
GRID_CLASS = "156049354"
PREFAB_INSTANCE_CLASS = "1001"
SCENE_ROOTS_CLASS = "1660057539"
IDENTIFIED_BASE = "IdentifiedFeatureBehaviour"
FEATURE_ID_FORMAT = re.compile(r"^[A-Za-z0-9_-]+$")
VECTOR_TYPES = {"Vector2Int", "Vector3Int", "Vector2", "Vector3"}
RECT_TYPES = {"RectInt", "Rect"}
# Fields every MonoBehaviour has, which aren't the component's own settings.
UNITY_FIELDS = {"m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
                "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name", "m_EditorClassIdentifier"}


class CsType:
    def __init__(self, name, kind, base):
        self.name = name
        self.kind = kind  # "class", "struct" or "enum"
        self.base = base
        self.fields = {}  # serialized field name -> declared type
        self.values = []  # enum member names, indexed by value


_cs_types = None


def strip_code(text):
    """C# source without comments, strings or char literals, so braces and semicolons can be counted."""
    text = re.sub(r"//[^\n]*|/\*.*?\*/", "", text, flags=re.S)
    text = re.sub(r'@"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\\n])*"', '""', text)
    return re.sub(r"'(?:\\.|[^'\\\n])'", "''", text)


def matching_brace(text, start):
    depth = 0
    for i in range(start, len(text)):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return i
    return len(text)


def body_statements(text, start):
    """The statements directly inside the braces opening at text[start], split at ';', '{' and '}'."""
    depth, current, statements = 0, [], []
    for i in range(start, len(text)):
        c = text[i]
        if c == "{":
            depth += 1
            if depth == 2:
                statements.append("".join(current))
                current = []
        elif c == "}":
            depth -= 1
            if depth == 0:
                break
        elif depth == 1:
            if c == ";":
                statements.append("".join(current))
                current = []
            else:
                current.append(c)
    return statements


def parse_field(statement):
    """(type, name) of a serialized field declaration, or None for anything else."""
    # Attributes start with a name, unlike an array type's '[]'.
    without_attributes = re.sub(r"\[\s*[A-Za-z][^\]]*\]", " ", statement)
    if "NonSerialized" in statement or "(" in without_attributes or "=>" in without_attributes:
        return None
    words = without_attributes.split("=", 1)[0].split()
    if "static" in words or "const" in words or "readonly" in words:
        return None
    serialized = "SerializeField" in statement or "public" in words
    words = [w for w in words if w not in ("public", "private", "protected", "internal", "new", "volatile")]
    if not serialized or len(words) != 2:
        return None
    return words[0], words[1]


def cs_types():
    """Every class, struct and enum declared under 2. Scripts, by name and by 'Outer.Name'."""
    global _cs_types
    if _cs_types is not None:
        return _cs_types
    _cs_types = {}
    declaration = re.compile(r"\b(class|struct|enum)\s+(\w+)(?:<[^>{]*>)?\s*(?::\s*([\w.<>, ]+?))?\s*(?:where\b[^{]*)?\{")
    for folder, _, files in os.walk(SCRIPTS):
        for name in files:
            if not name.endswith(".cs"):
                continue
            with open(os.path.join(folder, name), encoding="utf-8", errors="replace") as f:
                text = strip_code(f.read())
            enclosing = []  # (closing brace offset, name) of the types around the current position
            for match in declaration.finditer(text):
                kind, type_name, bases = match.groups()
                brace = match.end() - 1
                enclosing = [t for t in enclosing if t[0] > brace]
                outer = enclosing[-1][1] if enclosing else None
                base = None
                if bases and kind != "enum":
                    base = re.sub(r"<.*", "", bases.split(",")[0].strip()).split(".")[-1]
                cs_type = CsType(type_name, kind, base)
                end = matching_brace(text, brace)
                enclosing.append((end, type_name))

                if kind == "enum":
                    value = -1
                    for item in text[brace + 1:end].split(","):
                        item_name, _, explicit = item.strip().partition("=")
                        if not item_name.strip():
                            continue
                        explicit = explicit.strip()
                        value = int(explicit) if re.match(r"^-?\d+$", explicit) else value + 1
                        while len(cs_type.values) <= value:
                            cs_type.values.append(None)
                        cs_type.values[value] = item_name.strip()
                else:
                    for statement in body_statements(text, brace):
                        parsed = parse_field(statement)
                        if parsed:
                            cs_type.fields[parsed[1]] = parsed[0]

                _cs_types.setdefault(type_name, cs_type)
                if outer:
                    _cs_types[outer + "." + type_name] = cs_type
    return _cs_types


def field_types(class_name):
    """Serialized field name -> declared type, for a class and its bases."""
    chain = []
    cs_type = cs_types().get(class_name)
    while cs_type and cs_type.kind != "enum" and len(chain) < 20:
        chain.append(cs_type)
        cs_type = cs_types().get(cs_type.base) if cs_type.base else None
    result = {}
    for cs_type in reversed(chain):
        result.update(cs_type.fields)
    return result


def derives_from(class_name, base_name):
    cs_type = cs_types().get(class_name)
    for _ in range(20):
        if not cs_type or not cs_type.base:
            return False
        if cs_type.base == base_name:
            return True
        cs_type = cs_types().get(cs_type.base)
    return False


# Unity YAML: 'key: value', indented blocks, and '- ' list items at their key's own indent.

def indent_of(line):
    return len(line) - len(line.lstrip(" "))


def parse_scalar(text):
    if text == "[]":
        return []
    if text.startswith("{") and text.endswith("}"):
        return {k: v.strip() for k, v in re.findall(r"(\w+): ([^,}]*)", text)}
    return text


def parse_mapping(lines, i, indent):
    result = {}
    while i < len(lines):
        line = lines[i]
        if indent_of(line) != indent or line.lstrip().startswith("- "):
            break
        key, _, rest = line.strip().partition(":")
        rest = rest.strip()
        i += 1
        if rest:
            result[key] = parse_scalar(rest)
        elif i < len(lines) and indent_of(lines[i]) == indent and lines[i].lstrip().startswith("- "):
            result[key], i = parse_list(lines, i, indent)
        elif i < len(lines) and indent_of(lines[i]) > indent:
            result[key], i = parse_mapping(lines, i, indent_of(lines[i]))
        else:
            result[key] = ""
    return result, i


def parse_list(lines, i, indent):
    items = []
    while i < len(lines) and indent_of(lines[i]) == indent and lines[i].lstrip().startswith("- "):
        first = lines[i].lstrip()[2:]
        i += 1
        if re.match(r"^\w+:( |$)", first):
            block = [" " * (indent + 2) + first]
            while i < len(lines) and indent_of(lines[i]) > indent:
                block.append(lines[i])
                i += 1
            items.append(parse_mapping(block, 0, indent + 2)[0])
        else:
            items.append(parse_scalar(first))
    return items, i


def parse_doc(body):
    """A scene document's fields, skipping its '--- !u!' header and type name lines."""
    lines = [line for line in body.split("\n")[2:] if line.strip()]
    return parse_mapping(lines, 0, 2)[0]


def file_id(value):
    return value.get("fileID") if isinstance(value, dict) else None


class SceneObject:
    def __init__(self, object_id, body):
        self.object_id = object_id
        self.name = field(body, "m_Name") or ""
        self.active = field(body, "m_IsActive") != "0"
        self.components = re.findall(r"- component: \{fileID: (-?\d+)\}", body)
        self.transform = None


class Scene:
    """The GameObjects, transforms and MonoBehaviours of a scene file."""

    def __init__(self, path):
        text = read(path)
        starts = [(m.start(), m.group(1), m.group(2)) for m in DOC_HEADER.finditer(text)]
        self.objects = {}
        self.transforms = {}
        self.behaviours = {}
        self.grid_cell_sizes = []
        self.prefab_instances = 0
        self.roots = []
        for n, (start, cls, doc_id) in enumerate(starts):
            end = starts[n + 1][0] if n + 1 < len(starts) else len(text)
            body = text[start:end]
            if cls == PREFAB_INSTANCE_CLASS:
                self.prefab_instances += 1
            elif cls == GAMEOBJECT_CLASS:
                self.objects[doc_id] = SceneObject(doc_id, body)
            elif cls == TRANSFORM_CLASS:
                self.transforms[doc_id] = parse_doc(body)
            elif cls == MONOBEHAVIOUR_CLASS:
                self.behaviours[doc_id] = parse_doc(body)
            elif cls == GRID_CLASS:
                self.grid_cell_sizes.append(parse_doc(body).get("m_CellSize"))
            elif cls == SCENE_ROOTS_CLASS:
                self.roots = re.findall(r"- \{fileID: (-?\d+)\}", body)
        for transform_id, transform in self.transforms.items():
            owner = self.objects.get(file_id(transform.get("m_GameObject")))
            if owner:
                owner.transform = transform_id

    def owner(self, transform_id):
        return self.objects.get(file_id(self.transforms[transform_id].get("m_GameObject")))

    def parent(self, transform_id):
        return file_id(self.transforms[transform_id].get("m_Father"))

    def chain(self, obj):
        """The object's transform and its ancestors' transforms, innermost first."""
        result = []
        transform_id = obj.transform
        while transform_id in self.transforms and len(result) < 1000:
            result.append(transform_id)
            transform_id = self.parent(transform_id)
        return result

    def hierarchy(self):
        """GameObjects depth first, in hierarchy order."""
        order = []

        def visit(transform_id):
            if transform_id not in self.transforms:
                return
            obj = self.owner(transform_id)
            if obj:
                order.append(obj)
            for child in self.transforms[transform_id].get("m_Children", []):
                visit(file_id(child))

        roots = self.roots or [t for t in self.transforms if self.parent(t) == "0"]
        for root in roots:
            visit(root)
        return order

    def path_of(self, obj):
        return "/".join(reversed([self.owner(t).name for t in self.chain(obj) if self.owner(t)]))

    def active_in_hierarchy(self, obj):
        return all(self.owner(t) is None or self.owner(t).active for t in self.chain(obj))

    def world_position(self, obj):
        """The world position, applying each parent's position, rotation and scale."""
        position = (0.0, 0.0, 0.0)
        for transform_id in self.chain(obj):
            transform = self.transforms[transform_id]
            local = vector(transform.get("m_LocalPosition"))
            scale = vector(transform.get("m_LocalScale"), 1.0)
            rotation = transform.get("m_LocalRotation")
            rotation = rotation if isinstance(rotation, dict) else {}
            q = tuple(float(rotation.get(k, d)) for k, d in (("x", 0), ("y", 0), ("z", 0), ("w", 1)))
            scaled = tuple(p * s for p, s in zip(position, scale))
            position = tuple(a + b for a, b in zip(rotate(q, scaled), local))
        return position


def vector(value, default=0.0):
    value = value if isinstance(value, dict) else {}
    return tuple(float(value.get(k, default)) for k in ("x", "y", "z"))


def rotate(q, v):
    """v rotated by the quaternion q, as v + 2w(u x v) + 2u x (u x v), where u is q's vector part."""
    x, y, z, w = q
    vx, vy, vz = v
    cx, cy, cz = y * vz - z * vy, z * vx - x * vz, x * vy - y * vx
    ccx, ccy, ccz = y * cz - z * cy, z * cx - x * cz, x * cy - y * cx
    return (vx + 2 * (w * cx + ccx), vy + 2 * (w * cy + ccy), vz + 2 * (w * cz + ccz))


def script_class(behaviour):
    """A MonoBehaviour's class name, and whether its script is in 2. Scripts."""
    guid = (behaviour.get("m_Script") or {}).get("guid", "")
    path = guid_to_path().get(guid, "")
    identifier = behaviour.get("m_EditorClassIdentifier") or ""
    name = identifier.split("::")[-1].split(".")[-1] if identifier else os.path.splitext(os.path.basename(path))[0]
    return name, path.startswith("Assets/2. Scripts/")


def kind_name(class_name):
    """'NPC' for NpcBehaviour and 'Spawn Zone' for SpawnZoneBehaviour, matching the Editor."""
    name = re.sub(r"(Behaviour|Data)$", "", class_name)
    name = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", name)
    return "NPC" if name == "Npc" else name


class Feature:
    def __init__(self, scene, obj, behaviour_id, class_name):
        self.scene = scene
        self.obj = obj
        self.behaviour_id = behaviour_id
        self.class_name = class_name
        self.id = unquote(scene.behaviours[behaviour_id].get("id") or "")


def scene_features(scene):
    """The identified features in a scene, in hierarchy order, including inactive ones like the bake."""
    features = []
    for obj in scene.hierarchy():
        for component in obj.components:
            behaviour = scene.behaviours.get(component)
            if behaviour is None:
                continue
            class_name, ours = script_class(behaviour)
            if ours and derives_from(class_name, IDENTIFIED_BASE):
                features.append(Feature(scene, obj, component, class_name))
    return features


def unquote(text):
    if len(text) >= 2 and text[0] == text[-1] and text[0] in "'\"":
        return text[1:-1]
    return text


def quote(text):
    text = unquote(text)
    return f'"{text}"' if text == "" or " " in text or '"' in text else text


def reference_text(value):
    if value.get("fileID") == "0":
        return "none"
    guid = value.get("guid")
    if not guid:
        return f"scene object {value.get('fileID')}"
    path = guid_to_path().get(guid)
    return quote(path) if path else f"missing {guid}"


def format_value(value, type_name, path, out):
    """Appends (path, text) for a serialized value, using SerializedProperty paths such as points[1].tile."""
    type_name = (type_name or "").replace("global::", "")
    list_type = re.match(r"^List<(.+)>$", type_name)
    element = type_name[:-2] if type_name.endswith("[]") else list_type.group(1) if list_type else None

    if isinstance(value, list) or element is not None:
        items = value if isinstance(value, list) else []
        out.append((path + ".size", str(len(items))))
        for i, item in enumerate(items):
            format_value(item, element, f"{path}[{i}]", out)
        return

    short_type = type_name.split(".")[-1]
    cs_type = cs_types().get(type_name) or cs_types().get(short_type)
    if isinstance(value, dict):
        if "fileID" in value:
            out.append((path, reference_text(value)))
        elif short_type in RECT_TYPES or set(value) == {"x", "y", "width", "height"}:
            out.append((path, " ".join(value.get(k, "0") for k in ("x", "y", "width", "height"))))
        elif short_type in VECTOR_TYPES or (value and set(value) <= {"x", "y", "z", "w"}):
            out.append((path, " ".join(value[k] for k in ("x", "y", "z", "w") if k in value)))
        else:
            nested = field_types(cs_type.name) if cs_type and cs_type.kind != "enum" else {}
            for key, item in value.items():
                format_value(item, nested.get(key), f"{path}.{key}", out)
        return

    if cs_type and cs_type.kind == "enum" and re.match(r"^-?\d+$", value):
        index = int(value)
        name = cs_type.values[index] if 0 <= index < len(cs_type.values) else None
        out.append((path, name or value))
    elif short_type == "bool":
        out.append((path, {"1": "true", "0": "false"}.get(value, value)))
    elif short_type == "string":
        out.append((path, quote(value)))
    else:
        out.append((path, value))


def component_lines(behaviour, class_name):
    out = []
    types = field_types(class_name)
    for key, value in behaviour.items():
        if key not in UNITY_FIELDS:
            format_value(value, types.get(key), key, out)
    # A field added after the scene was last saved isn't in the file until Unity saves it again.
    for key in types:
        if key not in behaviour:
            out.append((key, "(not saved yet, so its default)"))
    return out


def area_tile_notes(class_name, lines, tile):
    """Area tiles for values stored as offsets from the feature's tile, keyed by path."""
    relative = dict(lines).get("regionSpace") == "Relative"
    notes = {}
    for path, text in lines:
        numbers = text.split()
        if not numbers or not all(re.match(r"^-?\d+$", n) for n in numbers):
            continue
        is_region = path == "region" and relative and len(numbers) == 4
        is_patrol = class_name == "PatrolAIBehaviour" and re.match(r"^points\[\d+\]\.tile$", path) and len(numbers) == 2
        if is_region or is_patrol:
            x, y = int(numbers[0]) + tile[0], int(numbers[1]) + tile[1]
            notes[path] = f"area {x} {y}" + (f" {numbers[2]} {numbers[3]}" if is_region else "")
    return notes


def other_areas_features(area):
    """(area, Feature) for the other areas in every world that holds this one."""
    result = []
    seen = {area.path}
    for _, areas in worlds():
        if not any(a.path == area.path for a in areas):
            continue
        for other in areas:
            if other.path in seen or not other.scene:
                continue
            seen.add(other.path)
            try:
                scene = Scene(other.scene)
            except OSError:
                continue
            result += [(other, f) for f in scene_features(scene)]
    return result


def feature_tile(scene, feature):
    # Rounded first, so a rotated parent's float error doesn't drop 10 to 9.9999999 and floor to 9.
    position = scene.world_position(feature.obj)
    return math.floor(round(position[0], 4)), math.floor(round(position[1], 4))


def cmd_features(args):
    area = find_area(args.id)
    scene = Scene(area.scene)
    features = scene_features(scene)
    print(f"Area {area.id}: scene {area.scene}")
    print("Tiles are area-local, as in the scene. The field names are the paths that 'set' edits use.")
    if any(vector(size, 1.0)[:2] != (1.0, 1.0) for size in scene.grid_cell_sizes):
        print("Warning: a grid's cells aren't 1x1, so these tiles may not match the bake.")
    if scene.prefab_instances:
        print(f"Warning: {scene.prefab_instances} prefab instances aren't read, so features inside them aren't listed.")
    print()

    others = other_areas_features(area)
    with_problems = 0
    for feature in features:
        tile = feature_tile(scene, feature)
        flags = []
        if not feature.id:
            flags.append("no ID")
        elif not FEATURE_ID_FORMAT.match(feature.id):
            flags.append("bad ID: use only letters, digits, '_' and '-'")
        else:
            flags += [f"duplicate ID, also on '{scene.path_of(f.obj)}'" for f in features if f is not feature and f.id.lower() == feature.id.lower()]
            flags += [f"duplicate ID, also in area '{a.id}'" for a, f in others if f.id.lower() == feature.id.lower()]
        if flags:
            with_problems += 1
        if not scene.active_in_hierarchy(feature.obj):
            flags.append("inactive, but still baked")

        print(f"{kind_name(feature.class_name)}  {feature.id or '(no ID)'}")
        print(f"  object  {quote(scene.path_of(feature.obj))}")
        print(f"  tile    {tile[0]} {tile[1]}")
        for flag in flags:
            print(f"  !       {flag}")

        # The feature's own component first, then the object's other project components in order.
        components = [feature.behaviour_id] + [c for c in feature.obj.components if c != feature.behaviour_id]
        for component in components:
            behaviour = scene.behaviours.get(component)
            if behaviour is None:
                continue
            class_name, ours = script_class(behaviour)
            if not ours:
                continue
            print(f"  {class_name}{'  (disabled)' if behaviour.get('m_Enabled') == '0' else ''}")
            lines = component_lines(behaviour, class_name)
            if component == feature.behaviour_id:
                lines = [(path, text) for path, text in lines if path != "id"]
            notes = area_tile_notes(class_name, lines, tile)
            width = max((len(path) for path, _ in lines), default=0)
            for path, text in lines:
                note = f"   ({notes[path]})" if path in notes else ""
                print(f"    {path:<{width}}  {text}{note}")
        print()

    print(f"{len(features)} features" + (f", {with_problems} with ID problems" if with_problems else ""))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)

    commands.add_parser("overview", help="every world's areas and a small map of them")

    area = commands.add_parser("area", help="an area's tilemaps as ASCII")
    area.add_argument("id")
    area.add_argument("--legend")

    edges = commands.add_parser("edges", help="neighbour strips along proposed bounds")
    edges.add_argument("--bounds", type=int, nargs=4, required=True, metavar=("X", "Y", "W", "H"))
    edges.add_argument("--depth", type=int, default=3)
    edges.add_argument("--legend")

    tiles = commands.add_parser("tiles", help="tile assets in a folder")
    tiles.add_argument("folder")

    spec = commands.add_parser("spec", help="write an area's spec, and a legend if needed, to Maps/")
    spec.add_argument("id")
    spec.add_argument("--legend", help="legend to reuse; new tiles go into a copy named after the area")
    spec.add_argument("--write-legend", action="store_true", help="add new tiles to --legend itself")
    spec.add_argument("--region", type=int, nargs=4, metavar=("X", "Y", "W", "H"))

    features = commands.add_parser("features", help="an area's identified features and their components' fields")
    features.add_argument("id")

    args = parser.parse_args()
    handlers = {"overview": cmd_overview, "area": cmd_area, "edges": cmd_edges, "tiles": cmd_tiles, "spec": cmd_spec, "features": cmd_features}
    handlers[args.command](args)


if __name__ == "__main__":
    main()
