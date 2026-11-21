using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class MapSpecParserTests {
        const string LegendJson = @"{
            ""layers"": [ { ""name"": ""Ground"", ""sortingLayer"": ""Ground"" },
                          { ""name"": ""Trees"", ""sortingLayer"": ""Entity On Ground"" } ],
            ""entries"": [ { ""symbol"": ""g"", ""layer"": ""Ground"", ""tile"": ""Assets/Grass.asset"", ""logic"": ""Assets/Walk.asset"" },
                           { ""symbol"": ""="", ""layer"": ""Ground"", ""tile"": ""Assets/Path.asset"", ""logic"": ""Assets/Walk.asset"" },
                           { ""symbol"": ""T"", ""layer"": ""Trees"", ""tile"": ""Assets/Tree.asset"", ""logic"": ""Assets/Wall.asset"" },
                           { ""symbol"": ""f"", ""layer"": ""Trees"", ""tile"": ""Assets/Flower.asset"" } ]
        }";

        const string SpecText =
            "# A comment\n" +
            "id: forest\n" +
            "world: Assets/World.asset\n" +
            "bounds: 10 20 3 2\n" +
            "legend: grassland\n" +
            "\n" +
            "[Ground]\n" +
            "gg=\n" +
            "g==\n" +
            "[Trees]\n" +
            "T.f\n" +
            "...\n";

        List<string> errors;

        [SetUp]
        public void SetUp() {
            errors = new List<string>();
        }

        MapLegend Legend() {
            return MapSpecParser.ParseLegend(LegendJson, "grassland.json", errors);
        }

        MapSpec Spec(string text = SpecText) {
            return MapSpecParser.Parse(text, "forest.txt", errors);
        }

        [Test]
        public void Parse_ValidSpec_ReadsHeaderAndLayers() {
            var spec = Spec();

            CollectionAssert.IsEmpty(errors);
            Assert.AreEqual("forest", spec.id);
            Assert.AreEqual("Assets/World.asset", spec.world);
            Assert.AreEqual("grassland", spec.legend);
            Assert.AreEqual(new RectInt(10, 20, 3, 2), spec.bounds);
            Assert.IsNull(spec.sceneFolder);
            Assert.AreEqual(2, spec.layers.Count);
            CollectionAssert.AreEqual(new[] { "gg=", "g==" }, spec.layers[0].rows);
            CollectionAssert.AreEqual(new[] { 8, 9 }, spec.layers[0].rowLines);
        }

        [Test]
        public void Parse_MissingHeaders_ReturnsNullAndNamesEach() {
            var spec = Spec("[Ground]\ngg\n");

            Assert.IsNull(spec);
            Assert.IsTrue(errors.Exists(e => e.Contains("'id:'")));
            Assert.IsTrue(errors.Exists(e => e.Contains("'world:'")));
            Assert.IsTrue(errors.Exists(e => e.Contains("'legend:'")));
            Assert.IsTrue(errors.Exists(e => e.Contains("bounds")));
        }

        [TestCase("bounds: 1 2 3")]
        [TestCase("bounds: 1 2 0 4")]
        [TestCase("bounds: a b c d")]
        public void Parse_BadBounds_ReportsLine(string boundsLine) {
            Spec(SpecText.Replace("bounds: 10 20 3 2", boundsLine));

            Assert.IsTrue(errors.Exists(e => e.StartsWith("forest.txt:4:")));
        }

        [Test]
        public void Parse_UnknownHeader_ReportsLine() {
            Spec(SpecText.Replace("legend: grassland", "legend: grassland\ncolour: red"));

            Assert.IsTrue(errors.Exists(e => e.StartsWith("forest.txt:6:") && e.Contains("colour")));
        }

        [Test]
        public void ParseLegend_Valid_HasNoErrors() {
            var legend = Legend();

            CollectionAssert.IsEmpty(errors);
            Assert.AreEqual(4, legend.entries.Length);
            Assert.IsFalse(legend.entries[3].hasLogic);
        }

        [Test]
        public void ParseLegend_BadEntries_ReportsEach() {
            var json = @"{ ""layers"": [ { ""name"": ""Ground"", ""sortingLayer"": ""Ground"" }, { ""name"": ""Logic"", ""sortingLayer"": ""Ground"" } ],
                ""entries"": [ { ""symbol"": ""."", ""layer"": ""Ground"", ""tile"": ""a"" },
                               { ""symbol"": ""gg"", ""layer"": ""Ground"", ""tile"": ""a"" },
                               { ""symbol"": ""x"", ""layer"": ""Water"", ""tile"": ""a"" },
                               { ""symbol"": ""x"", ""layer"": ""Ground"" } ] }";

            MapSpecParser.ParseLegend(json, "bad.json", errors);

            Assert.IsTrue(errors.Exists(e => e.Contains("reserved for logic")));
            Assert.IsTrue(errors.Exists(e => e.Contains("'.' is reserved")));
            Assert.IsTrue(errors.Exists(e => e.Contains("must be one character")));
            Assert.IsTrue(errors.Exists(e => e.Contains("'Water'")));
            Assert.IsTrue(errors.Exists(e => e.Contains("used twice")));
            Assert.IsTrue(errors.Exists(e => e.Contains("has no tile")));
        }

        [Test]
        public void ParseLegend_Footprint_ReadsItAndDefaultsToUnset() {
            var json = LegendJson.Replace(@"""logic"": ""Assets/Wall.asset"" }", @"""logic"": ""Assets/Wall.asset"", ""footprint"": { ""x"": 0, ""y"": 0, ""width"": 2, ""height"": 1 } }");

            var legend = MapSpecParser.ParseLegend(json, "grassland.json", errors);

            CollectionAssert.IsEmpty(errors);
            Assert.AreEqual(2, legend.entries[2].footprint.width);
            Assert.AreEqual(1, legend.entries[2].footprint.height);
            Assert.IsTrue(legend.entries[0].footprint == null || !legend.entries[0].footprint.isSet);
        }

        [Test]
        public void ParseLegend_EmptyFootprint_ReportsIt() {
            var json = LegendJson.Replace(@"""logic"": ""Assets/Wall.asset"" }", @"""logic"": ""Assets/Wall.asset"", ""footprint"": { ""x"": -1, ""width"": 0, ""height"": 1 } }");

            MapSpecParser.ParseLegend(json, "grassland.json", errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("footprint", errors[0]);
        }

        [Test]
        public void ParseLegend_NotJson_ReturnsNull() {
            Assert.IsNull(MapSpecParser.ParseLegend("not json", "bad.json", errors));
            Assert.AreEqual(1, errors.Count);
        }

        [Test]
        public void CheckAgainstLegend_ValidSpec_HasNoErrors() {
            MapSpecParser.CheckAgainstLegend(Spec(), Legend(), errors);

            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void CheckAgainstLegend_WrongWidth_ReportsRowLine() {
            MapSpecParser.CheckAgainstLegend(Spec(SpecText.Replace("g==\n", "g===\n")), Legend(), errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.StartsWith("forest.txt:9:", errors[0]);
        }

        [Test]
        public void CheckAgainstLegend_WrongRowCount_ReportsLayerLine() {
            MapSpecParser.CheckAgainstLegend(Spec(SpecText.Replace("...\n", "")), Legend(), errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.StartsWith("forest.txt:10:", errors[0]);
        }

        [Test]
        public void CheckAgainstLegend_UnknownSymbol_ReportsOncePerRow() {
            MapSpecParser.CheckAgainstLegend(Spec(SpecText.Replace("gg=", "zz=")), Legend(), errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("'z'", errors[0]);
        }

        [Test]
        public void CheckAgainstLegend_SymbolOnWrongLayer_ReportsLayer() {
            MapSpecParser.CheckAgainstLegend(Spec(SpecText.Replace("T.f", "T.g")), Legend(), errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("belongs to layer [Ground]", errors[0]);
        }

        [Test]
        public void CheckAgainstLegend_LogicLayerOrUnknownLayer_ReportsEach() {
            var text = SpecText + "[Logic]\n...\n...\n[Water]\n...\n...\n";

            MapSpecParser.CheckAgainstLegend(Spec(text), Legend(), errors);

            Assert.AreEqual(2, errors.Count);
            StringAssert.Contains("can't be written directly", errors[0]);
            StringAssert.Contains("[Water]", errors[1]);
        }

        [Test]
        public void Parse_Region_ReadsItAndChecksGridsAgainstIt() {
            var text = SpecText.Replace("legend: grassland", "legend: grassland\nregion: 11 20 2 1")
                .Replace("gg=\ng==\n", "g=\n").Replace("T.f\n...\n", "?f\n");
            var spec = Spec(text);

            MapSpecParser.CheckAgainstLegend(spec, Legend(), errors);

            CollectionAssert.IsEmpty(errors);
            Assert.IsTrue(spec.hasRegion);
            Assert.AreEqual(new RectInt(11, 20, 2, 1), spec.grid);
        }

        [Test]
        public void Parse_RegionOutsideBounds_ReportsLine() {
            Spec(SpecText.Replace("legend: grassland", "legend: grassland\nregion: 12 20 2 2"));

            Assert.IsTrue(errors.Exists(e => e.StartsWith("forest.txt:6:") && e.Contains("inside the bounds")));
        }

        [Test]
        public void CheckAgainstLegend_KeepSymbol_IsAllowed() {
            MapSpecParser.CheckAgainstLegend(Spec(SpecText.Replace("T.f", "T??")), Legend(), errors);

            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void ParseLegend_KeepSymbol_IsReserved() {
            MapSpecParser.ParseLegend(LegendJson.Replace(@"""symbol"": ""f""", @"""symbol"": ""?"""), "grassland.json", errors);

            Assert.IsTrue(errors.Exists(e => e.Contains("'?' is reserved")));
        }

        [Test]
        public void Layout_Region_PlacesGridAtRegion() {
            var text = SpecText.Replace("legend: grassland", "legend: grassland\nregion: 11 20 2 1")
                .Replace("gg=\ng==\n", "g=\n").Replace("T.f\n...\n", "..\n");

            var placements = MapSpecLayout.Layout(Spec(text), Legend());

            Assert.IsTrue(placements.Contains(new MapTilePlacement("Ground", new Vector2Int(11, 20), "Assets/Grass.asset")));
            Assert.IsTrue(placements.Contains(new MapTilePlacement("Ground", new Vector2Int(12, 20), "Assets/Path.asset")));
        }

        [Test]
        public void Layout_TopRowIsHighestY() {
            var placements = MapSpecLayout.Layout(Spec(), Legend());

            // Row 0 is y = 21, row 1 is y = 20.
            Assert.IsTrue(placements.Contains(new MapTilePlacement("Ground", new Vector2Int(12, 21), "Assets/Path.asset")));
            Assert.IsTrue(placements.Contains(new MapTilePlacement("Ground", new Vector2Int(10, 20), "Assets/Grass.asset")));
            Assert.IsTrue(placements.Contains(new MapTilePlacement("Trees", new Vector2Int(10, 21), "Assets/Tree.asset")));
        }

        [Test]
        public void Layout_LogicFromTopmostLayerWithLogic() {
            var placements = MapSpecLayout.Layout(Spec(), Legend());
            var logic = placements.FindAll(p => p.layer == MapSpecParser.LogicLayer);

            Assert.AreEqual(6, logic.Count);
            // The tree blocks its tile, and the flower has no logic, so the grass's shows through.
            Assert.IsTrue(logic.Contains(new MapTilePlacement("Logic", new Vector2Int(10, 21), "Assets/Wall.asset")));
            Assert.IsTrue(logic.Contains(new MapTilePlacement("Logic", new Vector2Int(12, 21), "Assets/Walk.asset")));
        }

        [Test]
        public void Layout_Footprint_CoversEachCellOverLowerLayers() {
            var legend = Legend();
            legend.entries[2].footprint = new MapFootprint { width = 2, height = 1 };

            var logic = MapSpecLayout.Layout(Spec(), legend).FindAll(p => p.layer == MapSpecParser.LogicLayer);

            // The tree at (10, 21) also blocks (11, 21), over the grass there.
            Assert.AreEqual(6, logic.Count);
            Assert.IsTrue(logic.Contains(new MapTilePlacement("Logic", new Vector2Int(10, 21), "Assets/Wall.asset")));
            Assert.IsTrue(logic.Contains(new MapTilePlacement("Logic", new Vector2Int(11, 21), "Assets/Wall.asset")));
            Assert.IsTrue(logic.Contains(new MapTilePlacement("Logic", new Vector2Int(10, 20), "Assets/Walk.asset")));
        }

        [Test]
        public void Layout_FootprintPastBounds_DropsOutsideCells() {
            var legend = Legend();
            legend.entries[2].footprint = new MapFootprint { width = 2, height = 2 };

            var logic = MapSpecLayout.Layout(Spec(SpecText.Replace("T.f", "..T")), legend).FindAll(p => p.layer == MapSpecParser.LogicLayer);

            // The tree at (12, 21) is in the top-right corner, so only its own cell is inside.
            Assert.AreEqual(6, logic.Count);
            Assert.IsTrue(logic.Contains(new MapTilePlacement("Logic", new Vector2Int(12, 21), "Assets/Wall.asset")));
            Assert.IsFalse(logic.Exists(p => p.cell.x > 12 || p.cell.y > 21));
        }
    }
}
