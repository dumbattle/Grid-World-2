using System;
using UnityEngine;

namespace GridWorld.Tests {
    /// <summary>A cutscene that can be baked, for tests of add-ons and features that hold one.</summary>
    [Serializable]
    public class SerializableTestCutscene : ICutscene {
        [SerializeField] string note = "";

        public string value => note;

        public SerializableTestCutscene() { }

        public SerializableTestCutscene(string note) {
            this.note = note;
        }

        public void Build(CutsceneBuildContext ctx, CutsceneBuilder b) { }
    }
}
