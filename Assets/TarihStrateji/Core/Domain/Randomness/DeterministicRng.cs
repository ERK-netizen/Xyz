namespace TarihStrateji.Core.Domain.Randomness
{
    /// <summary>
    /// Durumsuz deterministik PRNG (CLAUDE.md §2.4): sonuç yalnız
    /// <c>worldSeed + subsystemId + day + eventId + entityId</c> anahtarına ve çekiliş sırasına
    /// bağlıdır; sistemlerin çalışma sırası veya başka sistemlerin kaç sayı çektiği sonucu
    /// değiştirmez. Karıştırıcı SplitMix64 sonlandırıcısıdır; her bileşen ayrı bir sabitle
    /// katılır, böylece bileşenler yer değiştirdiğinde aynı sayı çıkmaz.
    /// </summary>
    public static class DeterministicRng
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;

        /// <summary>Anahtarın <paramref name="draw"/>'uncu 64-bit değeri.</summary>
        public static ulong Value(ulong worldSeed, RngSubsystem subsystem, int day, ulong eventId, ulong entityId, ulong draw)
        {
            ulong h = Mix(worldSeed ^ 0x243F6A8885A308D3UL);
            h = Mix(h ^ ((ulong)subsystem * 0x13198A2E03707344UL));
            h = Mix(h ^ ((ulong)(uint)day * 0xA4093822299F31D0UL));
            h = Mix(h ^ (eventId * 0x082EFA98EC4E6C89UL));
            h = Mix(h ^ (entityId * 0x452821E638D01377UL));
            return Mix(h ^ ((draw + 1UL) * Golden));
        }

        /// <summary>SplitMix64 sonlandırıcısı (Steele, Lea, Flood 2014).</summary>
        public static ulong Mix(ulong z)
        {
            unchecked
            {
                z += Golden;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
