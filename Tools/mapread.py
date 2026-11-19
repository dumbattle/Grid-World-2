"""Read-only views of Grid World areas, for drafting map specs without the Unity Editor.

Reads the project's text-serialized assets and scenes. Never writes to them.

    py Tools/mapread.py overview
    py Tools/mapread.py area <id> [--legend NAME]
    py Tools/mapread.py edges --bounds X Y W H [--depth N] [--legend NAME]
    py Tools/mapread.py tiles <folder under Assets>

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

TILEMAP_CLASS = "1839735485"
GAMEOBJECT_CLASS = "1"
UNITY_TILE_GUID = "0000000000000000e000000000000000"
DOC_HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)", re.M)
GUID = re.compile(r"guid: ([0-9a-f]{32})")
# Symbols for tiles the legend doesn't cover, avoiding '.', '#' and '['.
AUTO_SYMBOLS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!$%&*+-=?@^~"


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


def scene_tilemaps(scene_path):
    """Every tilemap in a scene, named after its GameObject. Assumes tilemaps sit at the origin."""
    text = read(scene_path)
    starts = [(m.start(), m.group(1), m.group(2)) for m in DOC_HEADER.finditer(text)]
    docs = []
    for i, (start, cls, file_id) in enumerate(starts):
        end = starts[i + 1][0] if i + 1 < len(starts) else len(text)
        docs.append((cls, file_id, text[start:end]))

    names = {}
    for cls, file_id, body in docs:
        if cls == GAMEOBJECT_CLASS:
            names[file_id] = field(body, "m_Name") or ""

    tilemaps = []
    for cls, file_id, body in docs:
        if cls != TILEMAP_CLASS:
            continue
        owner = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
        tilemap = Tilemap(names.get(owner.group(1), "?") if owner else "?")

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

    args = parser.parse_args()
    {"overview": cmd_overview, "area": cmd_area, "edges": cmd_edges, "tiles": cmd_tiles}[args.command](args)


if __name__ == "__main__":
    main()
