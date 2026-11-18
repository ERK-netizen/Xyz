using System;

namespace TarihStrateji.Core.Domain.Numerics
{
    /// <summary>
    /// Deterministik, tamamen tamsayı trigonometri. Kayan nokta kullanılmaz
    /// (CLAUDE.md §2.4). İç hesaplar 10^9 ölçekte yapılır; sonuçlar Fixed64'e (10^6) en yakına
    /// yuvarlanır. Açılar derece cinsindendir; yalnız <see cref="CentralAngleRadians"/> radyan
    /// döndürür.
    /// </summary>
    public static class FixedTrig
    {
        private const long S9 = 1_000_000_000L;

        // π ve π/2, 10^9 ölçekte (3.141592654 / 1.570796327).
        private const long Pi9 = 3_141_592_654L;
        private const long HalfPi9 = 1_570_796_327L;

        private const long FullTurnRaw = 360L * Fixed64.Scale;
        private const long HalfTurnRaw = 180L * Fixed64.Scale;
        private const long QuarterTurnRaw = 90L * Fixed64.Scale;
        private const long EighthTurnRaw = 45L * Fixed64.Scale;

        /// <summary>π, Fixed64 (3.141593).</summary>
        public static readonly Fixed64 Pi = Fixed64.FromRaw(3_141_593L);

        /// <summary>Derece cinsinden sinüs.</summary>
        public static Fixed64 SinDegrees(Fixed64 degrees)
        {
            return Fixed64.FromRaw(Round9To6(Sin9FromDegreesRaw(degrees.Raw)));
        }

        /// <summary>Derece cinsinden kosinüs.</summary>
        public static Fixed64 CosDegrees(Fixed64 degrees)
        {
            return Fixed64.FromRaw(Round9To6(Cos9FromDegreesRaw(degrees.Raw)));
        }

        /// <summary>Dereceyi radyana çevirir.</summary>
        public static Fixed64 DegreesToRadians(Fixed64 degrees)
        {
            return Fixed64.FromRaw(RoundDiv(degrees.Raw, Pi9, HalfTurnRaw * 1000L));
        }

        /// <summary>Radyanı dereceye çevirir.</summary>
        public static Fixed64 RadiansToDegrees(Fixed64 radians)
        {
            // rad·10^6 · 180·10^9 / (π·10^9) = derece·10^6, tek yuvarlama.
            return Fixed64.FromRaw(RoundDiv(radians.Raw, HalfTurnRaw * 1000L, Pi9));
        }

        /// <summary>Arksinüs, derece cinsinden, [-90, 90]. Girdi [-1, 1] dışındaysa hata.</summary>
        public static Fixed64 AsinDegrees(Fixed64 value)
        {
            if (value.Raw < -Fixed64.Scale || value.Raw > Fixed64.Scale)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "FixedTrig.AsinDegrees: [-1, 1] dışı.");
            }

