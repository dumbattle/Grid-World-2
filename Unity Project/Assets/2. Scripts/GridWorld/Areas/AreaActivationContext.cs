namespace GridWorld {
    /// <summary>
    /// For updating which areas are active, once per tick after the phases.
    /// </summary>
    public class AreaActivationContext : WorldFunctionContext
                                       , IPerm_ReadAreas
                                       , IPerm_UpdateAreas { }
}
