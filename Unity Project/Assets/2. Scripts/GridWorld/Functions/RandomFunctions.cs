namespace GridWorld {
    public static class RandomFunctions {
        /// <summary>A random int from <paramref name="min"/> up to but not including <paramref name="maxExclusive"/>.</summary>
        public static int RandomRange<T>(this T ctx, int min, int maxExclusive)
                    where T : IWorldFunctionContext {
            return ctx.data.random.Next(min, maxExclusive);
        }
    }
}
