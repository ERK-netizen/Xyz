using TarihStrateji.Core.Domain.Ids;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Harita renkleri: bölge fiilî kontrolcünün (controller) rengini alır; işgal edilmiş bölgede
    /// (sahip ≠ kontrolcü) hukukî sahibin rengi karışım olarak görünür ve ton koyulaşır. Ülke sınırı koyu,
    /// kıyı açık çizgidir; deniz arka plan rengidir.
    /// </summary>
    public static class MapPalette
    {
        public static readonly Color32 Background = new Color32(28, 58, 92, 255);
        public static readonly Color32 Border = new Color32(20, 20, 24, 255);
        public static readonly Color32 Coast = new Color32(190, 210, 225, 255);
        public static readonly Color32 Unclaimed = new Color32(150, 150, 140, 255);
        public static readonly Color32 Highlight = new Color32(255, 214, 64, 255);
        public static readonly Color32 Outline = new Color32(250, 250, 245, 255);
        public static readonly Color32 EnemyOutline = new Color32(200, 40, 40, 255);
        public static readonly Color32 EstimateOutline = new Color32(150, 150, 150, 255);

        /// <summary>Ülke kimliğine göre renk (kimlik − 1); senaryolar kimlikleri paylaşır: 1914 / 1939 adları yorumda.</summary>
        private static readonly Color32[] CountryColors =
        {
            new Color32(200, 48, 48, 255),   // 1 Osmanlı / Türkiye
            new Color32(46, 104, 196, 255),  // 2 Yunanistan
            new Color32(54, 150, 84, 255),   // 3 Bulgaristan
            new Color32(150, 72, 168, 255),  // 4 Sırbistan / Yugoslavya
            new Color32(214, 140, 40, 255),  // 5 Karadağ / Mısır
            new Color32(40, 160, 168, 255),  // 6 Arnavutluk / Irak
            new Color32(150, 110, 60, 255),  // 7 Romanya
            new Color32(120, 120, 130, 255), // 8 Avusturya-Macaristan / Alman Reich
            new Color32(200, 180, 70, 255),  // 9 İtalya
            new Color32(80, 80, 190, 255),   // 10 Rus İmparatorluğu / Sovyetler
            new Color32(170, 170, 160, 255), // 11 Birleşik Krallık
            new Color32(120, 170, 60, 255),  // 12 İran
            new Color32(110, 150, 220, 255), // 13 — / Fransa (Levant)
            new Color32(210, 110, 150, 255), // 14 — / Macaristan
            new Color32(90, 190, 140, 255),  // 15 yedek
            new Color32(180, 120, 210, 255), // 16 yedek
        };


        public static Color32 Country(CountryId country)
        {
            if (!country.IsValid)
            {
                return new Color32(128, 128, 128, 255);
            }

            return CountryColors[(int)((country.Value - 1UL) % (ulong)CountryColors.Length)];
        }

        /// <summary>Bölge dolgusu: kontrolcü rengi; işgalde sahibin rengiyle karışık ve koyu.</summary>
        public static Color32 Region(CountryId owner, CountryId controller)
        {
            if (!controller.IsValid)
            {
                return Unclaimed;
            }

            Color32 color = Country(controller);
            if (owner.IsValid && owner != controller)
            {
                color = Scale(Color32.Lerp(color, Country(owner), 0.35f), 0.8f);
            }

            return color;
        }

        public static Color32 Scale(Color32 color, float factor) =>
            new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
                color.a);
    }
}
