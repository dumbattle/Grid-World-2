namespace GridWorld {
    /// <summary>
    /// One line of a feature edit file. Every edit names the value it expects to find, so applying a
    /// file twice, or to an area that has changed since, can't do the wrong thing.
    /// </summary>
    public class FeatureEdit {
        public FeatureEditKind kind;
        public string id;
        /// <summary>For <see cref="FeatureEditKind.Set"/>, the component and field, like PatrolAIBehaviour.points[1].tile.</summary>
        public string path;
        /// <summary>The expected current value, as tokens. For a remove, the tile the feature must be on.</summary>
        public string[] from;
        /// <summary>The new value, as tokens. Empty for a remove.</summary>
        public string[] to;
        public int line;
        /// <summary>The line as written, without surrounding spaces.</summary>
        public string text;
    }
}
