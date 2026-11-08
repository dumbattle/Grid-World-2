namespace GridWorld {
    /// <summary>
    /// Turns frame time into whole ticks at a fixed rate. Runs at most
    /// <see cref="MaxTicksPerFrame"/> per frame and drops any time beyond that.
    /// </summary>
    public class TickClock {
        public const int TicksPerSecond = 60;
        public const int MaxTicksPerFrame = 4;
        public const float TickSeconds = 1f / TicksPerSecond;

        float accumulated;

        /// <summary>How far the view is between the last two ticks, from 0 to 1.</summary>
        public float alpha => accumulated / TickSeconds;

        /// <summary>Adds frame time and returns how many ticks to run.</summary>
        public int Advance(float deltaTime) {
            accumulated += deltaTime;

            int ticks = 0;
            while (accumulated >= TickSeconds && ticks < MaxTicksPerFrame) {
                accumulated -= TickSeconds;
                ticks++;
            }

            if (accumulated >= TickSeconds) {
                // Keep the fraction so the view's blend stays smooth.
                accumulated %= TickSeconds;
            }
            return ticks;
        }
    }
}
