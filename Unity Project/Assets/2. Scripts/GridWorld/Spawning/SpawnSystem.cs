namespace GridWorld {
    /// <summary>
    /// Ticks every spawn zone in the spawn phase, at the start of each tick, in bake order. A zone
    /// sees the mobs spawned by the zones before it.
    /// </summary>
    public class SpawnSystem {
        public void Run(SpawnContext ctx) {
            var zones = ctx.GetSpawnZones();
            for (int i = 0; i < zones.Count; i++) {
                zones[i].Tick(ctx);
            }
        }
    }
}
