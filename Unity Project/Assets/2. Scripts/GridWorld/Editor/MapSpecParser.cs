using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Reads map specs and legends, and checks a spec against its legend. Every problem is added to the
    /// error list with its file and line, so one pass reports them all. Doesn't touch the asset database.
    /// </summary>
    public static class MapSpecParser {
        public const char Empty = '.';
        /// <summary>The tilemap that logic tiles go on. Legends can't use this name.</summary>
        public const string LogicLayer = "Logic";

        // '#' starts a comment line and '[' a layer header, so neither can be a symbol.
        const string ReservedSymbols = ".#[";

        /// <summary>Returns the spec, or null if a required header is missing.</summary>
        public static MapSpec Parse(string text, string source, List<string> errors) {
            var spec = new MapSpec { source = source };
            var lines = text.Replace("\r", "").Split('\n');
            MapSpecLayer layer = null;
            // Seen at all, and seen valid. A bad bounds line is reported on its own line.
            bool sawBounds = false;
            bool hasBounds = false;

            for (int i = 0; i < lines.Length; i++) {
                int lineNumber = i + 1;
                var line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') {
                    continue;
                }

                if (line[0] == '[') {
                    if (line[line.Length - 1] != ']' || line.Length < 3) {
                        errors.Add($"{source}:{lineNumber}: expected a layer header like [Ground].");
                        layer = null;
                        continue;
                    }
                    var name = line.Substring(1, line.Length - 2).Trim();
                    if (spec.layers.Exists(l => l.name == name)) {
                        errors.Add($"{source}:{lineNumber}: layer [{name}] appears twice.");
                    }
                    layer = new MapSpecLayer { name = name, line = lineNumber };
                    spec.layers.Add(layer);
                    continue;
                }

                if (layer != null) {
                    layer.rows.Add(line);
                    layer.rowLines.Add(lineNumber);
                    continue;
                }

                if (spec.layers.Count > 0) {
                    // A bad layer header came before this, and its error is already reported.
                    continue;
                }

                bool isBounds = line.StartsWith("bounds", StringComparison.OrdinalIgnoreCase);
                sawBounds |= isBounds;
                if (ReadHeader(spec, line, source, lineNumber, errors)) {
                    hasBounds |= isBounds;
                }
            }

            bool complete = true;
            complete &= Require(spec.id, "id", source, errors);
            complete &= Require(spec.world, "world", source, errors);
            complete &= Require(spec.legend, "legend", source, errors);
            if (!sawBounds) {
                errors.Add($"{source}: missing 'bounds: x y width height'.");
            }
            complete &= hasBounds;
            if (spec.layers.Count == 0) {
                errors.Add($"{source}: no layers. Add one with a header like [Ground].");
            }
            return complete ? spec : null;
        }

        /// <summary>Returns the legend, or null if the JSON can't be read at all.</summary>
        public static MapLegend ParseLegend(string json, string source, List<string> errors) {
            MapLegend legend;
            try {
                legend = JsonUtility.FromJson<MapLegend>(json);
            }
            catch (ArgumentException e) {
                errors.Add($"{source}: not valid JSON. {e.Message}");
                return null;
            }
            if (legend == null) {
                errors.Add($"{source}: empty legend.");
                return null;
            }
            legend.layers ??= new MapLegendLayer[0];
            legend.entries ??= new MapLegendEntry[0];

            if (legend.layers.Length == 0) {
                errors.Add($"{source}: no layers.");
            }
            var layerNames = new HashSet<string>();
            foreach (var layer in legend.layers) {
                if (string.IsNullOrWhiteSpace(layer.name)) {
                    errors.Add($"{source}: a layer has no name.");
                    continue;
                }
                if (layer.name == LogicLayer) {
                    errors.Add($"{source}: layer '{LogicLayer}' is reserved for logic tiles. Use an entry's 'logic' field instead.");
                }
                if (!layerNames.Add(layer.name)) {
                    errors.Add($"{source}: layer '{layer.name}' is listed twice.");
                }
                if (string.IsNullOrWhiteSpace(layer.sortingLayer)) {
                    errors.Add($"{source}: layer '{layer.name}' has no sorting layer.");
                }
            }

            var symbols = new HashSet<char>();
            foreach (var entry in legend.entries) {
                if (string.IsNullOrEmpty(entry.symbol) || entry.symbol.Length != 1 || char.IsWhiteSpace(entry.symbol[0])) {
                    errors.Add($"{source}: symbol '{entry.symbol}' must be one character.");
                    continue;
                }
                char symbol = entry.symbol[0];
                if (ReservedSymbols.IndexOf(symbol) >= 0) {
                    errors.Add($"{source}: symbol '{symbol}' is reserved.");
                }
                if (!symbols.Add(symbol)) {
                    errors.Add($"{source}: symbol '{symbol}' is used twice.");
                }
                if (!layerNames.Contains(entry.layer ?? "")) {
                    errors.Add($"{source}: symbol '{symbol}' uses layer '{entry.layer}', which isn't in the layer list.");
                }
                if (string.IsNullOrWhiteSpace(entry.tile)) {
                    errors.Add($"{source}: symbol '{symbol}' has no tile.");
                }
                if (entry.footprint != null && entry.footprint.isSet && (entry.footprint.width < 1 || entry.footprint.height < 1)) {
                    errors.Add($"{source}: symbol '{symbol}' has a footprint with a width or height below 1.");
                }
            }
            return legend;
        }

        /// <summary>Checks layer names, grid sizes, and that every symbol belongs to its layer.</summary>
        public static void CheckAgainstLegend(MapSpec spec, MapLegend legend, List<string> errors) {
            var symbol2entry = SymbolLookup(legend);
            var source = spec.source;

            foreach (var layer in spec.layers) {
                if (layer.name == LogicLayer) {
                    errors.Add($"{source}:{layer.line}: [{LogicLayer}] can't be written directly. It comes from the legend's logic tiles.");
                    continue;
                }
                if (Array.Find(legend.layers, l => l.name == layer.name) == null) {
                    errors.Add($"{source}:{layer.line}: layer [{layer.name}] isn't in legend '{spec.legend}'.");
                    continue;
                }
                if (layer.rows.Count != spec.bounds.height) {
                    errors.Add($"{source}:{layer.line}: [{layer.name}] has {layer.rows.Count} rows, but the bounds are {spec.bounds.height} tall.");
                }

                for (int r = 0; r < layer.rows.Count; r++) {
                    var row = layer.rows[r];
                    int lineNumber = layer.rowLines[r];
                    if (row.Length != spec.bounds.width) {
                        errors.Add($"{source}:{lineNumber}: row is {row.Length} wide, but the bounds are {spec.bounds.width} wide.");
                    }

                    // One message per symbol per row is enough to find it.
                    var reported = new HashSet<char>();
                    foreach (char symbol in row) {
                        if (symbol == Empty || reported.Contains(symbol)) {
                            continue;
                        }
                        if (!symbol2entry.TryGetValue(symbol, out var entry)) {
                            errors.Add($"{source}:{lineNumber}: '{symbol}' isn't in legend '{spec.legend}'.");
                            reported.Add(symbol);
                        }
                        else if (entry.layer != layer.name) {
                            errors.Add($"{source}:{lineNumber}: '{symbol}' belongs to layer [{entry.layer}], not [{layer.name}].");
                            reported.Add(symbol);
                        }
                    }
                }
            }
        }

        internal static Dictionary<char, MapLegendEntry> SymbolLookup(MapLegend legend) {
            var symbol2entry = new Dictionary<char, MapLegendEntry>();
            foreach (var entry in legend.entries) {
                if (!string.IsNullOrEmpty(entry.symbol) && !symbol2entry.ContainsKey(entry.symbol[0])) {
                    symbol2entry.Add(entry.symbol[0], entry);
                }
            }
            return symbol2entry;
        }

        static bool ReadHeader(MapSpec spec, string line, string source, int lineNumber, List<string> errors) {
            int colon = line.IndexOf(':');
            if (colon < 0) {
                errors.Add($"{source}:{lineNumber}: expected 'key: value' before the first layer.");
                return false;
            }
            var key = line.Substring(0, colon).Trim().ToLowerInvariant();
            var value = line.Substring(colon + 1).Trim();

            switch (key) {
                case "id":
                    spec.id = value;
                    return true;
                case "world":
                    spec.world = value;
                    return true;
                case "legend":
                    spec.legend = value;
                    return true;
                case "scenefolder":
                    spec.sceneFolder = value;
                    return true;
                case "datafolder":
                    spec.dataFolder = value;
                    return true;
                case "bounds":
                    if (TryParseBounds(value, out var bounds)) {
                        spec.bounds = bounds;
                        return true;
                    }
                    errors.Add($"{source}:{lineNumber}: bounds should be 'x y width height', with a width and height above 0.");
                    return false;
                default:
                    errors.Add($"{source}:{lineNumber}: unknown header '{key}'.");
                    return false;
            }
        }

        static bool TryParseBounds(string value, out RectInt bounds) {
            bounds = default;
            var parts = value.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) {
                return false;
            }
            var numbers = new int[4];
            for (int i = 0; i < 4; i++) {
                if (!int.TryParse(parts[i], out numbers[i])) {
                    return false;
                }
            }
            if (numbers[2] <= 0 || numbers[3] <= 0) {
                return false;
            }
            bounds = new RectInt(numbers[0], numbers[1], numbers[2], numbers[3]);
            return true;
        }

        static bool Require(string value, string key, string source, List<string> errors) {
            if (string.IsNullOrWhiteSpace(value)) {
                errors.Add($"{source}: missing '{key}:'.");
                return false;
            }
            return true;
        }
    }
}
