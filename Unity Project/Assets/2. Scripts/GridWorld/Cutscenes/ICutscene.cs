namespace GridWorld {
    /// <summary>
    /// A cutscene, built fresh each time it starts. Implementations hold only fixed settings, such
    /// as the entities or feature IDs they act on: one instance may be started many times, and a
    /// baked one lives in an asset. Anything that changes while it runs belongs in its sequences.
    /// </summary>
    public interface ICutscene {
        /// <summary>Adds the cutscene's steps. <paramref name="ctx"/> reads state as it is when the cutscene starts.</summary>
        void Build(CutsceneBuildContext ctx, CutsceneBuilder b);
    }
}
