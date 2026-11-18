using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Domain.Randomness
{
    /// <summary>
    /// Tek bir anahtar üzerinde sıralı çekilişler. Kalıcı durum değildir (kayda yazılmaz):
    /// aynı anahtarla yeniden oluşturulduğunda aynı diziyi verir.
    /// </summary>
    public struct RngStream
    {
        private readonly ulong _worldSeed;
        private readonly RngSubsystem _subsystem;
        private readonly int _day;
        private readonly ulong _eventId;
        private readonly ulong _entityId;
        private ulong _draw;

        public RngStream(ulong worldSeed, RngSubsystem subsystem, int day, ulong eventId, ulong entityId)
        {
            _worldSeed = worldSeed;
            _subsystem = subsystem;
            _day = day;
            _eventId = eventId;
            _entityId = entityId;
            _draw = 0UL;
        }

        public ulong DrawCount => _draw;

        public ulong NextUInt64()
        {
            return DeterministicRng.Value(_worldSeed, _subsystem, _day, _eventId, _entityId, _draw++);
        }

        /// <summary>[0, maxExclusive) aralığında yansız tamsayı (reddetme örneklemesi).</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            ulong bound = (ulong)maxExclusive;
            ulong threshold = (0UL - bound) % bound; // 2^64 mod bound
            while (true)
            {
                ulong value = NextUInt64();
                if (value >= threshold)
                {
                    return (int)(value % bound);
                }
            }
        }

        /// <summary>[min, maxExclusive) aralığında yansız tamsayı.</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return minInclusive + NextInt(checked(maxExclusive - minInclusive));
        }

        /// <summary>[0, 1) aralığında, 10^-6 adımlı Fixed64.</summary>
        public Fixed64 NextFraction()
        {
            return Fixed64.FromRaw(NextInt((int)Fixed64.Scale));
        }

        /// <summary><paramref name="probability"/> olasılıkla true; [0,1] dışı değerler sıkıştırılır.</summary>
        public bool Chance(Fixed64 probability)
        {
            if (probability.Raw <= 0L)
            {
                NextUInt64(); // çekiliş sayısı olasılıktan bağımsız kalsın
                return false;
            }

            if (probability.Raw >= Fixed64.Scale)
            {
                NextUInt64();
                return true;
            }

            return NextFraction().Raw < probability.Raw;
        }
    }
}
