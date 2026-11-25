using System.Collections.Generic;
using NUnit.Framework;

namespace GridWorld.Tests {
    public class FeatureEditParserTests {
        const string Headers = "area: meadow\nworld: Assets/World Data.asset\n";

        static FeatureEditFile Parse(string text, List<string> errors) {
            return FeatureEditParser.Parse(text, "edits.txt", errors);
        }

        [Test]
        public void Parse_EachKindOfEdit_ReadsItsValues() {
            var errors = new List<string>();
            var file = Parse(Headers +
                "# A comment\n" +
                "move guard_1 from 12 5 to 12 6\n" +
                "face guard_1 from down to left\n" +
                "set guard_1 PatrolAIBehaviour.points[1].tile from 0 3 to 0 4\n" +
                "remove mob_old at 4 3\n", errors);

            CollectionAssert.IsEmpty(errors);
            Assert.AreEqual("meadow", file.areaId);
            Assert.AreEqual("Assets/World Data.asset", file.world);
            Assert.AreEqual(4, file.edits.Count);

            var move = file.edits[0];
            Assert.AreEqual(FeatureEditKind.Move, move.kind);
            CollectionAssert.AreEqual(new[] { "12", "5" }, move.from);
            CollectionAssert.AreEqual(new[] { "12", "6" }, move.to);
            Assert.AreEqual(4, move.line);

            Assert.AreEqual("facing", file.edits[1].path);
            CollectionAssert.AreEqual(new[] { "left" }, file.edits[1].to);

            Assert.AreEqual("PatrolAIBehaviour.points[1].tile", file.edits[2].path);
            CollectionAssert.AreEqual(new[] { "0", "3" }, file.edits[2].from);

            Assert.AreEqual(FeatureEditKind.Remove, file.edits[3].kind);
            CollectionAssert.AreEqual(new[] { "4", "3" }, file.edits[3].from);
        }

        [Test]
        public void Parse_QuotedValue_KeepsItsSpaces() {
            var errors = new List<string>();
            var file = Parse(Headers + "set guard_1 NpcBehaviour.definition from none to \"Assets/8. Data/Guard.asset\"\n", errors);

            CollectionAssert.IsEmpty(errors);
            CollectionAssert.AreEqual(new[] { "Assets/8. Data/Guard.asset" }, file.edits[0].to);
        }

        [Test]
        public void Parse_NoHeaders_ReportsBothAndReturnsNull() {
            var errors = new List<string>();
            var file = Parse("move guard_1 from 1 1 to 1 2\n", errors);

            Assert.IsNull(file);
            Assert.AreEqual(2, errors.Count);
        }

        [TestCase("jump guard_1 from 1 1 to 1 2")]
        [TestCase("move guard_1 1 1 to 1 2")]
        [TestCase("move guard_1 from 1 x to 1 2")]
        [TestCase("face guard_1 from down")]
        [TestCase("set guard_1 points from 1 to 2")]
        [TestCase("set guard_1 SpinAIBehaviour.minWaitTicks from 1")]
        [TestCase("remove guard_1 from 4 3")]
        [TestCase("move guard 1 from 1 1 to 1 2")]
        [TestCase("move guard_1 from 1 1 to 1 1")]
        [TestCase("set guard_1 NpcBehaviour.definition from none to \"Assets/x")]
        [TestCase("speed: 3")]
        public void Parse_BadLine_ReportsItsLineNumber(string line) {
            var errors = new List<string>();
            Parse(Headers + line + "\n", errors);

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.StartsWith("edits.txt:3:", errors[0]);
        }

        [Test]
        public void Parse_SameFieldTwice_ReportsTheSecond() {
            var errors = new List<string>();
            Parse(Headers + "move guard_1 from 1 1 to 1 2\nmove GUARD_1 from 1 2 to 1 3\n", errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.StartsWith("edits.txt:4:", errors[0]);
        }

        [Test]
        public void Parse_EditAfterRemove_ReportsIt() {
            var errors = new List<string>();
            Parse(Headers + "remove guard_1 at 1 1\nface guard_1 from down to up\n", errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("removed on line 3", errors[0]);
        }

        [Test]
        public void Parse_RemoveAfterEdit_ReportsIt() {
            var errors = new List<string>();
            Parse(Headers + "face guard_1 from down to up\nremove guard_1 at 1 1\n", errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("can't also be removed", errors[0]);
        }

        [Test]
        public void Parse_ToAsAQuotedValue_IsNotTheKeyword() {
            var errors = new List<string>();
            var file = Parse(Headers + "set sign_1 SignBehaviour.text from \"to\" to \"go to town\"\n", errors);

            CollectionAssert.IsEmpty(errors);
            CollectionAssert.AreEqual(new[] { "to" }, file.edits[0].from);
            CollectionAssert.AreEqual(new[] { "go to town" }, file.edits[0].to);
        }
    }
}
