using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Time
{
    /// <summary>
    /// Simülasyon takvimi: proleptik Gregoryen, 0001-01-01'den itibaren gün sayısı.
    /// Tamamen tamsayıdır; <c>DateTime</c> kullanılmaz, saat dilimi ve kültür yoktur.
    /// Bir simülasyon adımı (tick) tam bir gündür.
    /// </summary>
    [Serializable]
    public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        public const int MinYear = 1;
        public const int MaxYear = 9999;

        // 0001-01-01 ile 1970-01-01 arasındaki gün sayısı.
        private const int UnixEpochDayNumber = 719162;

        public static readonly GameDate MinValue = new GameDate(0);
        public static readonly GameDate MaxValue = FromYmd(MaxYear, 12, 31);

        private readonly int _dayNumber;

        private GameDate(int dayNumber)
        {
            _dayNumber = dayNumber;
        }

        /// <summary>0001-01-01 == 0.</summary>
        public int DayNumber => _dayNumber;

        public int Year
        {
            get
            {
                ToCivil(_dayNumber, out int y, out _, out _);
                return y;
            }
        }

        public int Month
        {
            get
            {
                ToCivil(_dayNumber, out _, out int m, out _);
                return m;
            }
        }

        public int Day
        {
            get
            {
                ToCivil(_dayNumber, out _, out _, out int d);
                return d;
            }
        }

        /// <summary>0 = Pazartesi … 6 = Pazar (ISO 8601).</summary>
        public int IsoDayOfWeekIndex => _dayNumber % 7; // 0001-01-01 Pazartesi

        public static GameDate FromDayNumber(int dayNumber)
        {
            if (dayNumber < 0 || dayNumber > MaxValue._dayNumber)
            {
                throw new ArgumentOutOfRangeException(nameof(dayNumber), DomainErrorKeys.TarihGecersiz);
            }

            return new GameDate(dayNumber);
        }

        public static GameDate FromYmd(int year, int month, int day)
        {
            if (!IsValid(year, month, day))
            {
                throw new ArgumentOutOfRangeException(nameof(year), DomainErrorKeys.TarihGecersiz);
            }

            return new GameDate(DaysFromCivil(year, month, day));
        }

        public static bool IsValid(int year, int month, int day)
        {
            return year >= MinYear && year <= MaxYear
                && month >= 1 && month <= 12
                && day >= 1 && day <= DaysInMonth(year, month);
        }

        public static bool IsLeapYear(int year)
        {
            return (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
        }

        public static int DaysInMonth(int year, int month)
        {
            switch (month)
            {
                case 2:
                    return IsLeapYear(year) ? 29 : 28;
                case 4:
                case 6:
                case 9:
                case 11:
                    return 30;
                case 1:
                case 3:
                case 5:
                case 7:
                case 8:
                case 10:
                case 12:
                    return 31;
                default:
                    throw new ArgumentOutOfRangeException(nameof(month), DomainErrorKeys.TarihGecersiz);
            }
        }

        public GameDate AddDays(int days)
        {
            return FromDayNumber(checked(_dayNumber + days));
        }

        /// <summary>Bu tarihten <paramref name="other"/> tarihine kadar geçen gün (negatif olabilir).</summary>
        public int DaysUntil(GameDate other)
        {
            return other._dayNumber - _dayNumber;
        }

        /// <summary>[start, endInclusive] tarih penceresi içinde mi (karakter/teknoloji pencereleri).</summary>
        public bool IsWithin(GameDate start, GameDate endInclusive)
        {
            return _dayNumber >= start._dayNumber && _dayNumber <= endInclusive._dayNumber;
        }

        /// <summary>Katı "yyyy-MM-dd" biçimi.</summary>
        public static bool TryParse(string text, out GameDate date)
        {
            date = MinValue;
            if (text == null || text.Length != 10 || text[4] != '-' || text[7] != '-')
            {
                return false;
            }

            if (!TryDigits(text, 0, 4, out int y) || !TryDigits(text, 5, 2, out int m) || !TryDigits(text, 8, 2, out int d))
            {
                return false;
            }

            if (!IsValid(y, m, d))
            {
                return false;
            }

            date = new GameDate(DaysFromCivil(y, m, d));
            return true;
        }

        public static GameDate Parse(string text)
        {
            if (!TryParse(text, out GameDate date))
            {
                throw new FormatException(DomainErrorKeys.TarihGecersiz);
            }

            return date;
        }

        public override string ToString()
        {
            ToCivil(_dayNumber, out int y, out int m, out int d);
            return y.ToString("D4", CultureInfo.InvariantCulture) + "-"
                + m.ToString("D2", CultureInfo.InvariantCulture) + "-"
                + d.ToString("D2", CultureInfo.InvariantCulture);
        }

        public static bool operator ==(GameDate a, GameDate b) => a._dayNumber == b._dayNumber;

        public static bool operator !=(GameDate a, GameDate b) => a._dayNumber != b._dayNumber;

        public static bool operator <(GameDate a, GameDate b) => a._dayNumber < b._dayNumber;

        public static bool operator >(GameDate a, GameDate b) => a._dayNumber > b._dayNumber;

        public static bool operator <=(GameDate a, GameDate b) => a._dayNumber <= b._dayNumber;

        public static bool operator >=(GameDate a, GameDate b) => a._dayNumber >= b._dayNumber;

        public bool Equals(GameDate other) => _dayNumber == other._dayNumber;

        public override bool Equals(object obj) => obj is GameDate other && _dayNumber == other._dayNumber;

        public override int GetHashCode() => _dayNumber;

        public int CompareTo(GameDate other) => _dayNumber.CompareTo(other._dayNumber);

        // Howard Hinnant'ın "days_from_civil" / "civil_from_days" tamsayı algoritmaları.
        private static int DaysFromCivil(int year, int month, int day)
        {
            int y = month <= 2 ? year - 1 : year;
            int era = (y >= 0 ? y : y - 399) / 400;
            int yoe = y - (era * 400);
            int mp = month > 2 ? month - 3 : month + 9;
            int doy = (((153 * mp) + 2) / 5) + day - 1;
            int doe = (yoe * 365) + (yoe / 4) - (yoe / 100) + doy;
            int daysSinceUnixEpoch = (era * 146097) + doe - 719468;
            return daysSinceUnixEpoch + UnixEpochDayNumber;
        }

        private static void ToCivil(int dayNumber, out int year, out int month, out int day)
        {
            int z = dayNumber - UnixEpochDayNumber + 719468;
            int era = (z >= 0 ? z : z - 146096) / 146097;
            int doe = z - (era * 146097);
            int yoe = (doe - (doe / 1460) + (doe / 36524) - (doe / 146096)) / 365;
            int y = yoe + (era * 400);
            int doy = doe - ((365 * yoe) + (yoe / 4) - (yoe / 100));
            int mp = ((5 * doy) + 2) / 153;
            day = doy - (((153 * mp) + 2) / 5) + 1;
            month = mp < 10 ? mp + 3 : mp - 9;
            year = month <= 2 ? y + 1 : y;
        }

        private static bool TryDigits(string text, int start, int length, out int value)
        {
            value = 0;
            for (int i = start; i < start + length; i++)
            {
                char c = text[i];
                if (c < '0' || c > '9')
                {
                    return false;
                }

                value = (value * 10) + (c - '0');
            }

            return true;
        }
    }
}
