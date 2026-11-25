namespace GridWorld {
    public enum FeatureEditKind {
        // Puts the feature's object on another tile.
        Move,
        // Changes the feature's facing.
        Face,
        // Changes one serialized field on one of the object's components.
        Set,
        // Deletes the feature's object.
        Remove,
    }
}
