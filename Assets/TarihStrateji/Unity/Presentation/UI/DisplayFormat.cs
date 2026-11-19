using System.Text;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Türkçe sayı ve tarih biçimleri (kültür verisine bağlı değil; IL2CPP'de aynı sonuç): binlik
    /// ayraç nokta, yüzde işareti önde ("%45"), tarih "1 Ocak 1925". Yalnız görüntü içindir.
    /// </summary>
    public static class DisplayFormat
    {
        private static readonly string[] MonthKeys =
        {
            "arayuz.ay1", "arayuz.ay2", "arayuz.ay3", "arayuz.ay4", "arayuz.ay5", "arayuz.ay6",
            "arayuz.ay7", "arayuz.ay8", "arayuz.ay9", "arayuz.ay10", "arayuz.ay11", "arayuz.ay12",
        };

        private static readonly StringBuilder Builder = new StringBuilder(32);

        public static string Thousands(long value)
        {
            Builder.Clear();
            ulong magnitude = value < 0 ? (ulong)(-(value + 1)) + 1UL : (ulong)value;
            string digits = magnitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value < 0)
            {
                Builder.Append('-');
            }

            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0)
                {
                    Builder.Append('.');
                }

                Builder.Append(digits[i]);
            }

            return Builder.ToString();
        }

        /// <summary>En yakın tam sayıya yuvarlanmış değer, binlik ayraçla.</summary>
        public static string Whole(Fixed64 value) => Thousands(RoundDiv(value.Raw, Fixed64.Scale));

        /// <summary>Oran (1 = %100) → "%45".</summary>
        public static string Percent(Fixed64 ratio) => "%" + Thousands(RoundDiv(ratio.Raw * 100L, Fixed64.Scale));

        public static string Date(GameDate date, Localizer text) =>
            date.Day.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + text.Get(MonthKeys[date.Month - 1]) + " "
            + date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static long RoundDiv(long value, long divisor)
        {
            long half = divisor / 2L;
            return value >= 0 ? (value + half) / divisor : -((-value + half) / divisor);
        }
    }
}
