using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace GridWorld.Tests {
    /// <summary>
    /// Reads the committed files in <c>Maps/</c>. Tools/mapread.py writes specs and legends too, so this
    /// catches a format change made on one side but not the other.
    /// </summary>
    public class MapSpecFilesTests {
        [Test]
        public void CommittedLegends_ReadCleanly() {
            var errors = new List<string>();
            foreach (var path in Files(MapPaths.legends, "*.json")) {
                var legend = MapSpecParser.ParseLegend(File.ReadAllText(path), Path.GetFileName(path), errors);
                if (legend != null) {
                    MapSpecChecks.FindLegendErrors(legend, Path.GetFileNameWithoutExtension(path), errors);
                }
            }
            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void CommittedSpecs_ReadCleanlyAgainstTheirLegends() {
            var errors = new List<string>();
            foreach (var path in Files(MapPaths.areas, "*.txt")) {
                // Building or applying depends on which areas exist, so only the file itself is checked.
                MapSpecMenu.Read(path, errors, out _);
            }
            CollectionAssert.IsEmpty(errors);
        }

        static string[] Files(string folder, string pattern) {
            if (!Directory.Exists(folder)) {
                Assert.Ignore($"No folder at {folder}.");
            }
            return Directory.GetFiles(folder, pattern);
        }
    }
}
