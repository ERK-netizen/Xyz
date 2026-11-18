using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Numerics
{
    /// <summary>
    /// Oyun kararlarında kullanılan tek sayısal tip: ondalık sabit noktalı, ölçek
    /// <see cref="Scale"/> = 1_000_000 (6 ondalık basamak). Ham değer <see cref="Raw"/> bir
    /// <c>long</c>'dur.
    /// <para>Kurallar (CLAUDE.md §2.4):</para>
    /// <list type="bullet">
    /// <item>Çarpma, bölme ve karekök 128-bit ara sonuçla hesaplanır.</item>
    /// <item>Yuvarlama en yakına, tam yarıda sıfırdan uzağa yapılır.</item>
    /// <item>Taşma her zaman <see cref="OverflowException"/> fırlatır.</item>
    /// <item>Kayan noktalı tiplerle dönüşüm bu derlemede yoktur; sunum katmanı yapar.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public readonly struct Fixed64 : IEquatable<Fixed64>, IComparable<Fixed64>, IComparable
    {
        /// <summary>Tek ölçek: 1.0 == 1_000_000 ham birim.</summary>
        public const long Scale = 1_000_000L;

        /// <summary>Ondalık basamak sayısı.</summary>
        public const int DecimalDigits = 6;

        private const ulong ScaleU = 1_000_000UL;
        private const ulong NegativeLimit = 0x8000000000000000UL; // |long.MinValue|

        public static readonly Fixed64 Zero = new Fixed64(0L);
        public static readonly Fixed64 One = new Fixed64(Scale);
        public static readonly Fixed64 Half = new Fixed64(Scale / 2L);
        public static readonly Fixed64 Hundred = new Fixed64(100L * Scale);
        public static readonly Fixed64 MinusOne = new Fixed64(-Scale);
        public static readonly Fixed64 Epsilon = new Fixed64(1L);
        public static readonly Fixed64 MaxValue = new Fixed64(long.MaxValue);
        public static readonly Fixed64 MinValue = new Fixed64(long.MinValue);

        /// <summary>Ham (ölçeklenmiş) değer.</summary>
        public readonly long Raw;

        private Fixed64(long raw)
        {
            Raw = raw;
        }

        // ------------------------------------------------------------------ oluşturma

        /// <summary>Ham değerden oluşturur (kayıt dosyası ve tablolar için).</summary>
        public static Fixed64 FromRaw(long raw)
        {
            return new Fixed64(raw);
        }

        /// <summary>Tamsayıdan oluşturur; ölçek taşarsa hata.</summary>
        public static Fixed64 FromInt(long value)
        {
            return new Fixed64(checked(value * Scale));
        }

        /// <summary>numerator / denominator oranını en yakına yuvarlayarak oluşturur.</summary>
        public static Fixed64 FromRatio(long numerator, long denominator)
        {
            if (denominator == 0L)
            {
                throw new DivideByZeroException("Fixed64.FromRatio: payda sıfır.");
            }

            bool negative = (numerator < 0L) != (denominator < 0L);
            UInt128Math.Multiply(Magnitude(numerator), ScaleU, out ulong hi, out ulong lo);
            ulong quotient = DivideRounded(hi, lo, Magnitude(denominator));
            return new Fixed64(ToSigned(quotient, negative));
        }

        /// <summary>Yüzde değerinden oluşturur: FromPercent(25) == 0.25.</summary>
        public static Fixed64 FromPercent(long percent)
        {
            return FromRatio(percent, 100L);
        }

        // ------------------------------------------------------------------ özellikler

        public bool IsZero => Raw == 0L;

        public bool IsNegative => Raw < 0L;

        public bool IsPositive => Raw > 0L;

        public int Sign => Raw > 0L ? 1 : (Raw < 0L ? -1 : 0);

        // ------------------------------------------------------------------ aritmetik

        public static Fixed64 operator +(Fixed64 a, Fixed64 b)
        {
            return new Fixed64(checked(a.Raw + b.Raw));
        }

        public static Fixed64 operator -(Fixed64 a, Fixed64 b)
        {
            return new Fixed64(checked(a.Raw - b.Raw));
        }

        public static Fixed64 operator -(Fixed64 value)
        {
            return new Fixed64(checked(-value.Raw));
        }

        public static Fixed64 operator +(Fixed64 value)
        {
            return value;
        }

        public static Fixed64 operator *(Fixed64 a, Fixed64 b)
        {
            bool negative = (a.Raw < 0L) != (b.Raw < 0L);
            UInt128Math.Multiply(Magnitude(a.Raw), Magnitude(b.Raw), out ulong hi, out ulong lo);
            ulong quotient = DivideRounded(hi, lo, ScaleU);
            return new Fixed64(ToSigned(quotient, negative));
        }

        public static Fixed64 operator /(Fixed64 a, Fixed64 b)
        {
            if (b.Raw == 0L)
            {
                throw new DivideByZeroException("Fixed64: sıfıra bölme.");
            }

            bool negative = (a.Raw < 0L) != (b.Raw < 0L);
            UInt128Math.Multiply(Magnitude(a.Raw), ScaleU, out ulong hi, out ulong lo);
            ulong quotient = DivideRounded(hi, lo, Magnitude(b.Raw));
            return new Fixed64(ToSigned(quotient, negative));
        }

        /// <summary>Kalan: işareti bölünenin işaretini izler (C# tamsayı semantiği).</summary>
        public static Fixed64 operator %(Fixed64 a, Fixed64 b)
        {
            if (b.Raw == 0L)
            {
                throw new DivideByZeroException("Fixed64: sıfıra göre kalan.");
            }

            if (b.Raw == -1L)
            {
                return Zero; // long.MinValue % -1 çalışma zamanında taşma fırlatabilir
            }

            return new Fixed64(a.Raw % b.Raw);
        }

        /// <summary>Tamsayı ile çarpım (ölçeksiz, kesin).</summary>
        public static Fixed64 operator *(Fixed64 a, long b)
        {
            return new Fixed64(checked(a.Raw * b));
        }

        /// <summary>Tamsayı ile çarpım (ölçeksiz, kesin).</summary>
        public static Fixed64 operator *(long a, Fixed64 b)
        {
            return new Fixed64(checked(a * b.Raw));
        }

        /// <summary>Tamsayıya bölüm, en yakına yuvarlanır.</summary>
        public static Fixed64 operator /(Fixed64 a, long b)
        {
            if (b == 0L)
            {
                throw new DivideByZeroException("Fixed64: sıfıra bölme.");
            }

            bool negative = (a.Raw < 0L) != (b < 0L);
            ulong quotient = DivideRounded(0UL, Magnitude(a.Raw), Magnitude(b));
            return new Fixed64(ToSigned(quotient, negative));
        }

        /// <summary>
        /// (a * b) / c tek yuvarlamayla. Ara çarpım 128 bittir; oran ve pay hesaplarında
        /// iki ayrı yuvarlamanın biriktirdiği hatayı önler.
        /// </summary>
        public static Fixed64 MulDiv(Fixed64 a, Fixed64 b, Fixed64 c)
        {
            if (c.Raw == 0L)
            {
                throw new DivideByZeroException("Fixed64.MulDiv: sıfıra bölme.");
            }

            bool negative = ((a.Raw < 0L) != (b.Raw < 0L)) != (c.Raw < 0L);
            UInt128Math.Multiply(Magnitude(a.Raw), Magnitude(b.Raw), out ulong hi, out ulong lo);
            ulong quotient = DivideRounded(hi, lo, Magnitude(c.Raw));
            return new Fixed64(ToSigned(quotient, negative));
        }

        // ------------------------------------------------------------------ yardımcılar

        public static Fixed64 Abs(Fixed64 value)
        {
            return value.Raw < 0L ? -value : value;
        }

        public static Fixed64 Min(Fixed64 a, Fixed64 b)
        {
            return a.Raw <= b.Raw ? a : b;
        }

        public static Fixed64 Max(Fixed64 a, Fixed64 b)
        {
            return a.Raw >= b.Raw ? a : b;
        }

        public static Fixed64 Clamp(Fixed64 value, Fixed64 min, Fixed64 max)
        {
            if (min.Raw > max.Raw)
            {
                throw new ArgumentException("Fixed64.Clamp: min > max.");
            }

            if (value.Raw < min.Raw)
            {
                return min;
            }

            return value.Raw > max.Raw ? max : value;
        }

        /// <summary>[0, 1] aralığına sıkıştırır.</summary>
        public static Fixed64 Clamp01(Fixed64 value)
        {
            return Clamp(value, Zero, One);
        }

        /// <summary>a + (b - a) * t; t sıkıştırılmaz.</summary>
        public static Fixed64 Lerp(Fixed64 a, Fixed64 b, Fixed64 t)
        {
            return a + ((b - a) * t);
        }

        /// <summary>Aşağı doğru en yakın tam sayı (negatif sonsuza).</summary>
        public static Fixed64 Floor(Fixed64 value)
        {
            long remainder = value.Raw % Scale;
            long raw = value.Raw - remainder;
            if (remainder < 0L)
            {
                raw = checked(raw - Scale);
            }

            return new Fixed64(raw);
        }

        /// <summary>Yukarı doğru en yakın tam sayı (pozitif sonsuza).</summary>
        public static Fixed64 Ceiling(Fixed64 value)
        {
            long remainder = value.Raw % Scale;
            long raw = value.Raw - remainder;
            if (remainder > 0L)
            {
                raw = checked(raw + Scale);
            }

            return new Fixed64(raw);
        }

        /// <summary>En yakın tam sayıya, yarıda sıfırdan uzağa.</summary>
        public static Fixed64 Round(Fixed64 value)
        {
            return FromInt(value.RoundToLong());
        }

        /// <summary>Sıfıra doğru keserek tam sayıya çevirir.</summary>
        public long TruncateToLong()
        {
            return Raw / Scale;
        }

        /// <summary>Negatif sonsuza doğru tam sayıya çevirir.</summary>
        public long FloorToLong()
        {
            long quotient = Raw / Scale;
            return (Raw % Scale) < 0L ? quotient - 1L : quotient;
        }

        /// <summary>En yakın tam sayıya, yarıda sıfırdan uzağa çevirir.</summary>
        public long RoundToLong()
        {
            ulong magnitude = Magnitude(Raw);
            ulong quotient = magnitude / ScaleU;
            ulong remainder = magnitude % ScaleU;
            if (remainder * 2UL >= ScaleU)
            {
                quotient++;
            }

            long result = (long)quotient; // |long.MinValue| / Scale sığar
            return Raw < 0L ? -result : result;
        }

        /// <summary>Karekök, en yakına yuvarlanmış. Negatif girdi hata verir.</summary>
        public static Fixed64 Sqrt(Fixed64 value)
        {
            if (value.Raw < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Fixed64.Sqrt: negatif değer.");
            }

            // sqrt(raw / S) * S == sqrt(raw * S)
            UInt128Math.Multiply((ulong)value.Raw, ScaleU, out ulong hi, out ulong lo);
            ulong root = UInt128Math.SqrtRounded(hi, lo);
            return new Fixed64((long)root); // sqrt(2^63 * 10^6) < 2^63
        }

        /// <summary>Negatif olmayan tam sayı üssü; kare-çarp ile, her çarpımda yuvarlanır.</summary>
        public static Fixed64 Pow(Fixed64 value, int exponent)
        {
            if (exponent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exponent), "Fixed64.Pow: negatif üs.");
            }

            Fixed64 result = One;
            Fixed64 baseValue = value;
            int e = exponent;
            while (e > 0)
            {
                if ((e & 1) != 0)
                {
                    result *= baseValue;
                }

                e >>= 1;
                if (e > 0)
                {
                    baseValue *= baseValue;
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ metin

        /// <summary>
        /// Kültürden bağımsız ayrıştırma: isteğe bağlı '-' veya '+', rakamlar, isteğe bağlı '.'
        /// ve ondalıklar. 6'dan fazla ondalık en yakına (yarıda sıfırdan uzağa) yuvarlanır.
        /// </summary>
        public static Fixed64 Parse(string text)
        {
            if (!TryParse(text, out Fixed64 value, out string errorKey))
            {
                throw new FormatException(errorKey);
            }

            return value;
        }

        public static bool TryParse(string text, out Fixed64 value)
        {
            return TryParse(text, out value, out _);
        }

        private static bool TryParse(string text, out Fixed64 value, out string errorKey)
        {
            value = Zero;
            errorKey = DomainErrorKeys.SayiBicimiGecersiz;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int index = 0;
            bool negative = false;
            if (text[0] == '-' || text[0] == '+')
            {
                negative = text[0] == '-';
                index = 1;
            }

            ulong integerPart = 0UL;
            ulong fractionPart = 0UL;
            int fractionDigits = 0;
            bool roundUp = false;
            bool anyDigit = false;
            bool seenPoint = false;

            for (; index < text.Length; index++)
            {
                char c = text[index];
                if (c == '.')
                {
                    if (seenPoint)
                    {
                        return false;
                    }

                    seenPoint = true;
                    continue;
                }

                if (c < '0' || c > '9')
                {
                    return false;
                }

                anyDigit = true;
                ulong digit = (ulong)(c - '0');
                if (!seenPoint)
                {
                    if (integerPart > (ulong.MaxValue - digit) / 10UL)
                    {
                        errorKey = DomainErrorKeys.SayiTasmasi;
                        return false;
                    }

                    integerPart = (integerPart * 10UL) + digit;
                }
                else if (fractionDigits < DecimalDigits)
                {
                    fractionPart = (fractionPart * 10UL) + digit;
                    fractionDigits++;
                }
                else if (fractionDigits == DecimalDigits)
                {
                    roundUp = digit >= 5UL; // yalnız ilk atılan basamak yuvarlamayı belirler
                    fractionDigits++;
                }
            }

            if (!anyDigit)
            {
                return false;
            }

            int pad = System.Math.Min(fractionDigits, DecimalDigits);
            for (; pad < DecimalDigits; pad++)
            {
                fractionPart *= 10UL;
            }

            ulong limit = negative ? NegativeLimit : (ulong)long.MaxValue;
            if (integerPart > limit / ScaleU)
            {
                errorKey = DomainErrorKeys.SayiTasmasi;
                return false;
            }

            ulong magnitude = (integerPart * ScaleU) + fractionPart + (roundUp ? 1UL : 0UL);
            if (magnitude > limit)
            {
                errorKey = DomainErrorKeys.SayiTasmasi;
                return false;
            }

            value = new Fixed64(ToSigned(magnitude, negative));
            errorKey = null;
            return true;
        }

        /// <summary>Kültürden bağımsız, sondaki sıfırları atılmış gösterim: "-12.5", "3".</summary>
        public override string ToString()
        {
            ulong magnitude = Magnitude(Raw);
            ulong integerPart = magnitude / ScaleU;
            ulong fractionPart = magnitude % ScaleU;
            string sign = Raw < 0L ? "-" : string.Empty;
            string integerText = integerPart.ToString(CultureInfo.InvariantCulture);
            if (fractionPart == 0UL)
            {
                return sign + integerText;
            }

            string fractionText = fractionPart.ToString("D6", CultureInfo.InvariantCulture).TrimEnd('0');
            return sign + integerText + "." + fractionText;
        }

        // ------------------------------------------------------------------ karşılaştırma

        public static bool operator ==(Fixed64 a, Fixed64 b) => a.Raw == b.Raw;

        public static bool operator !=(Fixed64 a, Fixed64 b) => a.Raw != b.Raw;

        public static bool operator <(Fixed64 a, Fixed64 b) => a.Raw < b.Raw;

        public static bool operator >(Fixed64 a, Fixed64 b) => a.Raw > b.Raw;

        public static bool operator <=(Fixed64 a, Fixed64 b) => a.Raw <= b.Raw;

        public static bool operator >=(Fixed64 a, Fixed64 b) => a.Raw >= b.Raw;

        public bool Equals(Fixed64 other) => Raw == other.Raw;

        public override bool Equals(object obj) => obj is Fixed64 other && Raw == other.Raw;

        public override int GetHashCode() => Raw.GetHashCode();

        public int CompareTo(Fixed64 other) => Raw.CompareTo(other.Raw);

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (obj is Fixed64 other)
            {
                return Raw.CompareTo(other.Raw);
            }

            throw new ArgumentException("Fixed64 ile karşılaştırılamayan tip.", nameof(obj));
        }

        // ------------------------------------------------------------------ iç yardımcılar

        private static ulong Magnitude(long value)
        {
            return value < 0L ? (ulong)(-(value + 1L)) + 1UL : (ulong)value;
        }

        private static long ToSigned(ulong magnitude, bool negative)
        {
            if (negative)
            {
                if (magnitude > NegativeLimit)
                {
                    throw new OverflowException("Fixed64: negatif taşma.");
                }

                return magnitude == NegativeLimit ? long.MinValue : -(long)magnitude;
            }

            if (magnitude > (ulong)long.MaxValue)
            {
                throw new OverflowException("Fixed64: pozitif taşma.");
            }

            return (long)magnitude;
        }

        /// <summary>(hi:lo) / divisor, en yakına (yarıda büyüklüğü artırarak) yuvarlanır.</summary>
        private static ulong DivideRounded(ulong hi, ulong lo, ulong divisor)
        {
            ulong quotient = UInt128Math.Divide(hi, lo, divisor, out ulong remainder);

            // remainder < divisor <= 2^63 olduğundan iki katı ulong'a sığar.
            bool roundUp = divisor > NegativeLimit
                ? remainder >= divisor - remainder
                : remainder * 2UL >= divisor;
            if (roundUp)
            {
                if (quotient == ulong.MaxValue)
                {
                    throw new OverflowException("Fixed64: yuvarlama taşması.");
                }

                quotient++;
            }

            return quotient;
        }
    }
}
