using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>İki taraflı antlaşma; <see cref="A"/> &lt; <see cref="B"/> (kimlik sırası).</summary>
    public sealed class Treaty
    {
        internal Treaty(TreatyId id, TreatyKind kind, CountryId a, CountryId b, GameDate signed, GameDate lastDay)
        {
            Id = id;
            Kind = kind;
            A = a < b ? a : b;
            B = a < b ? b : a;
            Signed = signed;
            LastDay = lastDay;
        }

        public TreatyId Id { get; }

        public TreatyKind Kind { get; }

        public CountryId A { get; }

        public CountryId B { get; }

        public GameDate Signed { get; }

        /// <summary>Antlaşmanın geçerli olduğu son gün (dahil).</summary>
        public GameDate LastDay { get; }

        public bool Binds(CountryId x, CountryId y) => (A == x && B == y) || (A == y && B == x);

        public bool IsActiveOn(GameDate day) => day >= Signed && day <= LastDay;

        public CountryId Other(CountryId country) => A == country ? B : A;
    }
}
