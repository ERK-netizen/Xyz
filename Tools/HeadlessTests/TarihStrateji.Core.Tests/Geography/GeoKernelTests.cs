using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Commands.Geo;
using TarihStrateji.Core.Simulation.Persistence;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>
    /// Adım 13/1: coğrafi oyun durumu çekirdekte uçtan uca: emir kapısı → P1 → P11–P14 coğrafi sistemler,
    /// aynı tohum + emirler → aynı hash (REG-012), kaydet/yükle sürekliliği, şablon uyuşmazlığında ret (REG-029).
    /// </summary>
    [TestFixture]
    public class GeoKernelTests
    {
        private static readonly GameContent Content = BaselineContent.Create();

        [Test]
        public void Commands_FlowThroughKernel_UnitsMarchAndFight()
        {
            SimulationKernel kernel = NewKernel(1914UL, out _);
            CommandReceipt recruit = kernel.Submit(new GeoRecruitUnitCommand(Red, BaselineUnits.InfantryDivision, GeoWarFixture.CityA1));
            Assert.That(recruit.Accepted, Is.True, recruit.Validation.ErrorKey);
            Assert.That(kernel.Submit(new GeoRecruitUnitCommand(Red, BaselineUnits.InfantryDivision, GeoWarFixture.CityB1)).Accepted, Is.False, "düşman şehri");
            Assert.That(kernel.Submit(new GeoRecruitUnitCommand(Blue, BaselineUnits.InfantryDivision, GeoWarFixture.CityB1)).Accepted, Is.False, "oyuncu dışı ülke");
            Advance(kernel, 1);

            LandUnit unit = FirstUnit(kernel.State, Red);
            Assert.That(unit.Node, Is.EqualTo(A1));
            Assert.That(kernel.Submit(new GeoMoveUnitCommand(Red, unit.Id, TargetRef.AtPoint(P("40.5", "28.9")))).Accepted, Is.True);
            Advance(kernel, 3);
            Assert.That(unit.Node, Is.EqualTo(A2), "30 km/gün yolda 40 km → 2 gün");

            CommandReceipt attack = kernel.Submit(new GeoOrderAttackCommand(Red, unit.Id, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value)));
            Assert.That(attack.Accepted, Is.True, attack.Validation.ErrorKey);
            DayReport day = Advance(kernel, 2);
            Assert.That(kernel.IsHalted, Is.False, string.Join("\n", kernel.HaltReasons));
            Assert.That(day, Is.Not.Null);
        }

        [Test]
        public void SameSeedAndCommands_GiveSameHash_AndSaveLoadContinues()
        {
            byte[] Run(out SimulationKernel kernel, out SimulationSetup setup)
            {
                kernel = NewKernel(7UL, out setup);
                kernel.Submit(new GeoRecruitUnitCommand(Red, BaselineUnits.InfantryDivision, GeoWarFixture.CityA1));
                Advance(kernel, 1);
                LandUnit unit = FirstUnit(kernel.State, Red);
                kernel.Submit(new GeoMoveUnitCommand(Red, unit.Id, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityAP.Value)));
                Advance(kernel, 2);
                return kernel.ComputeStateHash();
            }

            byte[] first = Run(out SimulationKernel a, out SimulationSetup setupA);
            byte[] second = Run(out SimulationKernel b, out _);
            Assert.That(second, Is.EqualTo(first), "REG-012");

            Result<SimulationKernel> restored = SimulationKernel.Restore(setupA, a.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(restored.Value.ComputeStateHash(), Is.EqualTo(first));
            Advance(a, 3);
            Advance(restored.Value, 3);
            Assert.That(restored.Value.ComputeStateHash(), Is.EqualTo(a.ComputeStateHash()), "yüklenen oyun aynı şekilde sürer");
        }

        [Test]
        public void Load_RejectsMismatchedOrMissingMapContent()
        {
            SimulationKernel kernel = NewKernel(3UL, out SimulationSetup setup);
            byte[] snapshot = kernel.CaptureSnapshot();

            var noTemplate = StandardRules.InstallGeographic(new SimulationSetup(), Content);
            Assert.That(SimulationKernel.Restore(noTemplate, snapshot).ErrorKey, Is.EqualTo(GeographyErrorKeys.CografyaSablonuYok));

            var otherMap = StandardRules.InstallGeographic(new SimulationSetup { GeoWorldTemplate = Build }, Content);
            Assert.That(SimulationKernel.Restore(otherMap, snapshot).ErrorKey, Is.EqualTo(GeographyErrorKeys.CografyaUyumsuz), "şehirsiz farklı içerik");
        }

        internal static GeoWorld WorldWithCities()
        {
            GeoWorld world = Build();
            Ok(world.AddCity(new City(GeoWarFixture.CityA1, "sehir.test.a1", P("40.5", "28.5"), A, A1, 5, Red, Fixed64.One)));
            Ok(world.AddCity(new City(GeoWarFixture.CityAP, "sehir.test.ap", P("40.1", "28.5"), A, AP, 3, Red, Fixed64.One)));
            Ok(world.AddCity(new City(GeoWarFixture.CityB1, "sehir.test.b1", P("40.5", "29.5"), B, B1, 5, Blue, Fixed64.One)));
            Ok(world.AddCity(new City(GeoWarFixture.CityCP, "sehir.test.cp", P("38.9", "28.5"), C, CP, 2, Red, Fixed64.One)));
            return world;
        }

        private static SimulationKernel NewKernel(ulong seed, out SimulationSetup setup)
        {
            GameState state = GameState.CreateNewGeo(seed, GameMode.Normal, Day1925, Red, WorldWithCities());
            state.Population.AddRegion(A, BaselineContent.AgePyramid(400_000));
            state.Population.AddRegion(B, BaselineContent.AgePyramid(400_000));
            state.Population.AddRegion(C, BaselineContent.AgePyramid(50_000));
            foreach (CountryId c in new[] { Red, Blue })
            {
                state.Economy.AddCountry(c, Fixed64.FromInt(50_000), Fixed64.Parse("0.2"));
                state.Economy.SeedStock(c, BaselineGoods.InfantryEquipment, Fixed64.FromInt(2_000));
            }

            Core.Diplomacy.DiplomacyRules.StartWar(state.Diplomacy, Red, Blue, Day1925);
            setup = StandardRules.InstallGeographic(new SimulationSetup { GeoWorldTemplate = WorldWithCities }, Content);
            Result<SimulationKernel> kernel = SimulationKernel.Create(setup, state);
            Assert.That(kernel.IsSuccess, Is.True, kernel.ErrorKey);
            return kernel.Value;
        }

        private static DayReport Advance(SimulationKernel kernel, int days)
        {
            DayReport last = null;
            for (int i = 0; i < days; i++)
            {
                Result<DayReport> report = kernel.AdvanceDay();
                Assert.That(report.IsSuccess, Is.True, report.ErrorKey);
                Assert.That(report.Value.Halted, Is.False, string.Join("\n", report.Value.Violations));
                last = report.Value;
            }

            return last;
        }

        private static LandUnit FirstUnit(GameState state, CountryId owner)
        {
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                if (state.Warfare.Units.ValueAt(i).Owner == owner)
                {
                    return state.Warfare.Units.ValueAt(i);
                }
            }

            Assert.Fail("birlik yok");
            return null;
        }
    }
}
