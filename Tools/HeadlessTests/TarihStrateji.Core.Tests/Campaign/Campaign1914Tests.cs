using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Tests.Scenario;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Campaign
{
    /// <summary>
    /// "1914 MVP" 365 günlük başsız YZ kampanyası (Gözlemci, 12 ülke, barışla başlar), 4 tohum. Bantlar
    /// Adım 14 ölçümünden (4 tohum) pay bırakılarak konuldu; asıl 1914 dengesi Adım 17'nin işidir, bu test
    /// gerilemeyi (iflas, kıtlık çöküşü, kartopu, kilitlenme) yakalar:
    /// <list type="bullet">
    /// <item>Hazine / başlangıç ∈ [0,25; 3,0] (ölçüm 0,31–2,63; Osmanlı savaş harcamasıyla en düşük).</item>
    /// <item>Başlangıçta sahip olunan bölgelerin nüfus değişimi ∈ [−%25; +%5] (ölçüm −%18…+%1,8; toprak kazancı sayılmaz).</item>
    /// <item>Seferber / çalışma çağı ≤ 0,30 (ölçüm ≤ 0,29: bölge kaybeden küçük devletler; büyükler ≈ 0,12 tavanında).</item>
    /// <item>Bir ülke en çok 6 bölge kazanır (ölçüm en çok +5): kartopu yok.</item>
    /// <item>Her tohumda ≥ 1 savaş, ≥ 1 barış ve > 0 muharebe: YZ diplomasi ve ordu mekaniklerini kullanıyor.</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class Campaign1914Tests
    {
        private const int Days = 365;

        [TestCase(1UL)]
        [TestCase(2UL)]
        [TestCase(3UL)]
        [TestCase(4UL)]
        public void Campaign1914_StaysInsideBalanceBands(ulong seed)
        {
            SimulationKernel k = Scenario1914Tests.Kernel(seed, GameMode.Observer, true);
            var home = new Dictionary<ulong, List<RegionId>>();
            var treasury = new Dictionary<ulong, Fixed64>();
            var population = new Dictionary<ulong, long>();
            var held = new Dictionary<ulong, int>();
            for (int i = 0; i < k.State.Economy.Countries.Count; i++)
            {
                CountryId c = k.State.Economy.Countries.KeyAt(i);
                home[c.Value] = HomeRegions(k, c);
                treasury[c.Value] = k.State.Economy.Countries.ValueAt(i).Treasury;
                population[c.Value] = Population(k, home[c.Value]);
                held[c.Value] = Held(k, c);
            }

            int wars = 0;
            int peaces = 0;
            int battles = 0;
            for (int d = 0; d < Days; d++)
            {
                int before = k.State.Diplomacy.Wars.Count;
                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, r.IsSuccess ? string.Join("\n", r.Value.Violations) : r.ErrorKey);
                int after = k.State.Diplomacy.Wars.Count;
                wars += after > before ? after - before : 0;
                peaces += after < before ? before - after : 0;
                for (int n = 0; n < r.Value.Notes.Count; n++)
                {
                    if (r.Value.Notes[n].StartsWith("bilgi.savas.muharebe", System.StringComparison.Ordinal))
                    {
                        battles++;
                    }
                }
            }

            Assert.That(wars, Is.GreaterThanOrEqualTo(1), "YZ savaş ilan eder");
            Assert.That(peaces, Is.GreaterThanOrEqualTo(1), "YZ iki taraflı barış yapar");
            Assert.That(battles, Is.GreaterThan(0), "ordular çarpışır");
            for (int i = 0; i < k.State.Economy.Countries.Count; i++)
            {
                CountryId c = k.State.Economy.Countries.KeyAt(i);
                string who = "ülke " + c.Value + ", tohum " + seed;
                Fixed64 ratio = Fixed64.MulDiv(k.State.Economy.Countries.ValueAt(i).Treasury, Fixed64.One, treasury[c.Value]);
                Assert.That(ratio >= Fixed64.Parse("0.25") && ratio <= Fixed64.FromInt(3), Is.True, who + ": hazine oranı " + ratio);
                Fixed64 change = Fixed64.FromRatio(Population(k, home[c.Value]) - population[c.Value], population[c.Value]);
                Assert.That(change >= Fixed64.Parse("-0.25") && change <= Fixed64.Parse("0.05"), Is.True, who + ": nüfus değişimi " + change);
                // Seferberlik ülkenin şu anki bölgelerinde ölçülür: barışla devredilen bölgedeki seferberler yeni
                // sahibin askerleridir (başlangıç bölgeleriyle ölçmek onları eski sahibe yazar).
                Fixed64 mobilized = MobilizedShare(k, HomeRegions(k, c));
                Assert.That(mobilized <= Fixed64.Parse("0.3"), Is.True, who + ": seferberlik " + mobilized);
                Assert.That(Held(k, c) - held[c.Value], Is.LessThanOrEqualTo(6), who + ": bölge kazancı");
            }
        }

        private static List<RegionId> HomeRegions(SimulationKernel k, CountryId country)
        {
            var list = new List<RegionId>();
            for (int i = 0; i < k.State.World.Regions.Count; i++)
            {
                k.State.World.TryGetControl(k.State.World.Regions[i], out RegionControl rc);
                if (rc.Owner == country)
                {
                    list.Add(rc.Region);
                }
            }

            return list;
        }

        private static int Held(SimulationKernel k, CountryId country)
        {
            int n = 0;
            for (int i = 0; i < k.State.World.Regions.Count; i++)
            {
                k.State.World.TryGetControl(k.State.World.Regions[i], out RegionControl rc);
                n += rc.Controller == country ? 1 : 0;
            }

            return n;
        }

        private static long Population(SimulationKernel k, List<RegionId> regions)
        {
            long total = 0L;
            for (int i = 0; i < regions.Count; i++)
            {
                total += k.State.Population.Regions[regions[i]].Total;
            }

            return total;
        }

        private static Fixed64 MobilizedShare(SimulationKernel k, List<RegionId> regions)
        {
            long working = 0L;
            long mobilized = 0L;
            for (int i = 0; i < regions.Count; i++)
            {
                RegionPopulation p = k.State.Population.Regions[regions[i]];
                working += p.WorkingAge;
                mobilized += p.Mobilized;
            }

            return working == 0L ? Fixed64.Zero : Fixed64.FromRatio(mobilized, working);
        }
    }
}
