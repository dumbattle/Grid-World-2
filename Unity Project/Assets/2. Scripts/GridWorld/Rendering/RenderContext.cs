namespace GridWorld {
    /// <summary>
    /// Read-only access for the view and camera, outside the phases.
    /// </summary>
    public class RenderContext : WorldFunctionContext
                               , IPerm_ReadEntities
                               , IPerm_ReadCamera { }
}
