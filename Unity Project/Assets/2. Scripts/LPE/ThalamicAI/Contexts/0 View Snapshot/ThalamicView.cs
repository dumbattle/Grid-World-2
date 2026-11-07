namespace LPE.AI.ThalamicAI {
    public abstract class ThalamicView<TWorld> {
        static SnapshotContext ctx = new SnapshotContext();
        public void Capture(TWorld world) { 
            Capture(ctx, world);
        
        }
        protected abstract void Capture(SnapshotContext ctx, TWorld world);
    }
}
