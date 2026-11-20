using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using TarihStrateji.Core.Content;
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
    /// "1914 MVP" denge taraması (game-balance becerisi): sabit tohum listesi × 365 gün, YZ'li Gözlemci koşusu;
    /// metrikler yalnız okunur (kurallara durum eklenmez). Her metrik için P10/P50/P90 yazılır. Uzun sürdüğü
    /// için CI'da koşmaz (<see cref="ExplicitAttribute"/>); elle: <c>dotnet test --filter Category=Denge</c>.
    /// Tohum sayısı <c>DENGE_TOHUM</c> (varsayılan 32), rapor dosyası <c>DENGE_CIKTI</c> ortam değişkeniyle.
    /// Bant testleri (CI kapısı) <see cref="Campaign1914Tests"/>'tedir; bu sınıf karar verisi üretir.
    /// </summary>
    [TestFixture]
    [Explicit("Uzun denge taraması; elle çalıştırılır.")]
    [Category("Denge")]
    public class BalanceSweep1914
    {
        private const int Days = 365;

        [Test]
        public void Sweep()
        {
            int seeds = int.TryParse(Environment.GetEnvironmentVariable("DENGE_TOHUM"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 32;
            var metrics = new SortedDictionary<string, List<long>>(StringComparer.Ordinal);
            for (int s = 1; s <= seeds; s++)
            {
                RunSeed((ulong)s, metrics);
            }

            var report = new StringBuilder();
            report.Append("| Metrik | P10 | P50 | P90 | En az | En çok |\n|---|---|---|---|---|---|\n");
            foreach (KeyValuePair<string, List<long>> m in metrics)
            {
                m.Value.Sort();
                report.Append("| ").Append(m.Key).Append(" | ").Append(Fmt(m.Key, Pct(m.Value, 10))).Append(" | ").Append(Fmt(m.Key, Pct(m.Value, 50)))
                    .Append(" | ").Append(Fmt(m.Key, Pct(m.Value, 90))).Append(" | ").Append(Fmt(m.Key, m.Value[0])).Append(" | ")
                    .Append(Fmt(m.Key, m.Value[m.Value.Count - 1])).Append(" |\n");
            }

            TestContext.Progress.WriteLine(report.ToString());
            string output = Environment.GetEnvironmentVariable("DENGE_CIKTI");
            if (!string.IsNullOrEmpty(output))
            {
                File.WriteAllText(output, "Tohum: " + seeds + ", gün: " + Days + "\n\n" + report);
            }
        }

        private static void RunSeed(ulong seed, SortedDictionary<string, List<long>> metrics)
        {
            SimulationKernel k = Scenario1914Tests.Kernel(seed, GameMode.Observer, true);
            var countries = new List<CountryId>();
            for (int i = 0; i < k.State.Economy.Countries.Count; i++)
            {
                countries.Add(k.State.Economy.Countries.KeyAt(i));
            }

            var treasury = new Dictionary<CountryId, Fixed64>();
            var population = new Dictionary<CountryId, long>();
            var home = new Dictionary<CountryId, List<RegionId>>();
            var held = new Dictionary<CountryId, int>();
            var daysAtWar = new Dictionary<CountryId, int>();
            for (int i = 0; i < countries.Count; i++)
            {
                CountryId c = countries[i];
                treasury[c] = k.State.Economy.Countries[c].Treasury;
                home[c] = Owned(k, c);
                population[c] = Population(k, home[c]);
                held[c] = Held(k, c);
                daysAtWar[c] = 0;
            }

            var warStart = new Dictionary<WarId, int>();
            var durations = new List<long>();
            int declared = 0;
            int firstWar = -1;
            int battles = 0;
            int capitulations = 0;
            int maxWars = 0;
            for (int d = 1; d <= Days; d++)
            {
                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True, "tohum " + seed + " gün " + d);
                for (int i = 0; i < r.Value.Notes.Count; i++)
                {
                    string note = r.Value.Notes[i];
                    battles += note.StartsWith("bilgi.savas.muharebe", StringComparison.Ordinal) ? 1 : 0;
                    capitulations += note.StartsWith("bilgi.diplomasi.teslim", StringComparison.Ordinal) ? 1 : 0;
                }

                var live = new HashSet<WarId>();
                for (int i = 0; i < k.State.Diplomacy.Wars.Count; i++)
                {
                    WarId id = k.State.Diplomacy.Wars.KeyAt(i);
                    live.Add(id);
                    if (!warStart.ContainsKey(id))
                    {
                        warStart[id] = d;
                        declared++;
                        firstWar = firstWar < 0 ? d : firstWar;
                    }
                }

                var ended = new List<WarId>();
                foreach (KeyValuePair<WarId, int> w in warStart)
                {
                    if (!live.Contains(w.Key) && w.Value > 0)
                    {
                        ended.Add(w.Key);
                    }
                }

                ended.Sort();
                for (int i = 0; i < ended.Count; i++)
                {
                    durations.Add(d - warStart[ended[i]]);
                    warStart[ended[i]] = -1;
                }

                maxWars = Math.Max(maxWars, live.Count);
                for (int i = 0; i < countries.Count; i++)
                {
                    daysAtWar[countries[i]] += k.State.Diplomacy.IsAtAnyWar(countries[i]) ? 1 : 0;
                }
            }

            Add(metrics, "A01 ilk savaş günü", firstWar < 0 ? Days + 1 : firstWar);
            Add(metrics, "A02 yılda başlayan savaş çifti", declared);
            Add(metrics, "A03 aynı anda en çok savaş", maxWars);
            Add(metrics, "A04 muharebe sayısı", battles);
            Add(metrics, "A05 kapitülasyon", capitulations);
            Add(metrics, "A06 biten savaş çifti", durations.Count);
            for (int i = 0; i < durations.Count; i++)
            {
                Add(metrics, "A07 biten savaşın süresi (gün)", durations[i]);
            }

            // Sansürlü veri: yalnız bitenlerin süresine bakmak hayatta kalma yanlılığı verir (eşik yükselince uzun
            // savaşlar bitmez, ortancaya yalnız kısa olanlar girer). Başlayan savaşlara oranlanmış paylar asıl ölçüdür.
            int shortWars = 0;
            for (int i = 0; i < durations.Count; i++)
            {
                shortWars += durations[i] < 120 ? 1 : 0;
            }

            Add(metrics, "A10 120 günden kısa biten savaş ‰ (başlayanlara)", declared == 0 ? 0 : shortWars * 1000L / declared);
            Add(metrics, "A11 yıl sonunda süren savaş ‰ (başlayanlara)", declared == 0 ? 0 : (declared - durations.Count) * 1000L / declared);

            int regions = k.State.World.Regions.Count;
            int largest = 0;
            for (int i = 0; i < countries.Count; i++)
            {
                CountryId c = countries[i];
                int now = Held(k, c);
                largest = Math.Max(largest, now - held[c]);
                Add(metrics, "B01 hazine oranı ‰ (ülke)", Fixed64.MulDiv(k.State.Economy.Countries[c].Treasury, Fixed64.FromInt(1000), treasury[c]).RoundToLong());
                Add(metrics, "B02 başlangıç bölgeleri nüfus değişimi ‰ (ülke)", (Population(k, home[c]) - population[c]) * 1000L / Math.Max(1L, population[c]));
                Add(metrics, "B03 seferber / çalışma çağı ‰ (ülke, güncel bölgeler)", Mobilized(k, Owned(k, c)));
                Add(metrics, "B04 bölge kazancı (ülke)", now - held[c]);
                Add(metrics, "B05 savaşta geçen gün (ülke)", daysAtWar[c]);
                if (c == Scenario1914.Ottoman)
                {
                    Add(metrics, "C01 Osmanlı hazine oranı ‰", Fixed64.MulDiv(k.State.Economy.Countries[c].Treasury, Fixed64.FromInt(1000), treasury[c]).RoundToLong());
                    Add(metrics, "C02 Osmanlı bölge kazancı", now - held[c]);
                    Add(metrics, "C03 Osmanlı savaşta geçen gün", daysAtWar[c]);
                }
            }

            Add(metrics, "A08 en büyük bölge kazancı (tohum)", largest);
            Add(metrics, "A09 bölge sayısı", regions);
        }

        private static string Fmt(string key, long v) => v.ToString(CultureInfo.InvariantCulture);

        private static long Pct(List<long> sorted, int p)
        {
            int index = (((sorted.Count - 1) * p) + 50) / 100;
            return sorted[index];
        }

        private static void Add(SortedDictionary<string, List<long>> metrics, string key, long value)
        {
            if (!metrics.TryGetValue(key, out List<long> list))
            {
                list = new List<long>();
                metrics.Add(key, list);
            }

            list.Add(value);
        }

        private static List<RegionId> Owned(SimulationKernel k, CountryId country)
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

        private static long Mobilized(SimulationKernel k, List<RegionId> regions)
        {
            long working = 0L;
            long mobilized = 0L;
            for (int i = 0; i < regions.Count; i++)
            {
                RegionPopulation p = k.State.Population.Regions[regions[i]];
                working += p.WorkingAge;
                mobilized += p.Mobilized;
            }

            return working == 0L ? 0L : mobilized * 1000L / working;
        }
    }
}
