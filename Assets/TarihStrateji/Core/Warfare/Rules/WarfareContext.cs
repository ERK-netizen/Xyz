using System;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>Savaş kurallarının okuduğu ve yazdığı durumların demeti (Simulation kurar).</summary>
    public sealed class WarfareContext
    {
        public WarfareContext(
            WorldMap world,
            WarfareState warfare,
            WarfareCatalog catalog,
            PopulationState population,
            EconomyState economy,
            EffectEngine effects,
            IHostility hostility,
            GameDate today)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Warfare = warfare ?? throw new ArgumentNullException(nameof(warfare));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Population = population ?? throw new ArgumentNullException(nameof(population));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Hostility = hostility ?? throw new ArgumentNullException(nameof(hostility));
            Today = today;
        }

        public WorldMap World { get; }

        public WarfareState Warfare { get; }

        public WarfareCatalog Catalog { get; }

        public PopulationState Population { get; }

        public EconomyState Economy { get; }

        public EffectEngine Effects { get; }

        public IHostility Hostility { get; }

        public GameDate Today { get; }
    }
}
