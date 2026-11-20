using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Tests.Support;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Simulation
{
    /// <summary>"Aynı tohum → aynı hash" (CLAUDE.md §4) ve kaydet/yükle sürekliliği.</summary>
    [TestFixture]
    public class DeterminismTests
    {
        private static SimulationKernel NewKernel(ulong seed) =>
            TestSimulation.Kernel(TestSimulation.Setup(new RandomTurmoilSystem()), TestSimulation.NewState(seed));

        /// <summary>Her iki çekirdeğe aynı günlerde aynı emirleri verir.</summary>
        private static void Script(SimulationKernel k, int day)
        {
            if (day % 7 == 0)
            {
                RegionId region = day % 2 == 0 ? TestWorlds.West : TestWorlds.East;
                k.State.World.TryGetControl(region, out RegionControl control);
                k.Submit(new SetRegionControllerCommand(TestWorlds.CountryA, region, control.Controller == TestWorlds.CountryA ? TestWorlds.CountryC : TestWorlds.CountryA));
            }

            if (day % 11 == 0)
            {
                k.Submit(new TacticalStrikeCommand(TestWorlds.CountryA, TestWorlds.InlandCity));
            }
        }

        private static List<string> Run(SimulationKernel k, int fromDay, int days)
        {
            var hashes = new List<string>();
            for (int d = fromDay; d < fromDay + days; d++)
            {
                Script(k, d);
                Result<DayReport> r = k.AdvanceDay();
                Assert.That(r.IsSuccess && !r.Value.Halted, Is.True);
                hashes.Add(Sha256.ToHex(r.Value.StateHash));
            }

            return hashes;
        }

        [Test]
        public void SameSeedAndOrders_GiveIdenticalHashesEveryDay()
        {
            List<string> a = Run(NewKernel(42), 0, 120);
            List<string> b = Run(NewKernel(42), 0, 120);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(new HashSet<string>(a).Count, Is.GreaterThan(100), "durum gerçekten değişiyor olmalı");

            List<string> other = Run(NewKernel(43), 0, 120);
            Assert.That(other[119], Is.Not.EqualTo(a[119]), "farklı tohum farklı tarih üretir");
        }

        [Test]
        public void SaveMidRun_RestoreAndContinue_MatchesUninterruptedRun()
        {
            const int split = 77; // 7 × 11: bölme gününde hem bekleyen hem anında çözülen emir verilir
            SimulationKernel continuous = NewKernel(7);
            List<string> expected = Run(continuous, 0, 120);

            SimulationKernel first = NewKernel(7);
            Run(first, 0, split);
            Script(first, split);
            Assert.That(first.State.Journal.Pending.Count, Is.EqualTo(1), "bekleyen emir anlık görüntüye girmeli");
            byte[] snapshot = first.CaptureSnapshot();

            Result<SimulationKernel> restored = SimulationKernel.Restore(TestSimulation.Setup(new RandomTurmoilSystem()), snapshot);
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(restored.Value.CaptureSnapshot(), Is.EqualTo(snapshot), "yeniden kodlama bayt bayt aynı");
            Assert.That(restored.Value.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()));

            var resumed = new List<string>();
            for (int d = split; d < 120; d++)
            {
                if (d != split)
                {
                    Script(restored.Value, d);
                }

                resumed.Add(Sha256.ToHex(restored.Value.AdvanceDay().Value.StateHash));
            }

            Assert.That(resumed, Is.EqualTo(expected.GetRange(split, 120 - split)));
        }

        [Test]
        public void PendingOrdersAndLedger_SurviveTheSnapshot()
        {
            SimulationKernel k = NewKernel(9);
            k.Submit(new SetRegionControllerCommand(TestWorlds.CountryA, TestWorlds.West, TestWorlds.CountryC));
            k.Submit(new TacticalStrikeCommand(TestWorlds.CountryA, TestWorlds.InlandCity));
            SimulationKernel copy = SimulationKernel.Restore(TestSimulation.Setup(new RandomTurmoilSystem()), k.CaptureSnapshot()).Value;
            Assert.That(copy.State.Journal.Pending.Count, Is.EqualTo(1));
            Assert.That(copy.State.Journal.Pending[0].Command, Is.TypeOf<SetRegionControllerCommand>());
            Assert.That(copy.State.Ledger.Count, Is.EqualTo(1));
            Assert.That(copy.StateVersion, Is.EqualTo(k.StateVersion));
        }

        [Test]
        public void StateHash_IgnoresVolatileUiVersion()
        {
            SimulationKernel k = NewKernel(3);
            byte[] before = k.ComputeStateHash();
            k.State.StateVersion += 100;
            Assert.That(k.ComputeStateHash(), Is.EqualTo(before));
            Assert.That(Sha256.ToHex(before), Has.Length.EqualTo(64));
        }

        /// <summary>
        /// Altın: 60 günlük betikli simülasyonun son durum hash'i. Değişirse kayıt/yeniden oynatma
        /// uyumluluğu kırılmıştır (PRNG, codec, faz sırası veya kural değişimi).
        /// </summary>
        [Test]
        public void GoldenHash_After60Days()
        {
            List<string> hashes = Run(NewKernel(1923), 0, 60);
            Assert.That(hashes[59], Is.EqualTo(GoldenValues.KernelDay60), "yeni hash: " + hashes[59]);
        }
    }
}
