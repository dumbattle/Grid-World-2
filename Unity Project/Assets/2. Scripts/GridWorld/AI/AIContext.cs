namespace GridWorld {
    public class AIContext : WorldFunctionContext
                           , IPerm_ReadTiles
                           , IPerm_ReadEntities
                           , IPerm_ReadAreas
                           , IPerm_RequestMove
                           , IPerm_UseRandom { }
}
