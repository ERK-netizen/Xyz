namespace TarihStrateji.Core.Population
{
    /// <summary>
    /// Yaş bantları: 17 adet 5 yıllık bant (0–4, 5–9, …, 75–79, 80+). Çalışma çağı 15–64
    /// (bant 3–12), doğurganlık çağı 15–49 (bant 3–9).
    /// </summary>
    public static class AgeBands
    {
        public const int Count = 17;
        public const int YearsPerBand = 5;
        public const int WorkingFirst = 3;
        public const int WorkingLast = 12;
        public const int FertileFirst = 3;
        public const int FertileLast = 9;
        public const int Last = Count - 1;

        public static bool IsWorkingAge(int band) => band >= WorkingFirst && band <= WorkingLast;
    }
}
