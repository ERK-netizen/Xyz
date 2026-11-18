namespace TarihStrateji.Core.Tests.Support
{
    /// <summary>Testler için platformdan bağımsız SplitMix64 üreteci (altın değerler buna dayanır).</summary>
    public sealed class TestRng
    {
        private ulong _state;

        public TestRng(ulong seed)
        {
            _state = seed;
        }

        public ulong NextUInt64()
        {
            ulong z = _state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>[0, maxExclusive).</summary>
        public int Next(int maxExclusive) => (int)(NextUInt64() % (ulong)maxExclusive);

        public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);

        public bool Chance(int percent) => Next(100) < percent;
    }
}
