using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class MapSpecDiffTests {
        const string Grass = "Assets/Grass.asset";
        const string Path = "Assets/Path.asset";
        const string Tree = "Assets/Tree.asset";
        const string Bush = "Assets/Bush.asset";
        const string Walk = "Assets/Walk.asset";
        const string Wall = "Assets/Wall.asset";

        const string Empty =
            "....\n" +
            "....\n";

        static MapLegend Legend() {
            return new MapLegend {
                layers = new[] {
                    new MapLegendLayer { name = "Ground", sortingLayer = "Ground" },
                    new MapLegendLayer { name = "Trees", sortingLayer = "Entity On Ground" },
                },
                entries = new[] {
                    new MapLegendEntry { symbol = "g", layer = "Ground", tile = Grass, logic = Walk },
                    new MapLegendEntry { symbol = "=", layer = "Ground", tile = Path, logic = Walk },
                    new MapLegendEntry { symbol = "T", layer = "Trees", tile = Tree, logic = Wall, footprint = new MapFootprint { width = 2, height = 1 } },
                    new MapLegendEntry { symbol = "b", layer = "Trees", tile = Bush },
                },
            };
        }

        // Bounds 0 0 4 2: row 0 is y = 1, row 1 is y = 0.
        static MapSpec Spec(string ground, string trees = Empty, string region = null) {
            var bounds = region == null ? "" : $"region: {region}\n";
            var text = "id: a\nworld: w\nbounds: 0 0 4 2\nlegend: l\n" + bounds + "[Ground]\n" + ground + "[Trees]\n" + trees;
            var errors = new List<string>();
            var spec = MapSpecParser.Parse(text, "a.txt", errors);
            MapSpecParser.CheckAgainstLegend(spec, Legend(), errors);
            CollectionAssert.IsEmpty(errors);
            return spec;
        }

        /// <summary>The area a new-area build of the spec would make.</summary>
        static MapAreaSnapshot Built(MapSpec spec) {
            var snapshot = new MapAreaSnapshot();
            foreach (var placement in MapSpecDiff.Compute(spec, Legend(), new MapAreaSnapshot()).changes) {
                var cells = placement.layer == MapSpecParser.LogicLayer ? snapshot.logic : snapshot.Layer(placement.layer);
                cells[placement.cell] = placement.tilePath;
            }
            return snapshot;
        }

        static MapSpecChanges Diff(MapSpec spec, MapAreaSnapshot before) {
            return MapSpecDiff.Compute(spec, Legend(), before);
        }

        static MapTilePlacement Change(string layer, int x, int y, string tile) {
            return new MapTilePlacement(layer, new Vector2Int(x, y), tile);
        }

        const string AllGrass = "gggg\ngggg\n";
        const string TreeTopLeft = "T...\n....\n";

        [Test]
        public void Compute_UnchangedSpec_HasNoChanges() {
            var spec = Spec(AllGrass, TreeTopLeft);

            var changes = Diff(spec, Built(spec));

            CollectionAssert.IsEmpty(changes.changes);
            CollectionAssert.IsEmpty(changes.warnings);
        }

        [Test]
        public void Compute_KeepSymbol_LeavesUnknownTiles() {
            var before = Built(Spec(AllGrass));
            before.Layer("Ground")[new Vector2Int(0, 0)] = "Assets/Unknown.asset";

            var changes = Diff(Spec("????\n????\n", "????\n????\n"), before);

            CollectionAssert.IsEmpty(changes.changes);
        }

        [Test]
        public void Compute_ChangedCell_ChangesOnlyThatCell() {
            var changes = Diff(Spec("gg=g\ngggg\n"), Built(Spec(AllGrass)));

            // Path and grass have the same logic, so only the art changes.
            CollectionAssert.AreEqual(new[] { Change("Ground", 2, 1, Path) }, changes.changes);
        }

        [Test]
        public void Compute_EmptySymbol_ClearsArtAndLogic() {
            var changes = Diff(Spec("ggg.\ngggg\n"), Built(Spec(AllGrass)));

            CollectionAssert.AreEquivalent(new[] { Change("Ground", 3, 1, null), Change("Logic", 3, 1, null) }, changes.changes);
        }

        [Test]
        public void Compute_AddedTree_BlocksItsFootprint() {
            var changes = Diff(Spec(AllGrass, TreeTopLeft), Built(Spec(AllGrass)));

            CollectionAssert.AreEquivalent(new[] {
                Change("Trees", 0, 1, Tree), Change("Logic", 0, 1, Wall), Change("Logic", 1, 1, Wall),
            }, changes.changes);
        }

        [Test]
        public void Compute_RemovedTree_FreesItsFootprint() {
            var changes = Diff(Spec(AllGrass), Built(Spec(AllGrass, TreeTopLeft)));

            CollectionAssert.AreEquivalent(new[] {
                Change("Trees", 0, 1, null), Change("Logic", 0, 1, Walk), Change("Logic", 1, 1, Walk),
            }, changes.changes);
        }

        [Test]
        public void Compute_HandPaintedLogic_IsKeptWithWarning() {
            var before = Built(Spec(AllGrass, TreeTopLeft));
            before.logic[new Vector2Int(1, 1)] = "Assets/Ledge.asset";

            var changes = Diff(Spec(AllGrass), before);

            CollectionAssert.AreEquivalent(new[] { Change("Trees", 0, 1, null), Change("Logic", 0, 1, Walk) }, changes.changes);
            Assert.AreEqual(1, changes.warnings.Count);
            StringAssert.Contains("(1, 1)", changes.warnings[0]);
        }

        [Test]
        public void Compute_ArtWithNoLogicEntry_KeepsLogicWithWarning() {
            var before = Built(Spec(AllGrass));
            // Ground tiles the legend gives no logic to, as an export makes for tiles with mixed logic.
            before.Layer("Ground")[new Vector2Int(0, 1)] = "Assets/Unknown.asset";

            var changes = Diff(Spec("?ggg\ngggg\n", "b...\n....\n"), before);

            CollectionAssert.AreEqual(new[] { Change("Trees", 0, 1, Bush) }, changes.changes);
            Assert.AreEqual(1, changes.warnings.Count);
        }

        [Test]
        public void Compute_Region_OnlyTouchesCellsInside() {
            var changes = Diff(Spec("==\n==\n", "..\n..\n", "2 0 2 2"), Built(Spec(AllGrass)));

            Assert.AreEqual(4, changes.changes.Count);
            Assert.IsTrue(changes.changes.TrueForAll(c => c.layer == "Ground" && c.cell.x >= 2 && c.tilePath == Path));
        }
    }
}
