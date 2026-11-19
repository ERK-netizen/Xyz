using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>Sunum için ülke özeti (hazine, istikrar, nüfus, ordu, savaş hâli).</summary>
    public readonly struct CountryView
    {
        public CountryView(CountryId id, Fixed64 treasury, Fixed64 taxRate, Fixed64 stability, long population, int divisions, bool atWar)
        {
            Id = id;
            Treasury = treasury;
            TaxRate = taxRate;
            Stability = stability;
            Population = population;
            Divisions = divisions;
            AtWar = atWar;
        }

        public CountryId Id { get; }

        public Fixed64 Treasury { get; }

        public Fixed64 TaxRate { get; }

        public Fixed64 Stability { get; }

        public long Population { get; }

        public int Divisions { get; }

        public bool AtWar { get; }
    }
}
