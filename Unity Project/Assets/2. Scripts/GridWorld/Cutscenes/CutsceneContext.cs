namespace GridWorld {
    /// <summary>What cutscene steps use to act on the world. The cutscene system hands it to them.</summary>
    public class CutsceneContext : WorldFunctionContext
                                 , IPerm_ReadEntities
                                 , IPerm_ReadTiles
                                 , IPerm_RequestMove
                                 , IPerm_Teleport { }
}
