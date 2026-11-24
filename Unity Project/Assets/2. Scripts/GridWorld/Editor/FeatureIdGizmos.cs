using UnityEditor;
using UnityEngine;

namespace GridWorld {
    /// <summary>Labels every identified feature with its ID, and marks missing or clashing IDs in red.</summary>
    static class FeatureIdGizmos {
        static GUIStyle normalStyle;
        static GUIStyle problemStyle;

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void Draw(IdentifiedFeatureBehaviour feature, GizmoType type) {
            if (normalStyle == null) {
                normalStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
                problemStyle = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = new Color(1f, 0.3f, 0.3f) } };
            }

            var tile = TileSpaces.TileOf(feature.transform);
            var center = new Vector3(tile.x + 0.5f, tile.y + 0.5f);
            var id = feature.featureId;
            bool problem = id.Length == 0 || FeatureIds.FindFormatError(id) != null || FeatureIdIndex.Current.IsDuplicated(id);

            if (problem) {
                Gizmos.color = new Color(1f, 0.3f, 0.3f);
                Gizmos.DrawWireCube(center, Vector3.one);
            }
            Handles.Label(center + new Vector3(-0.45f, 0.95f), id.Length > 0 ? id : "(no ID)", problem ? problemStyle : normalStyle);
        }
    }
}
