namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Harita ayrıntı düzeyi politikası (yalnız sunum, ADR-011). Kamera ortografik yarı boyuna (dünya birimi
    /// = 1° enlem) göre 4 düzey: 0 en yakın (tam çizgi, tüm şehir adları), 3 en uzak (en sade sınır, yalnız
    /// büyük şehir adları). Eşiklerde %15 histerezis: yakınlaştırma sınırında titreme olmaz.
    /// </summary>
    public static class MapLod
    {
        public const int Levels = 4;

        /// <summary>Düzey sınırları (yarı boy): bunun üstü bir sonraki düzeydir.</summary>
        private static readonly float[] Thresholds = { 2.5f, 6f, 14f };

        /// <summary>Düzey başına sınır sadeleştirme toleransı (derece).</summary>
        private static readonly float[] Tolerances = { 0f, 0.02f, 0.06f, 0.15f };

        /// <summary>Düzey başına etiketi gösterilecek en küçük gelişim seviyesi (1–10).</summary>
        private static readonly int[] LabelMinDevelopment = { 1, 3, 4, 6 };

        public static int LevelFor(float orthographicSize, int current)
        {
            int level = 0;
            while (level < Thresholds.Length && orthographicSize > Thresholds[level])
            {
                level++;
            }

            if (current < 0 || current >= Levels || level == current)
            {
                return level;
            }

            // Histerezis: mevcut düzeyin sınırından %15 taşmadıkça düzey korunur.
            if (level > current)
            {
                return orthographicSize > Thresholds[current] * 1.15f ? level : current;
            }

            return orthographicSize < Thresholds[current - 1] * 0.85f ? level : current;
        }

        public static float SimplifyTolerance(int level) => Tolerances[Clamp(level)];

        public static int MinLabelDevelopment(int level) => LabelMinDevelopment[Clamp(level)];

        private static int Clamp(int level) => level < 0 ? 0 : level >= Levels ? Levels - 1 : level;
    }
}