            long target9 = (value.Raw < 0L ? -value.Raw : value.Raw) * 1000L;
            long rad9 = Asin9(target9);
            long degRaw = RoundDiv(rad9, HalfTurnRaw, Pi9);
            return Fixed64.FromRaw(value.Raw < 0L ? -degRaw : degRaw);
        }

        /// <summary>
        /// Küre üzerindeki iki nokta arasındaki merkez açısı (radyan, [0, π]). Kiriş yöntemi:
        /// birim vektör farkının boyu c, açı = 2·asin(c/2). Küçük mesafelerde de hassastır ve
        /// boylam farkı ±180° dikişinde doğal olarak en kısa yoldan hesaplanır.
        /// </summary>
        public static Fixed64 CentralAngleRadians(Fixed64 latA, Fixed64 lonA, Fixed64 latB, Fixed64 lonB)
        {
            UnitVector9(latA.Raw, lonA.Raw, out long ax, out long ay, out long az);
            UnitVector9(latB.Raw, lonB.Raw, out long bx, out long by, out long bz);

            long dx = ax - bx;
            long dy = ay - by;
            long dz = az - bz;

            // dx, dy, dz ∈ [-2·10^9, 2·10^9]; kareler 10^18 ölçekte, toplam ≤ 4·10^18 (+ yuvarlama payı).
            ulong sum = (ulong)(dx * dx) + (ulong)(dy * dy) + (ulong)(dz * dz);
            long chord9 = (long)UInt128Math.SqrtRounded(0UL, sum); // 10^9 ölçek, ≤ 2·10^9
            long half9 = chord9 / 2L + (chord9 % 2L);              // yarı kiriş, yukarı yuvarlanmış
            if (half9 > S9)
            {
                half9 = S9;
            }

            long angle9 = 2L * Asin9(half9);
            return Fixed64.FromRaw(Round9To6(angle9));
        }

        // ------------------------------------------------------------------ iç hesaplar (10^9)

        private static void UnitVector9(long latRaw, long lonRaw, out long x, out long y, out long z)
        {
            long cosLat = Cos9FromDegreesRaw(latRaw);
            long sinLat = Sin9FromDegreesRaw(latRaw);
            long cosLon = Cos9FromDegreesRaw(lonRaw);
            long sinLon = Sin9FromDegreesRaw(lonRaw);
            x = Mul9(cosLat, cosLon);
            y = Mul9(cosLat, sinLon);
            z = sinLat;
        }

        private static long Cos9FromDegreesRaw(long degreesRaw)
        {
            // cos(d) = sin(d + 90); toplam taşmasın diye önce 360'a indirgenir.
            long reduced = degreesRaw % FullTurnRaw;
            return Sin9FromDegreesRaw(reduced + QuarterTurnRaw);
        }

        private static long Sin9FromDegreesRaw(long degreesRaw)
        {
            long d = degreesRaw % FullTurnRaw;
            if (d < 0L)
            {
                d += FullTurnRaw;
            }

            bool negative = false;
            if (d >= HalfTurnRaw)
            {
                d -= HalfTurnRaw;
                negative = true;
            }

            if (d > QuarterTurnRaw)
            {
                d = HalfTurnRaw - d;
            }

            // d ∈ [0, 90]
            long result = d <= EighthTurnRaw
                ? SinSeries9(DegreesRawToRad9(d))
                : CosSeries9(DegreesRawToRad9(QuarterTurnRaw - d));
            return negative ? -result : result;
        }

        private static long DegreesRawToRad9(long degreesRaw)
        {
            // degreesRaw ≤ 45·10^6, Pi9 ≈ 3.1·10^9 → çarpım ≤ 1.4·10^17.
            return RoundDiv(degreesRaw, Pi9, HalfTurnRaw);
        }

        /// <summary>sin(x), x ∈ [0, π/2] (10^9). x &gt; π/4 için kosinüs özdeşliği kullanılır.</summary>
        private static long Sin9Radians(long x9)
        {
            return x9 <= HalfPi9 / 2L ? SinSeries9(x9) : CosSeries9(HalfPi9 - x9);
        }

        /// <summary>Taylor serisi, |x| ≤ π/4 (10^9). Hata &lt; 10^-13.</summary>
        private static long SinSeries9(long x9)
        {
            long x2 = Mul9(x9, x9);
            long term = x9;
            long sum = x9;
            for (long n = 2L; n <= 14L; n += 2L)
            {
                term = -Mul9(term, x2) / (n * (n + 1L));
                sum += term;
            }

            return sum;
        }

        /// <summary>Taylor serisi, |x| ≤ π/4 (10^9). Hata &lt; 10^-13.</summary>
        private static long CosSeries9(long x9)
        {
            long x2 = Mul9(x9, x9);
            long term = S9;
            long sum = S9;
            for (long n = 1L; n <= 14L; n += 2L)
            {
                term = -Mul9(term, x2) / (n * (n + 1L));
                sum += term;
            }

            return sum;
        }

        /// <summary>asin(y), y ∈ [0, 1] (10^9) → radyan (10^9); sinüsün monotonluğuyla ikili arama.</summary>
        private static long Asin9(long y9)
        {
            if (y9 <= 0L)
            {
                return 0L;
            }

            if (y9 >= S9)
            {
                return HalfPi9;
            }

            long low = 0L;
            long high = HalfPi9;
            while (high - low > 1L)
            {
                long mid = low + ((high - low) / 2L);
                if (Sin9Radians(mid) <= y9)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            long errLow = y9 - Sin9Radians(low);
            long errHigh = Sin9Radians(high) - y9;
            return errHigh < errLow ? high : low;
        }

        /// <summary>a·b / 10^9, |a|, |b| ≤ ~3·10^9 için taşmasız (çarpım ≤ 9.2·10^18).</summary>
        private static long Mul9(long a, long b)
        {
            return RoundDiv(a, b, S9);
        }

        /// <summary>(a·b)/d en yakına, yarıda sıfırdan uzağa; 128-bit ara çarpım.</summary>
        private static long RoundDiv(long a, long b, long d)
        {
            bool negative = ((a < 0L) != (b < 0L)) != (d < 0L);
            ulong ua = a < 0L ? (ulong)(-a) : (ulong)a;
            ulong ub = b < 0L ? (ulong)(-b) : (ulong)b;
            ulong ud = d < 0L ? (ulong)(-d) : (ulong)d;
            UInt128Math.Multiply(ua, ub, out ulong hi, out ulong lo);
            ulong q = UInt128Math.Divide(hi, lo, ud, out ulong r);
            if (r * 2UL >= ud)
            {
                q++;
            }

            long result = checked((long)q);
            return negative ? -result : result;
        }

        private static long Round9To6(long value9)
        {
            long q = value9 / 1000L;
            long r = value9 % 1000L;
            if (r >= 500L)
            {
                q++;
            }
            else if (r <= -500L)
            {
                q--;
            }

            return q;
        }
    }
}
