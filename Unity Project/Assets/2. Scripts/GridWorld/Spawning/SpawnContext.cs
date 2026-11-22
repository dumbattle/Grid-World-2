namespace GridWorld {
    public class SpawnContext : WorldFunctionContext
                              , IPerm_ReadTiles
                              , IPerm_ReadEntities
                              , IPerm_ReadAreas
                              , IPerm_ReadCamera
                              , IPerm_UseRandom
                              , IPerm_SpawnEntities
                              , IPerm_DespawnEntities { }
}
