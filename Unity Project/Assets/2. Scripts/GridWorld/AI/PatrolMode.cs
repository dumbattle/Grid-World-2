namespace GridWorld {
    public enum PatrolMode {
        // After the last point, walk back to the first.
        Loop,
        // After the last point, walk the points in reverse.
        PingPong,
    }
}
