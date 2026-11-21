using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Creates a new area scene from a checked spec and paints it. It only creates areas: once an area
    /// exists, its scene is the source of truth, so a spec for a taken ID is refused. Painting goes
    /// through the same diff and apply path as editing, starting from the empty area.
    /// </summary>
    public static class MapSpecBuilder {
        /// <summary>
        /// Returns the new area, or null after logging every problem. Without
        /// <paramref name="openAdditively"/>, open scenes close without saving, so ask first.
        /// </summary>
        public static AreaBehaviour Build(MapSpec spec, MapLegend legend, bool openAdditively) {
            var errors = MapSpecChecks.Find(spec, legend, MapSpecMode.Build, openAdditively);
            if (errors.Count > 0) {
                LogErrors(errors);
                return null;
            }

            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(spec.world);
            var area = NewAreaCreator.Create(Request(spec, world, openAdditively));
            if (area == null) {
                return null;
            }

            // Set up each layer first, so a tilemap New Area already made, like Ground, still gets the
            // legend's sorting layer.
            foreach (var layer in spec.layers) {
                FindOrCreateTilemap(area, layer.name, legend);
            }

            var snapshot = MapSpecApplier.Snapshot(area, errors, new List<string>());
            if (snapshot == null) {
                // New Area made something the spec can't address, so the scene is left as it made it.
                LogErrors(errors);
                return null;
            }
            // Tiles load after the scene opens, because opening it can unload unreferenced assets.
            MapSpecApplier.Paint(area, legend, MapSpecDiff.Compute(spec, legend, snapshot), false);

            // Saving bakes the painted tiles into the area's data.
            EditorSceneManager.SaveScene(area.gameObject.scene);
            return area;
        }

        internal static NewAreaRequest Request(MapSpec spec, WorldDataSO world, bool openAdditively) {
            var worldPath = AssetDatabase.GetAssetPath(world);
            var last = NewAreaRules.LastArea(world);

            return new NewAreaRequest {
                id = spec.id,
                world = world,
                sceneFolder = spec.sceneFolder ?? NewAreaRules.DefaultFolder(last == null ? null : last.scenePath, worldPath),
                dataFolder = spec.dataFolder ?? NewAreaRules.DefaultFolder(last == null ? null : AssetDatabase.GetAssetPath(last), worldPath),
                sceneName = NewAreaRules.SceneName(spec.id),
                dataName = NewAreaRules.DataName(spec.id),
                bounds = spec.bounds,
                openAdditively = openAdditively,
            };
        }

        internal static Tilemap FindOrCreateTilemap(AreaBehaviour area, string layerName, MapLegend legend) {
            if (layerName == MapSpecParser.LogicLayer) {
                return area.GetComponentInChildren<LogicLayerBehaviour>(true).GetComponent<Tilemap>();
            }

            var child = area.transform.Find(layerName);
            var tilemapObject = child != null && child.GetComponent<Tilemap>() != null ? child.gameObject : NewAreaCreator.CreateTilemap(area.gameObject, layerName, "");

            var layer = Array.Find(legend.layers, l => l.name == layerName);
            var renderer = tilemapObject.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = layer.sortingLayer;
            // Tall tiles on the Y-sorted layer sort against entities one by one, not as one tilemap.
            if (layer.sortingLayer == SortingLayerNames.EntityOnGround) {
                renderer.mode = TilemapRenderer.Mode.Individual;
            }
            return tilemapObject.GetComponent<Tilemap>();
        }

        static void LogErrors(List<string> errors) {
            foreach (var error in errors) {
                Debug.LogError($"Can't build area from spec: {error}");
            }
        }
    }
}
