using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    public partial class FeatureIdWindow {
        const string SingleFieldName = "FeatureIdSingleField";

        // One problem: a clashing ID shared by several features, or one feature's missing or bad ID.
        struct Problem {
            public string key;
            public string duplicateId;
            public IdentifiedFeatureBehaviour feature;
        }

        int problemIndex;
        string shownKey;
        IdentifiedFeatureBehaviour requested;
        string draft = "";
        string keeperKey;
        readonly Dictionary<string, string> key2draft = new Dictionary<string, string>();

        void DrawProblems(FeatureIdIndex index) {
            var problems = FindProblems(index);
            if (problems.Count == 0) {
                shownKey = null;
                EditorGUILayout.HelpBox("All features in the open scenes have unique IDs.", MessageType.Info);
                return;
            }

            ChooseShown(problems);
            var problem = problems[problemIndex];
            if (problem.key != shownKey) {
                shownKey = problem.key;
                draft = problem.feature != null ? problem.feature.featureId : "";
                keeperKey = null;
                key2draft.Clear();
                if (problem.feature != null) {
                    FrameInScene(problem.feature);
                    EditorGUI.FocusTextInControl(SingleFieldName);
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUI.DisabledScope(problemIndex == 0)) {
                    if (GUILayout.Button("◀ Prev", GUILayout.Width(70))) {
                        Step(problems, -1);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{problemIndex + 1} of {problems.Count}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(problemIndex == problems.Count - 1)) {
                    if (GUILayout.Button("Next ▶", GUILayout.Width(70))) {
                        Step(problems, 1);
                    }
                }
            }
            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (problem.duplicateId != null) {
                DrawDuplicate(index, problem.duplicateId);
            }
            else {
                DrawSingle(index, problem.feature);
            }
            EditorGUILayout.EndScrollView();
        }

        void Step(List<Problem> problems, int direction) {
            problemIndex = Mathf.Clamp(problemIndex + direction, 0, problems.Count - 1);
            shownKey = null;
            GUIUtility.keyboardControl = 0;
        }

        // Keeps showing the same problem while the list changes around it.
        void ChooseShown(List<Problem> problems) {
            if (requested != null) {
                for (int i = 0; i < problems.Count; i++) {
                    if (problems[i].feature == requested || (problems[i].duplicateId != null && FeatureIds.Same(problems[i].duplicateId, requested.featureId))) {
                        problemIndex = i;
                        break;
                    }
                }
                requested = null;
                return;
            }
            for (int i = 0; i < problems.Count; i++) {
                if (problems[i].key == shownKey) {
                    problemIndex = i;
                    return;
                }
            }
            problemIndex = Mathf.Clamp(problemIndex, 0, problems.Count - 1);
        }

        // Clashes first, then bad formats, then missing IDs. A baked record with a missing or bad ID
        // can't be fixed without opening its scene, so only its clashes are listed.
        static List<Problem> FindProblems(FeatureIdIndex index) {
            var problems = new List<Problem>();
            var badFormats = new List<Problem>();
            var missing = new List<Problem>();
            var listed = new HashSet<string>(FeatureIds.comparer);

            foreach (var entry in index.entries) {
                switch (entry.status) {
                    case FeatureIdIndex.Status.Duplicate:
                        if (listed.Add(entry.id)) {
                            problems.Add(new Problem { key = $"duplicate:{entry.id.ToLowerInvariant()}", duplicateId = entry.id });
                        }
                        break;
                    case FeatureIdIndex.Status.BadFormat when entry.isOpen:
                        badFormats.Add(new Problem { key = KeyOf(entry, 0), feature = entry.feature });
                        break;
                    case FeatureIdIndex.Status.Missing when entry.isOpen:
                        missing.Add(new Problem { key = KeyOf(entry, 0), feature = entry.feature });
                        break;
                }
            }
            problems.AddRange(badFormats);
            problems.AddRange(missing);
            return problems;
        }

        void DrawSingle(FeatureIdIndex index, IdentifiedFeatureBehaviour feature) {
            var tile = TileSpaces.TileOf(feature.transform);
            EditorGUILayout.LabelField($"{FeatureIdIndex.KindName(feature.GetType())}  '{feature.name}'  at {TileText(tile)}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Scene: {feature.gameObject.scene.name}", EditorStyles.miniLabel);
            var current = feature.featureId.Length == 0 ? "No ID." : FeatureIds.FindFormatError(feature.featureId);
            EditorGUILayout.HelpBox(current, MessageType.Warning);
            EditorGUILayout.Space();

            // Read Enter before the text field, which would otherwise keep the event.
            var e = Event.current;
            bool enter = IsEnter(e) && GUI.GetNameOfFocusedControl() == SingleFieldName;

            GUI.SetNextControlName(SingleFieldName);
            draft = EditorGUILayout.TextField("ID", draft);

            var draftProblem = index.FindProblem(draft, feature);
            EditorGUILayout.HelpBox(draftProblem ?? $"'{draft}' is free.", draftProblem != null ? MessageType.Error : MessageType.Info);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Focus")) {
                    FrameInScene(feature);
                }
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(draftProblem != null)) {
                    if (GUILayout.Button("Set ID", GUILayout.Width(90)) || (enter && draftProblem == null)) {
                        SetId(feature, draft);
                        shownKey = null;
                        if (enter) {
                            e.Use();
                        }
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        void DrawDuplicate(FeatureIdIndex index, string id) {
            var rows = index.WithId(id);
            var keys = KeysOf(rows);
            if (keeperKey != null && !keys.Contains(keeperKey)) {
                keeperKey = null;
            }

            EditorGUILayout.LabelField($"Duplicate ID: {id}  ({rows.Count} features)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Choose the feature that keeps the ID, then type a new ID for each of the others.", MessageType.None);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.Label("Keeps", EditorStyles.miniBoldLabel, GUILayout.Width(40));
                GUILayout.Label("Feature", EditorStyles.miniBoldLabel, GUILayout.MinWidth(160));
                GUILayout.Label("Area", EditorStyles.miniBoldLabel, GUILayout.Width(80));
                GUILayout.Label("Tile", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                GUILayout.Label("New ID", EditorStyles.miniBoldLabel, GUILayout.MinWidth(140));
                GUILayout.Space(94);
            }

            bool ready = keeperKey != null;
            for (int i = 0; i < rows.Count; i++) {
                var row = rows[i];
                var key = keys[i];
                bool keeps = key == keeperKey;
                string rowProblem = null;

                using (new EditorGUILayout.HorizontalScope()) {
                    if (EditorGUILayout.Toggle(keeps, EditorStyles.radioButton, GUILayout.Width(40)) && !keeps) {
                        keeperKey = key;
                        keeps = true;
                    }
                    var name = row.isOpen ? $"{row.kind} '{row.name}'" : $"{row.kind} (scene not open)";
                    GUILayout.Label(name, GUILayout.MinWidth(160));
                    GUILayout.Label(row.areaId, GUILayout.Width(80));
                    GUILayout.Label(TileText(row.tile), GUILayout.Width(60));

                    if (keeps) {
                        GUILayout.Label($"keeps {row.id}", EditorStyles.miniLabel, GUILayout.MinWidth(140));
                    }
                    else if (row.isOpen) {
                        // Starts as the shared ID, since a fix is usually a small change such as a suffix.
                        if (!key2draft.TryGetValue(key, out var rowDraft)) {
                            rowDraft = row.id;
                        }
                        key2draft[key] = EditorGUILayout.TextField(rowDraft, GUILayout.MinWidth(140));
                        rowProblem = FindDraftProblem(index, id, row, key, keys);
                    }
                    else {
                        GUILayout.Label("Open its scene to rename it", EditorStyles.miniLabel, GUILayout.MinWidth(140));
                        rowProblem = "Open its scene to rename it.";
                    }

                    if (row.isOpen) {
                        if (GUILayout.Button("Focus", GUILayout.Width(90))) {
                            FrameInScene(row.feature);
                        }
                    }
                    else if (GUILayout.Button("Open Scene", GUILayout.Width(90))) {
                        OpenScene(row.scenePath);
                        GUIUtility.ExitGUI();
                    }
                }

                if (!keeps && keeperKey != null) {
                    if (rowProblem != null) {
                        EditorGUILayout.LabelField("        " + rowProblem, ErrorStyle());
                        ready = false;
                    }
                }
            }

            EditorGUILayout.Space();
            if (keeperKey == null) {
                EditorGUILayout.HelpBox("Choose which feature keeps the ID.", MessageType.Info);
            }
            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!ready)) {
                    if (GUILayout.Button("Apply", GUILayout.Width(90))) {
                        var changes = new List<KeyValuePair<IdentifiedFeatureBehaviour, string>>();
                        for (int i = 0; i < rows.Count; i++) {
                            if (keys[i] != keeperKey) {
                                changes.Add(new KeyValuePair<IdentifiedFeatureBehaviour, string>(rows[i].feature, key2draft[keys[i]]));
                            }
                        }
                        SetIds(changes, "Fix Duplicate Feature ID");
                        shownKey = null;
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        // The group's other members are being renamed, so only the kept ID, other drafts, and features
        // outside the group can clash.
        string FindDraftProblem(FeatureIdIndex index, string id, FeatureIdIndex.Entry row, string key, List<string> keys) {
            var rowDraft = key2draft[key];
            if (rowDraft.Length == 0) {
                return "Type a new ID.";
            }
            if (FeatureIds.Same(rowDraft, id)) {
                return $"'{id}' is the ID being kept.";
            }
            var problem = index.FindProblem(rowDraft, row.feature);
            if (problem != null) {
                return problem;
            }
            foreach (var other in keys) {
                if (other != key && other != keeperKey && key2draft.TryGetValue(other, out var otherDraft) && FeatureIds.Same(otherDraft, rowDraft)) {
                    return $"Another feature here would also get '{rowDraft}'.";
                }
            }
            return null;
        }

        static GUIStyle errorStyle;

        static GUIStyle ErrorStyle() {
            if (errorStyle == null) {
                errorStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.35f, 0.35f) } };
            }
            return errorStyle;
        }
    }
}
