namespace GridWorld {
    public enum FeatureEditStatus {
        // The feature has the expected 'from' value, so applying changes it.
        Change,
        // The feature already has the 'to' value, so applying leaves it alone.
        AlreadyApplied,
        // Anything else. While any edit conflicts, nothing in the file is applied.
        Conflict,
    }
}
