using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GridWorld {
    /// <summary>
    /// Reads feature edit files. Every problem is added to the error list with its file and line, so
    /// one pass reports them all. Doesn't touch the scene or the asset database.
    /// </summary>
    /// <example>
    /// area: test_a
    /// world: Assets/8. Data/Manual Test/Manual WorldData.asset
    /// move    guard_1  from 12 5   to 12 6
    /// face    guard_1  from down   to left
    /// set     guard_1  PatrolAIBehaviour.points[1].tile  from 0 3  to 0 4
    /// remove  mob_old  at 4 3
    /// </example>
    public static class FeatureEditParser {
        struct Token {
            public string text;
            public bool quoted;

            public bool Is(string keyword) {
                return !quoted && string.Equals(text, keyword, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Returns the file, or null if a header is missing.</summary>
        public static FeatureEditFile Parse(string text, string source, List<string> errors) {
            var file = new FeatureEditFile { source = source };
            var lines = text.Replace("\r", "").Split('\n');
            // What each feature has edits for, so a field edited twice or an edit after a remove is caught.
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var id2removeLine = new Dictionary<string, int>(FeatureIds.comparer);
            var id2firstLine = new Dictionary<string, int>(FeatureIds.comparer);

            for (int i = 0; i < lines.Length; i++) {
                int lineNumber = i + 1;
                var line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') {
                    continue;
                }
                string where = $"{source}:{lineNumber}";

                if (ReadHeader(file, line, where, errors)) {
                    continue;
                }

                var tokens = Tokenize(line, out string tokenError);
                if (tokenError != null) {
                    errors.Add($"{where}: {tokenError}");
                    continue;
                }
                var edit = ReadEdit(tokens, where, errors);
                if (edit == null) {
                    continue;
                }
                edit.line = lineNumber;
                edit.text = line;

                var target = $"{edit.id}|{(edit.kind == FeatureEditKind.Set ? edit.path : edit.kind.ToString())}";
                if (!targets.Add(target)) {
                    errors.Add($"{where}: '{edit.id}' already has this edit earlier in the file. Combine them into one.");
                    continue;
                }
                if (id2removeLine.TryGetValue(edit.id, out int removeLine)) {
                    errors.Add($"{where}: '{edit.id}' is removed on line {removeLine}, so it can't have other edits.");
                    continue;
                }
                if (edit.kind == FeatureEditKind.Remove) {
                    if (id2firstLine.TryGetValue(edit.id, out int firstLine)) {
                        errors.Add($"{where}: '{edit.id}' has an edit on line {firstLine}, so it can't also be removed.");
                        continue;
                    }
                    id2removeLine[edit.id] = lineNumber;
                }
                if (!id2firstLine.ContainsKey(edit.id)) {
                    id2firstLine[edit.id] = lineNumber;
                }
                file.edits.Add(edit);
            }

            bool complete = true;
            if (string.IsNullOrEmpty(file.areaId)) {
                errors.Add($"{source}: missing 'area: <area ID>'.");
                complete = false;
            }
            if (string.IsNullOrEmpty(file.world)) {
                errors.Add($"{source}: missing 'world: <path to the world data asset>'.");
                complete = false;
            }
            return complete ? file : null;
        }

        // 'area:' and 'world:' take the rest of the line, since both can contain spaces.
        static bool ReadHeader(FeatureEditFile file, string line, string where, List<string> errors) {
            int colon = line.IndexOf(':');
            if (colon <= 0) {
                return false;
            }
            var key = line.Substring(0, colon).Trim();
            var value = line.Substring(colon + 1).Trim();
            if (key.IndexOf(' ') >= 0 || key.IndexOf('"') >= 0) {
                return false;
            }

            switch (key.ToLowerInvariant()) {
                case "area":
                    file.areaId = value;
                    break;
                case "world":
                    file.world = value.Replace('\\', '/');
                    break;
                default:
                    // An edit's field path can't contain ':', so this is a misspelt header.
                    errors.Add($"{where}: unknown header '{key}'. Headers are 'area' and 'world'.");
                    break;
            }
            if (value.Length == 0) {
                errors.Add($"{where}: '{key}' needs a value.");
            }
            return true;
        }

        static FeatureEdit ReadEdit(List<Token> tokens, string where, List<string> errors) {
            var verb = tokens[0].text.ToLowerInvariant();
            FeatureEditKind kind;
            switch (verb) {
                case "move":
                    kind = FeatureEditKind.Move;
                    break;
                case "face":
                    kind = FeatureEditKind.Face;
                    break;
                case "set":
                    kind = FeatureEditKind.Set;
                    break;
                case "remove":
                    kind = FeatureEditKind.Remove;
                    break;
                default:
                    errors.Add($"{where}: unknown edit '{tokens[0].text}'. Edits are move, face, set, and remove.");
                    return null;
            }

            if (tokens.Count < 2) {
                errors.Add($"{where}: '{verb}' needs a feature ID.");
                return null;
            }
            var edit = new FeatureEdit { kind = kind, id = tokens[1].text, to = new string[0] };
            var idError = FeatureIds.FindFormatError(edit.id);
            if (idError != null) {
                errors.Add($"{where}: '{edit.id}': {idError}");
                return null;
            }

            switch (kind) {
                case FeatureEditKind.Move:
                    if (tokens.Count != 8 || !tokens[2].Is("from") || !tokens[5].Is("to")) {
                        errors.Add($"{where}: expected 'move <ID> from <x> <y> to <x> <y>'.");
                        return null;
                    }
                    edit.from = ReadTile(tokens, 3, where, errors);
                    edit.to = ReadTile(tokens, 6, where, errors);
                    if (edit.from == null || edit.to == null) {
                        return null;
                    }
                    break;
                case FeatureEditKind.Face:
                    if (tokens.Count != 6 || !tokens[2].Is("from") || !tokens[4].Is("to")) {
                        errors.Add($"{where}: expected 'face <ID> from <direction> to <direction>'.");
                        return null;
                    }
                    edit.path = "facing";
                    edit.from = new[] { tokens[3].text };
                    edit.to = new[] { tokens[5].text };
                    break;
                case FeatureEditKind.Set:
                    if (!ReadSet(edit, tokens, where, errors)) {
                        return null;
                    }
                    break;
                case FeatureEditKind.Remove:
                    if (tokens.Count != 5 || !tokens[2].Is("at")) {
                        errors.Add($"{where}: expected 'remove <ID> at <x> <y>'.");
                        return null;
                    }
                    edit.from = ReadTile(tokens, 3, where, errors);
                    if (edit.from == null) {
                        return null;
                    }
                    break;
            }

            if (kind != FeatureEditKind.Remove && Same(edit.from, edit.to)) {
                errors.Add($"{where}: the 'from' and 'to' values are the same.");
                return null;
            }
            return edit;
        }

        static bool ReadSet(FeatureEdit edit, List<Token> tokens, string where, List<string> errors) {
            const string Usage = "expected 'set <ID> <Component>.<field> from <value> to <value>'.";
            if (tokens.Count < 7 || !tokens[3].Is("from")) {
                errors.Add($"{where}: {Usage}");
                return false;
            }
            edit.path = tokens[2].text;
            int dot = edit.path.IndexOf('.');
            if (dot <= 0 || dot == edit.path.Length - 1) {
                errors.Add($"{where}: '{edit.path}' should start with the component, like PatrolAIBehaviour.mode.");
                return false;
            }

            int toIndex = -1;
            for (int i = 4; i < tokens.Count; i++) {
                if (tokens[i].Is("to")) {
                    toIndex = i;
                    break;
                }
            }
            if (toIndex < 5 || toIndex == tokens.Count - 1) {
                errors.Add($"{where}: {Usage} Quote a value that is the word \"to\".");
                return false;
            }
            edit.from = Texts(tokens, 4, toIndex);
            edit.to = Texts(tokens, toIndex + 1, tokens.Count);
            return true;
        }

        static string[] ReadTile(List<Token> tokens, int start, string where, List<string> errors) {
            var tile = new string[2];
            for (int i = 0; i < 2; i++) {
                if (!int.TryParse(tokens[start + i].text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) {
                    errors.Add($"{where}: '{tokens[start + i].text}' isn't a whole number.");
                    return null;
                }
                tile[i] = value.ToString(CultureInfo.InvariantCulture);
            }
            return tile;
        }

        static string[] Texts(List<Token> tokens, int start, int end) {
            var texts = new string[end - start];
            for (int i = start; i < end; i++) {
                texts[i - start] = tokens[i].text;
            }
            return texts;
        }

        static bool Same(string[] a, string[] b) {
            if (a.Length != b.Length) {
                return false;
            }
            for (int i = 0; i < a.Length; i++) {
                if (a[i] != b[i]) {
                    return false;
                }
            }
            return true;
        }

        // Words split on spaces. "Double quotes" make one word of text with spaces, such as an asset path.
        static List<Token> Tokenize(string line, out string error) {
            error = null;
            var tokens = new List<Token>();
            int i = 0;
            while (i < line.Length) {
                if (char.IsWhiteSpace(line[i])) {
                    i++;
                    continue;
                }
                if (line[i] == '"') {
                    int close = line.IndexOf('"', i + 1);
                    if (close < 0) {
                        error = "a quote isn't closed.";
                        return tokens;
                    }
                    tokens.Add(new Token { text = line.Substring(i + 1, close - i - 1), quoted = true });
                    i = close + 1;
                    continue;
                }
                var word = new StringBuilder();
                while (i < line.Length && !char.IsWhiteSpace(line[i])) {
                    word.Append(line[i]);
                    i++;
                }
                tokens.Add(new Token { text = word.ToString() });
            }
            return tokens;
        }
    }
}
