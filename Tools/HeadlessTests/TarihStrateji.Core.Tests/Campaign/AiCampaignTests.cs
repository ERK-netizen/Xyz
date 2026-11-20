using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Tests.Scenario;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Campaign
{
    /// <summary>
    /// 365 günlük başsız kampanya (Gözlemci modu, "Ege 1925" savaşla başlar, iki YZ ülkesi): determinizm,
    /// kaydet/yükle sürekliliği ve game-balance bantları. Bantlar ve gerekçeleri:
    /// <list type="bullet">
    /// <item>Hazine / başlangıç ∈ [0,6; 2,5]: ne iflas ne kaçak ekonomi (ölçüm: ≈ 0,75–1,25).</item>
    /// <item>Nüfus değişimi ∈ [−%15; +%5]: bir yıllık savaş ağır ama yıkıcı değil.</item>
    /// <item>Seferber / çalışma çağı ≤ 0,20: tarihî seferberlik bandının üstü.</item>
    /// <item>Bir ülkenin kontrol ettiği bölge payı en çok 0,25 artar: kartopu yok.</item>
    /// <item>Her tohumda savaş iki taraflı barışla biter ve en az bir muharebe olur: YZ mekanikleri kullanıyor
    /// (Edirne–Drama cephesinde yığınak, üstünlükte saldırı, çıkmazda barış).</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class AiCampaignTests
    {
        private const int Days = 365;
        private static readonly CountryId[] All = { AegeanWar.Turkey, AegeanWar.Greece };

        private static SimulationKernel NewKernel(ulong seed) =>
            TestSimulation.Kernel(AegeanWar.Setup(ai: true), new AegeanWar(seed, GameMode.Observer, atWar: true).State);

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
            SimulationKernel a = NewKernel(7);
            Outcome full = Run(a, 0, Days);

            SimulationKernel b = NewKernel(7);
            Run(b, 0, 180);
            Result<SimulationKernel> restored = SimulationKernel.Restore(AegeanWar.Setup(ai: true), b.CaptureSnapshot());
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
            SimulationKernel k = NewKernel(seed);
            var startPop = new Dictionary<ulong, long>();
            foreach (CountryId c in All)
            {
                startPop[c.Value] = Population(k, c);
            }

            Fixed64 startTreasury = k.State.Economy.Countries[AegeanWar.Turkey].Treasury;
            var startShare = new Dictionary<ulong, Fixed64>();
            foreach (CountryId c in All)
            {
                startShare[c.Value] = ControlledShare(k, c);
            }

            Outcome o = Run(k, 0, Days);

            Assert.That(o.PeacesMade, Is.GreaterThanOrEqualTo(1), "YZ iki taraflı barış yapar");
            Assert.That(o.Battles, Is.GreaterThan(0), "ordular çarpışır");

            foreach (CountryId c in All)
            {
                string who = "ülke " + c.Value + ", tohum " + seed;
                Fixed64 treasuryRatio = Fixed64.MulDiv(k.State.Economy.Countries[c].Treasury, Fixed64.One, startTreasury);
                Assert.That(treasuryRatio >= Fixed64.Parse("0.6") && treasuryRatio <= Fixed64.Parse("2.5"), Is.True, who + ": hazine oranı " + treasuryRatio);

                Fixed64 popChange = Fixed64.FromRatio(Population(k, c) - startPop[c.Value], startPop[c.Value]);
                Assert.That(popChange >= Fixed64.Parse("-0.15") && popChange <= Fixed64.Parse("0.05"), Is.True, who + ": nüfus değişimi " + popChange);

                Assert.That(MobilizedShare(k, c) <= Fixed64.Parse("0.2"), Is.True, who + ": seferberlik " + MobilizedShare(k, c));

                Fixed64 gain = ControlledShare(k, c) - startShare[c.Value];
                Assert.That(gain <= Fixed64.Parse("0.25"), Is.True, who + ": bölge payı artışı " + gain);
            }
        }

        private static long Population(SimulationKernel k, CountryId country)
        {
            long total = 0;
            for (int i = 0; i < k.State.World.Regions.Count; i++)
            {
                k.State.World.TryGetControl(k.State.World.Regions[i], out RegionControl region);
                if (region.Owner == country)
                {
                    total += k.State.Population.Regions[region.Region].Total;
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
                k.State.World.TryGetControl(k.State.World.Regions[i], out RegionControl region);
                if (region.Owner == country)
                {
                    RegionPopulation p = k.State.Population.Regions[region.Region];
                    working += p.WorkingAge;
                    mobilized += p.Mobilized;
                }
            }

            return working == 0 ? Fixed64.Zero : Fixed64.FromRatio(mobilized, working);
        }

        private static Fixed64 ControlledShare(SimulationKernel k, CountryId country)
        {
            int held = 0;
            for (int i = 0; i < k.State.World.Regions.Count; i++)
            {
                k.State.World.TryGetControl(k.State.World.Regions[i], out RegionControl region);
                if (region.Controller == country)
                {
                    held++;
                }
            }

            return Fixed64.FromRatio(held, k.State.World.Regions.Count);
        }
    }
}
