using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems.Geo;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Scenario
{
    /// <summary>
    /// Savaş hâlindeki "Ege 1925" (Türkiye = A saldırgan, Yunanistan = B): diplomasi, istihbarat ve YZ
    /// testlerinin ortak kurulumu. Kayıttan yükleme için şablon dünya <see cref="AegeanScenario.BuildWorld"/>.
    /// </summary>
    public sealed class AegeanWar
    {
        public static readonly GameContent Content = BaselineContent.Create();
        public static readonly CountryId Turkey = BaselineStatecraft.Turkey;
        public static readonly CountryId Greece = BaselineStatecraft.Greece;

        /// <summary>Selanik bölgesi (Yunan) ve Drama kavşağı.</summary>
        public static readonly RegionId SelanikRegion = new RegionId(11);
        public static readonly TransportNodeId Drama = new TransportNodeId(114);

        public AegeanWar(ulong seed = 1925, GameMode mode = GameMode.Normal, bool atWar = true)
        {
            State = ScenarioFactory.CreateAegean(Content, seed, mode, mode == GameMode.Observer ? CountryId.None : Turkey);
            if (atWar)
            {
                DiplomacyRules.DeclareWar(State.Diplomacy, Turkey, Greece, new RegionId[0], State.Today);
            }
        }

        public GameState State { get; }

        public GeoWarfareContext War => GeoWarfareContexts.From(State, Content);

        public static SimulationSetup Setup(bool ai = false)
        {
            SimulationSetup setup = StandardRules.Install(new SimulationSetup { HashIntervalDays = 1, GeoWorldTemplate = AegeanScenario.BuildWorld,
                Capitals = CapitalTable.FromCityKeys(AegeanScenario.BuildWorld(), AegeanScenario.CapitalCityKeys).Of }, Content);
            return ai ? StandardRules.InstallArtificialIntelligence(setup, Content) : setup;
        }

        public CityId City(string key) => ScenarioFactory.CityOf(State, key);

        /// <summary>Şehirde birlik toplar, düğüme koyar, tam teşkilat ve ikmal verir (yalnız test kurulumu).</summary>
        public LandUnit Place(CountryId country, string recruitCity, TransportNodeId node)
        {
            State.Economy.SeedStock(country, BaselineGoods.InfantryEquipment, Fixed64.FromInt(200));
            var id = GeoUnitRules.Recruit(War, country, BaselineUnits.InfantryDivision, City(recruitCity));
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            LandUnit unit = State.Warfare.Units[id.Value];
            unit.Node = node;
            unit.Organization = Content.Warfare.UnitTypes[unit.Type].MaxOrganization;
            unit.Supply = Fixed64.One;
            return unit;
        }

        public void SetController(RegionId region, CountryId controller)
        {
            State.World.TryGetControl(region, out RegionControl control);
            control.Controller = controller;
            control.Stage = controller == control.Owner ? RegionControlStage.Secured : RegionControlStage.Occupied;
        }

        public CountryId ControllerOf(RegionId region)
        {
            State.World.TryGetControl(region, out RegionControl control);
            return control.Controller;
        }
    }
}
