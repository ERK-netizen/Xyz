using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.Campaign
{
    /// <summary>
    /// 365 günlük başsız kampanya (Gözlemci modu, dört YZ ülkesi): determinizm, kaydet/yükle
    /// sürekliliği ve game-balance bantları. Bantlar ölçümle belirlendi (6 tohum: hazine 1,5–2,0×,
    /// nüfus −%12,5…+%1,7, en büyük toprak payı ≤ 0,29, 12–25 muharebe) ve gerekçeleri:
    /// <list type="bullet">
    /// <item>Hazine / başlangıç ∈ [0,6; 2,5]: ne iflas ne kaçak ekonomi.</item>
    /// <item>Nüfus değişimi ∈ [−%15; +%5]: bir yıllık savaş ağır ama yıkıcı değil (barışta ≈ +%1,7).</item>
    /// <item>Seferber / çalışma çağı ≤ 0,20: tarihî seferberlik bandının üstü.</item>
    /// <item>En büyük ülkenin kontrol ettiği kara payı ≤ 0,40 (başlangıç 0,25): kartopu yok.</item>
    /// <item>Her tohumda en az bir savaş ve bir barış, en az bir muharebe: YZ mekanikleri kullanıyor.</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class AiCampaignTests
    {
        private const int Days = 365;

        private sealed class Outcome
        {
            public readonly List<string> Hashes = new List<string>();
            public int WarsDeclared;
            public int PeacesMade;
            public int Battles;
        }

        private static Outcome Run(SimulationKernel k, int from, int to)
        {
            var o = new Outcome();
            for (int d = from; d < to; d++)
            {
                int before = k.State.Diplomacy.Wars.Count;
                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, r.IsSuccess ? string.Join("\n", r.Value.Violations) : r.ErrorKey);
                o.Hashes.Add(Sha256.ToHex(r.Value.StateHash));
                int after = k.State.Diplomacy.Wars.Count;
                if (after > before)
                {
                    o.WarsDeclared += after - before;
                }
                else if (after < before)
                {
                    o.PeacesMade += before - after;
                }

                for (int i = 0; i < r.Value.Notes.Count; i++)
                {
                    if (r.Value.Notes[i].StartsWith("bilgi.savas.muharebe", System.StringComparison.Ordinal))
                    {
                        o.Battles++;
                    }
                }
            }

            return o;
        }

        [Test]
        public void Campaign365_IsDeterministic_SurvivesSaveLoad_AndMatchesGolden()
        {
            SimulationKernel a = new ContinentScenario(7).Kernel();
            Outcome full = Run(a, 0, Days);

            SimulationKernel b = new ContinentScenario(7).Kernel();
            Run(b, 0, 180);
            Result<SimulationKernel> restored = SimulationKernel.Restore(ContinentScenario.Setup(), b.CaptureSnapshot());
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Outcome resumed = Run(restored.Value, 180, Days);
            Assert.That(resumed.Hashes, Is.EqualTo(full.Hashes.GetRange(180, Days - 180)), "kayıttan devam eden YZ kampanyası birebir aynı");
            Assert.That(full.Hashes[Days - 1], Is.EqualTo(Support.GoldenValues.CampaignDay365), "yeni hash: " + full.Hashes[Days - 1]);
        }

        [TestCase(1UL)]
        [TestCase(2UL)]
        [TestCase(3UL)]
        [TestCase(4UL)]
        public void Campaign365_StaysInsideBalanceBands(ulong seed)
        {
            var scenario = new ContinentScenario(seed);
            SimulationKernel k = scenario.Kernel();
            var startPop = new Dictionary<ulong, long>();
            foreach (CountryId c in ContinentScenario.All)
            {
                startPop[c.Value] = Population(k, c);
            }

            Fixed64 startTreasury = k.State.Economy.Countries[ContinentScenario.A].Treasury;
            Outcome o = Run(k, 0, Days);

            Assert.That(o.WarsDeclared, Is.GreaterThanOrEqualTo(1), "YZ savaş ilan eder");
            Assert.That(o.PeacesMade, Is.GreaterThanOrEqualTo(1), "YZ iki taraflı barış yapar");
            Assert.That(o.Battles, Is.GreaterThan(0), "ordular çarpışır");

            int land = 0;
            var controlled = new Dictionary<ulong, int>();
            for (int i = 0; i < k.State.World.CellCount; i++)
            {
                HexCell cell = k.State.World.CellAt(i);
                if (!cell.IsWater && cell.Controller.IsValid)
                {
                    land++;
                    controlled[cell.Controller.Value] = (controlled.TryGetValue(cell.Controller.Value, out int n) ? n : 0) + 1;
                }
            }

            foreach (CountryId c in ContinentScenario.All)
            {
                string who = "ülke " + c.Value + ", tohum " + seed;
                Fixed64 treasuryRatio = Fixed64.MulDiv(k.State.Economy.Countries[c].Treasury, Fixed64.One, startTreasury);
                Assert.That(treasuryRatio >= Fixed64.Parse("0.6") && treasuryRatio <= Fixed64.Parse("2.5"), Is.True, who + ": hazine oranı " + treasuryRatio);

                Fixed64 popChange = Fixed64.FromRatio(Population(k, c) - startPop[c.Value], startPop[c.Value]);
                Assert.That(popChange >= Fixed64.Parse("-0.15") && popChange <= Fixed64.Parse("0.05"), Is.True, who + ": nüfus değişimi " + popChange);

                Assert.That(MobilizedShare(k, c) <= Fixed64.Parse("0.2"), Is.True, who + ": seferberlik " + MobilizedShare(k, c));

                Fixed64 share = Fixed64.FromRatio(controlled.TryGetValue(c.Value, out int held) ? held : 0, land);
                Assert.That(share <= Fixed64.Parse("0.4"), Is.True, who + ": toprak payı " + share);
            }
        }

        private static long Population(SimulationKernel k, CountryId country)
        {
            long total = 0;
            for (int i = 0; i < k.State.World.Regions.Count; i++)
            {
                StrategicRegion region = k.State.World.Regions.ValueAt(i);
                if (region.Owner == country)
                {
                    total += k.State.Population.Regions[region.Id].Total;
                }
            }

            return total;
        }

        private static Fixed64 MobilizedShare(SimulationKernel k, CountryId country)
        {
            long working = 0;
            long mobilized = 0;
            for (int i = 0; i < k.State.World.Regions.Count; i++)
            {
                StrategicRegion region = k.State.World.Regions.ValueAt(i);
                if (region.Owner == country)
                {
                    RegionPopulation p = k.State.Population.Regions[region.Id];
                    working += p.WorkingAge;
                    mobilized += p.Mobilized;
                }
            }

            return working == 0 ? Fixed64.Zero : Fixed64.FromRatio(mobilized, working);
        }
    }
}
