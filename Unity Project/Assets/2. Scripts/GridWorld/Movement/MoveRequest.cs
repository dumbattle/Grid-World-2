using LPE;

namespace GridWorld {
    public readonly struct MoveRequest {
        public readonly Direction4 direction;
        public readonly bool turnOnly;
        // Read only when a step starts, so speed never changes mid-step.
        public readonly bool run;

        public MoveRequest(Direction4 direction, bool turnOnly, bool run) {
            this.direction = direction;
            this.turnOnly = turnOnly;
            this.run = run;
        }
    }
}
