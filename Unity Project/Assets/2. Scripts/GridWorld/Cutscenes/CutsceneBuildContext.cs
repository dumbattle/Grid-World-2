namespace GridWorld {
    /// <summary>What a cutscene can read while it's built. Its steps do the writing.</summary>
    public class CutsceneBuildContext : WorldFunctionContext
                                      , IPerm_ReadEntities
                                      , IPerm_ReadTiles { }
}
