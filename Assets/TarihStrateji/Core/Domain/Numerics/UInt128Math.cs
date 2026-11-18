using System;

namespace TarihStrateji.Core.Domain.Numerics
{
    /// <summary>
    /// .NET Standard 2.1'de <c>Math.BigMul(ulong, ulong, out ulong)</c> ve <c>UInt128</c> yoktur.
    /// Bu sınıf Fixed64 çarpma/bölme/karekök için gereken 128-bit işaretsiz ara hesapları
    /// tamamen tamsayı ile, platformdan bağımsız ve deterministik olarak yapar.
    /// </summary>
    internal static class UInt128Math
    {
        /// <summary>64 x 64 → 128 bit çarpım (hi:lo).</summary>
        public static void Multiply(ulong a, ulong b, out ulong hi, out ulong lo)
        {
            ulong aLo = a & 0xFFFFFFFFUL;
            ulong aHi = a >> 32;
            ulong bLo = b & 0xFFFFFFFFUL;
            ulong bHi = b >> 32;

            ulong p0 = aLo * bLo;
            ulong p1 = aLo * bHi;
            ulong p2 = aHi * bLo;
            ulong p3 = aHi * bHi;

            ulong middle = (p0 >> 32) + (p1 & 0xFFFFFFFFUL) + (p2 & 0xFFFFFFFFUL);
            lo = (p0 & 0xFFFFFFFFUL) | (middle << 32);
            hi = p3 + (p1 >> 32) + (p2 >> 32) + (middle >> 32);
        }

        /// <summary>
        /// (hi:lo) / divisor. Bölümün 64 bite sığması gerekir (hi &lt; divisor); sığmıyorsa
        /// <see cref="OverflowException"/> fırlatılır.
        /// </summary>
        public static ulong Divide(ulong hi, ulong lo, ulong divisor, out ulong remainder)
        {
            if (divisor == 0UL)
            {
                throw new DivideByZeroException("Fixed64: sıfıra bölme.");
            }

            if (hi == 0UL)
            {
                remainder = lo % divisor;
                return lo / divisor;
            }

            if (hi >= divisor)
            {
                throw new OverflowException("Fixed64: 128-bit bölüm 64 bite sığmıyor.");
            }

            // Klasik kaydır-çıkar uzun bölme. Her adımda kalan < divisor kalır.
            ulong rem = hi;
            ulong quotient = 0UL;
            for (int bit = 63; bit >= 0; bit--)
            {
                bool carry = (rem & 0x8000000000000000UL) != 0UL;
                rem = (rem << 1) | ((lo >> bit) & 1UL);
                quotient <<= 1;
                if (carry || rem >= divisor)
                {
                    rem -= divisor;
                    quotient |= 1UL;
                }
            }

            remainder = rem;
            return quotient;
        }

        /// <summary>(aHi:aLo) ile (bHi:bLo) karşılaştırması: -1, 0, 1.</summary>
        public static int Compare(ulong aHi, ulong aLo, ulong bHi, ulong bLo)
        {
            if (aHi != bHi)
            {
                return aHi < bHi ? -1 : 1;
            }

            if (aLo != bLo)
            {
                return aLo < bLo ? -1 : 1;
            }

            return 0;
        }

        /// <summary>(hi:lo) değerinin tamsayı karekökü, en yakına (yarıda yukarı) yuvarlanmış.</summary>
        public static ulong SqrtRounded(ulong hi, ulong lo)
        {
            if (hi == 0UL && lo == 0UL)
            {
                return 0UL;
            }

            // floor(sqrt(n)) ikili arama ile; n < 2^128 olduğundan sonuç < 2^64.
            // 64 bitlik bir sayının karekökü en fazla 2^32 - 1'dir.
            ulong low = 0UL;
            ulong high = hi == 0UL ? 0xFFFFFFFFUL : 0xFFFFFFFFFFFFFFFFUL;

            while (low < high)
            {
                ulong span = high - low;
                ulong mid = low + (span >> 1) + (span & 1UL); // taşmasız tavan ortası
                Multiply(mid, mid, out ulong sqHi, out ulong sqLo);
                if (Compare(sqHi, sqLo, hi, lo) <= 0)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1UL;
                }
            }

            ulong floor = low;

            // n - floor^2 > floor ise sqrt(n) >= floor + 0.5 → yukarı yuvarla.
            Multiply(floor, floor, out ulong fHi, out ulong fLo);
            ulong diffLo = lo - fLo;
            ulong borrow = lo < fLo ? 1UL : 0UL;
            ulong diffHi = hi - fHi - borrow;
            if (diffHi != 0UL || diffLo > floor)
            {
                return floor + 1UL;
            }

            return floor;
        }
    }
}
