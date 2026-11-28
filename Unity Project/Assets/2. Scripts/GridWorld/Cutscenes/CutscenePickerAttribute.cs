using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Put on a <c>[SerializeReference]</c> <see cref="ICutscene"/> field to choose its class from a
    /// list in the Inspector, then edit its fields. Lists serializable classes with a parameterless
    /// constructor.
    /// </summary>
    public class CutscenePickerAttribute : PropertyAttribute { }
}
