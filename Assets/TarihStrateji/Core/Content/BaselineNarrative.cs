using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Temel olay ve odak kimlikleri (asla yeniden kullanılmaz).</summary>
    public static class BaselineNarrative
    {
        public static readonly EventId GreatDepression = new EventId(1);
        public static readonly EventId WarWeariness = new EventId(2);
        public static readonly EventId IndustrialBoom = new EventId(3);

        public static readonly FocusId Industrialization = new FocusId(1);
        public static readonly FocusId HeavyIndustry = new FocusId(2);
        public static readonly FocusId ArmyModernization = new FocusId(3);
        public static readonly FocusId NavalBuildup = new FocusId(4);
        public static readonly FocusId Neutrality = new FocusId(5);
    }
}
