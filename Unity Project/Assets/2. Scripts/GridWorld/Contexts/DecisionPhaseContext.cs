namespace GridWorld {
    public class DecisionPhaseContext : WorldFunctionContext
                                      , IPerm_ReadEntities
                                      , IPerm_ReadTiles
                                      , IPerm_RequestMove
                                      , IPerm_PlayerControl
                                      , IPerm_WriteInteractAttempts
                                      , IPerm_ClearInteractAttempts { }
}
