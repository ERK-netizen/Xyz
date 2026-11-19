using System;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>Coğrafi savaş kurallarının okuduğu ve yazdığı durumların demeti (Simulation kurar).</summary>
    public sealed class GeoWarfareContext
    {
        public GeoWarfareContext(
            GeoWorld world,
            WarfareState warfare,
            WarfareCatalog catalog,
            GeoWarfareParameters geo,
            PopulationState population,
            EconomyState economy,
            EffectEngine effects,
            IHostility hostility,
            GameDate today)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Warfare = warfare ?? throw new ArgumentNullException(nameof(warfare));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Geo = geo ?? throw new ArgumentNullException(nameof(geo));
            Population = population ?? throw new ArgumentNullException(nameof(population));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Hostility = hostility ?? throw new ArgumentNullException(nameof(hostility));
            Today = today;
        }

        public GeoWorld World { get; }

        public WarfareState Warfare { get; }

        public WarfareCatalog Catalog { get; }

        public GeoWarfareParameters Geo { get; }

        public PopulationState Population { get; }

        public EconomyState Economy { get; }

        public EffectEngine Effects { get; }

        public IHostility Hostility { get; }

        public GameDate Today { get; }

        public bool IsHostile(Domain.Ids.CountryId a, Domain.Ids.CountryId b) => a.IsValid && b.IsValid && a != b && Hostility.AtWar(a, b);
    }
}
