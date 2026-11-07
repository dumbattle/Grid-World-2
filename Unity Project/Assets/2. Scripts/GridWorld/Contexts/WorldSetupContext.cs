namespace GridWorld {
    /// <summary>
    /// Builds the world before the first tick. Don't use it during the phases.
    /// </summary>
    public class WorldSetupContext : WorldFunctionContext
                                   , IPerm_SetupWorld { }
}
