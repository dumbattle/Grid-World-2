namespace GridWorld {
    /// <summary>
    /// Builds the world before the first tick, including world feature registration. Don't use it
    /// during the phases.
    /// </summary>
    public class WorldSetupContext : WorldFunctionContext
                                   , IPerm_SetupWorld
                                   , IPerm_ReadEntities { }
}
