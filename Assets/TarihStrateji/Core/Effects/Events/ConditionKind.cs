namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>
    /// Olay/odak koşulu türü. Değerlendirme Simulation'dadır (<see cref="ICountryConditions"/>).
    /// Değerler içerik verisine yazılır; asla yeniden numaralanmaz.
    /// </summary>
    public enum ConditionKind : byte
    {
        /// <summary>Tarih ≥ <c>Id</c> (gün numarası).</summary>
        DateOnOrAfter = 1,
        AtWar = 2,
        NotAtWar = 3,

        /// <summary>İstikrar &lt; <c>Value</c>.</summary>
        StabilityBelow = 4,

        /// <summary>İstikrar ≥ <c>Value</c>.</summary>
        StabilityAtLeast = 5,

        /// <summary><c>Id</c> teknolojisi araştırılmış.</summary>
        HasTechnology = 6,

        /// <summary><c>Id</c> odağı tamamlanmış.</summary>
        FocusCompleted = 7,

        /// <summary>Hazine ≥ <c>Value</c>.</summary>
        TreasuryAtLeast = 8,
    }
}
