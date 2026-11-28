namespace GridWorld {
    /// <summary>
    /// What <see cref="CutsceneSystem"/> uses to start, advance, and end cutscenes. It hands its
    /// steps a <see cref="CutsceneContext"/> and its build functions a <see cref="CutsceneBuildContext"/>,
    /// but never acts on the world itself.
    /// </summary>
    public class CutsceneSystemContext : WorldFunctionContext
                                       , IPerm_ReadEntities
                                       , IPerm_RunCutscenes { }
}
