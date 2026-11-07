using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class EntityDefinitionSOTests {
        [Test]
        public void CreateInstance_Defaults_MatchStartingMovementDurations() {
            var definition = ScriptableObject.CreateInstance<EntityDefinitionSO>();

            try {
                Assert.AreEqual(16, definition.walkTicks);
                Assert.AreEqual(8, definition.runTicks);
            }
            finally {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
