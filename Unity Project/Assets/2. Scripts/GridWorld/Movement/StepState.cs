namespace GridWorld {
    public enum StepState {
        Idle,
        Moving,
        // The step reached its end tile. It clears to idle in the next movement phase unless a new step starts.
        Finished,
    }
}
