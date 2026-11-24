using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    public partial class FeatureIdWindow {
        const string InlineFieldName = "FeatureIdInlineField";
        const float StatusWidth = 22f;
        const float IdWidth = 170f;
        const float KindWidth = 90f;
        const float AreaWidth = 90f;
        const float TileWidth = 70f;
        const float ButtonWidth = 90f;

        enum SortBy { Area, Kind, Id, Name }

        static readonly string[] sortLabels = { "Area", "Kind", "ID", "Name" };

        string search = "";
        bool problemsOnly;
        bool showUnopened;
        SortBy sortBy;
        string selectedKey;
        string editingKey;
        string editDraft = "";
        bool focusInline;

        void DrawAllFeatures(FeatureIdIndex index) {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
                problemsOnly = GUILayout.Toggle(problemsOnly, "Problems only", EditorStyles.toolbarButton, GUILayout.Width(100));
                showUnopened = GUILayout.Toggle(showUnopened, "Show unopened areas", EditorStyles.toolbarButton, GUILayout.Width(140));
                GUILayout.Label("Sort", GUILayout.Width(30));
                sortBy = (SortBy)EditorGUILayout.Popup((int)sortBy, sortLabels, EditorStyles.toolbarPopup, GUILayout.Width(70));
            }

            var rows = new List<FeatureIdIndex.Entry>();
            var rowKeys = new List<string>();
            var keys = KeysOf(index.entries);
            for (int i = 0; i < index.entries.Count; i++) {
                if (Shows(index.entries[i])) {
                    rows.Add(index.entries[i]);
                    rowKeys.Add(keys[i]);
                }
            }
            SortRows(rows, rowKeys);

            HandleKeys(rows, rowKeys, index);

            DrawHeader();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < rows.Count; i++) {
                DrawRow(rows[i], rowKeys[i], index);
            }
            if (rows.Count == 0) {
                EditorGUILayout.LabelField("No features match.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndScrollView();

            DrawEditProblem(rows, rowKeys, index);
        }

        bool Shows(FeatureIdIndex.Entry entry) {
            if (!entry.isOpen && !showUnopened) {
                return false;
            }
            if (problemsOnly && entry.status == FeatureIdIndex.Status.Ok) {
                return false;
            }
            if (search.Length > 0) {
                bool matches = entry.id.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || entry.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matches) {
                    return false;
                }
            }
            return true;
        }

        // Stable, so rows that tie keep their hierarchy order.
        void SortRows(List<FeatureIdIndex.Entry> rows, List<string> rowKeys) {
            var order = new List<int>(rows.Count);
            for (int i = 0; i < rows.Count; i++) {
                order.Add(i);
            }
            order.Sort((a, b) => {
                int byField = string.Compare(SortValue(rows[a]), SortValue(rows[b]), System.StringComparison.OrdinalIgnoreCase);
                return byField != 0 ? byField : a.CompareTo(b);
            });

            var sortedRows = new List<FeatureIdIndex.Entry>(rows.Count);
            var sortedKeys = new List<string>(rows.Count);
            foreach (int i in order) {
                sortedRows.Add(rows[i]);
                sortedKeys.Add(rowKeys[i]);
            }
            rows.Clear();
            rows.AddRange(sortedRows);
            rowKeys.Clear();
            rowKeys.AddRange(sortedKeys);
        }

        string SortValue(FeatureIdIndex.Entry entry) {
            switch (sortBy) {
                case SortBy.Kind:
                    return entry.kind;
                case SortBy.Id:
                    return entry.id;
                case SortBy.Name:
                    return entry.name;
                default:
                    return entry.areaId;
            }
        }

        // Up and down move the selection and frame it. Enter and Escape finish an inline edit.
        void HandleKeys(List<FeatureIdIndex.Entry> rows, List<string> rowKeys, FeatureIdIndex index) {
            var e = Event.current;
            if (e.type != EventType.KeyDown) {
                return;
            }

            if (editingKey != null) {
                if (e.keyCode == KeyCode.Escape) {
                    StopEditing();
                    e.Use();
                }
                else if (IsEnter(e)) {
                    int row = rowKeys.IndexOf(editingKey);
                    if (row >= 0 && index.FindProblem(editDraft, rows[row].feature) == null) {
                        SetId(rows[row].feature, editDraft);
                    }
                    StopEditing();
                    e.Use();
                }
                return;
            }

            if (e.keyCode != KeyCode.UpArrow && e.keyCode != KeyCode.DownArrow) {
                return;
            }
            if (GUIUtility.keyboardControl != 0 || rows.Count == 0) {
                return;
            }
            int current = rowKeys.IndexOf(selectedKey);
            int next = e.keyCode == KeyCode.UpArrow ? current - 1 : current + 1;
            if (current < 0) {
                next = 0;
            }
            next = Mathf.Clamp(next, 0, rows.Count - 1);
            Select(rows[next], rowKeys[next]);
            e.Use();
        }

        void StopEditing() {
            editingKey = null;
            GUIUtility.keyboardControl = 0;
            Repaint();
        }

        void Select(FeatureIdIndex.Entry entry, string key) {
            selectedKey = key;
            if (entry.isOpen) {
                FrameInScene(entry.feature);
            }
            Repaint();
        }

        static void DrawHeader() {
            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            var cells = Cells(rect);
            GUI.Label(cells[1], "ID", EditorStyles.miniBoldLabel);
            GUI.Label(cells[2], "Kind", EditorStyles.miniBoldLabel);
            GUI.Label(cells[3], "Name", EditorStyles.miniBoldLabel);
            GUI.Label(cells[4], "Area", EditorStyles.miniBoldLabel);
            GUI.Label(cells[5], "Tile", EditorStyles.miniBoldLabel);
        }

        // Status, ID, kind, name, area, tile, and button cells. The name takes the spare width.
        static Rect[] Cells(Rect row) {
            float fixedWidth = StatusWidth + IdWidth + KindWidth + AreaWidth + TileWidth + ButtonWidth;
            float nameWidth = Mathf.Max(80f, row.width - fixedWidth);
            var widths = new[] { StatusWidth, IdWidth, KindWidth, nameWidth, AreaWidth, TileWidth, ButtonWidth };

            var cells = new Rect[widths.Length];
            float x = row.x;
            for (int i = 0; i < widths.Length; i++) {
                cells[i] = new Rect(x, row.y, widths[i] - 4f, row.height);
                x += widths[i];
            }
            return cells;
        }

        void DrawRow(FeatureIdIndex.Entry entry, string key, FeatureIdIndex index) {
            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight + 2f, GUILayout.ExpandWidth(true));
            var cells = Cells(rect);
            var e = Event.current;

            if (key == selectedKey) {
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.9f, 0.35f));
            }

            DrawStatus(cells[0], entry.status);
            using (new EditorGUI.DisabledScope(!entry.isOpen)) {
                if (key == editingKey) {
                    GUI.SetNextControlName(InlineFieldName);
                    editDraft = EditorGUI.TextField(cells[1], editDraft);
                    // Only once, since focusing again would reselect the text while typing.
                    if (focusInline && e.type == EventType.Layout) {
                        focusInline = false;
                        EditorGUI.FocusTextInControl(InlineFieldName);
                    }
                }
                else {
                    GUI.Label(cells[1], entry.id.Length > 0 ? entry.id : "(no ID)");
                }
                GUI.Label(cells[2], entry.kind);
                GUI.Label(cells[3], entry.isOpen ? entry.name : "(scene not open)");
                GUI.Label(cells[4], entry.areaId);
                GUI.Label(cells[5], TileText(entry.tile));
            }

            if (!entry.isOpen && GUI.Button(cells[6], "Open Scene", EditorStyles.miniButton)) {
                OpenScene(entry.scenePath);
                GUIUtility.ExitGUI();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition) && !cells[6].Contains(e.mousePosition) && key != editingKey) {
                if (editingKey != null) {
                    StopEditing();
                }
                GUIUtility.keyboardControl = 0;
                Select(entry, key);
                if (e.clickCount == 2 && entry.isOpen && cells[1].Contains(e.mousePosition)) {
                    editingKey = key;
                    editDraft = entry.id;
                    focusInline = true;
                }
                e.Use();
            }
        }

        static void DrawStatus(Rect cell, FeatureIdIndex.Status status) {
            string icon;
            string tooltip;
            switch (status) {
                case FeatureIdIndex.Status.Missing:
                    icon = "console.warnicon.sml";
                    tooltip = "No ID.";
                    break;
                case FeatureIdIndex.Status.BadFormat:
                    icon = "console.erroricon.sml";
                    tooltip = "The ID has characters that aren't allowed.";
                    break;
                case FeatureIdIndex.Status.Duplicate:
                    icon = "console.erroricon.sml";
                    tooltip = "Another feature has this ID.";
                    break;
                default:
                    return;
            }
            var content = new GUIContent(EditorGUIUtility.IconContent(icon).image, tooltip);
            GUI.Label(cell, content);
        }

        void DrawEditProblem(List<FeatureIdIndex.Entry> rows, List<string> rowKeys, FeatureIdIndex index) {
            if (editingKey == null) {
                EditorGUILayout.HelpBox("Click a feature to frame it, and use the arrow keys to move between them. Double-click an ID to edit it.", MessageType.None);
                return;
            }
            int row = rowKeys.IndexOf(editingKey);
            if (row < 0) {
                editingKey = null;
                return;
            }
            var problem = index.FindProblem(editDraft, rows[row].feature);
            EditorGUILayout.HelpBox(problem ?? $"'{editDraft}' is free. Press Enter to set it, or Escape to cancel.", problem != null ? MessageType.Error : MessageType.Info);
        }
    }
}
