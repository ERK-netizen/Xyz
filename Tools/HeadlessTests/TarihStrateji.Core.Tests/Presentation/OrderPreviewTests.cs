using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.ReadModel;
using TarihStrateji.Core.Tests.Scenario;
using TarihStrateji.Core.Tests.Simulation;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Tests.Presentation
{
    /// <summary>Mobil emir akışının önizlemesi (Adım 16): emirle aynı kurallar, durumu değiştirmez, savaş sisine uyar.</summary>
    [TestFixture]
    public class OrderPreviewTests
    {
        private static TargetRef City(AegeanWar s, string key) => TargetRef.Entity(TargetKind.City, s.City(key).Value);

        [Test]
        public void MovePreview_HasRouteDaysAndSupply_AndDoesNotChangeState()
        {
            var s = new AegeanWar(seed: 7);
            LandUnit unit = s.Place(AegeanWar.Turkey, "sehir.ege.edirne", AegeanWar.Drama);
            SimulationKernel k = TestSimulation.Kernel(AegeanWar.Setup(), s.State);
            byte[] before = k.CaptureSnapshot();

            var path = new List<GeoPoint>();
            OrderPreview p = k.PreviewOrder(unit.Id.Value, City(s, "sehir.ege.istanbul"), false, path);

            Assert.That(p.IsValid, Is.True, p.ErrorKey);
            Assert.That(p.IsAttack, Is.False);
            Assert.That(path.Count, Is.GreaterThanOrEqualTo(2), "konum + en az bir düğüm");
            k.State.World.TryGetCity(s.City("sehir.ege.istanbul"), out City istanbul);
            Assert.That(path[path.Count - 1], Is.EqualTo(istanbul.Location));
            Assert.That(p.Days, Is.GreaterThanOrEqualTo(1));
            Assert.That(p.SupplyAtTarget, Is.EqualTo(Fixed64.One), "kendi ana şehri tam ikmal kaynağı");
            Assert.That(p.Risk, Is.EqualTo(OrderPreview.RiskNone));
            Assert.That(unit.Route.Count, Is.EqualTo(0), "önizleme emir vermez");
            Assert.That(k.CaptureSnapshot(), Is.EqualTo(before), "önizleme durumu değiştirmez");

            // Daha uzak hedef daha uzun sürer.
            OrderPreview far = k.PreviewOrder(unit.Id.Value, City(s, "sehir.ege.ankara"), false, new List<GeoPoint>());
            Assert.That(far.IsValid, Is.True, far.ErrorKey);
            Assert.That(far.Days, Is.GreaterThan(p.Days));
        }

        [Test]
        public void InvalidOrders_ReturnTheValidationErrorKey()
        {
            var s = new AegeanWar(seed: 7);
            LandUnit greek = s.Place(AegeanWar.Greece, "sehir.ege.selanik", AegeanWar.Drama);
            LandUnit own = s.Place(AegeanWar.Turkey, "sehir.ege.istanbul", s.State.World.TryGetCity(s.City("sehir.ege.istanbul"), out City c) ? c.Node : default);
            SimulationKernel k = TestSimulation.Kernel(AegeanWar.Setup(), s.State);

            OrderPreview foreign = k.PreviewOrder(greek.Id.Value, City(s, "sehir.ege.istanbul"), false, new List<GeoPoint>());
            Assert.That(foreign.IsValid, Is.False, "yabancı birliğe emir verilemez");
            Assert.That(foreign.ErrorKey, Is.Not.Null.And.StartsWith("hata."));

            var path = new List<GeoPoint> { default };
            OrderPreview tooFar = k.PreviewOrder(own.Id.Value, TargetRef.Entity(TargetKind.LandUnit, greek.Id.Value), true, path);
            Assert.That(tooFar.IsValid, Is.False, "bitişik olmayan hedefe saldırı geçersiz");
            Assert.That(path, Is.Empty, "geçersiz önizlemede rota yok");
        }

        [Test]
        public void AttackPreview_UsesOnlyKnownEnemies_AndRatesRisk()
        {
            var s = new AegeanWar(seed: 7);
            LandUnit scout = s.Place(AegeanWar.Turkey, "sehir.ege.edirne", AegeanWar.Drama);
            LandUnit enemy = s.Place(AegeanWar.Greece, "sehir.ege.selanik", AegeanWar.Drama);
            SimulationKernel k = TestSimulation.Kernel(AegeanWar.Setup(), s.State);
            var target = TargetRef.Entity(TargetKind.LandUnit, enemy.Id.Value);

            // Gün geçmeden istihbarat yok: düşman bilinmiyor, risk yok.
            OrderPreview blind = k.PreviewOrder(scout.Id.Value, target, true, new List<GeoPoint>());
            Assert.That(blind.IsValid, Is.True, blind.ErrorKey);
            Assert.That(blind.EnemyPower, Is.EqualTo(Fixed64.Zero), "savaş sisi: görülmemiş birlik hesaba girmez");

            TestSimulation.AdvanceOk(k, 1);
            var path = new List<GeoPoint>();
            OrderPreview seen = k.PreviewOrder(scout.Id.Value, target, true, path);
            Assert.That(seen.IsValid && seen.IsAttack, Is.True, seen.ErrorKey);
            Assert.That(seen.Days, Is.EqualTo(0));
            Assert.That(seen.EnemyPower.IsPositive, Is.True, "temas güce girer");
            Assert.That(seen.Risk, Is.EqualTo(OrderPreviewProjection.Risk(seen.OwnPower, seen.EnemyPower)));
            Assert.That(path.Count, Is.EqualTo(2));
        }

        [Test]
        public void Risk_Bands()
        {
            Assert.That(OrderPreviewProjection.Risk(Fixed64.One, Fixed64.Zero), Is.EqualTo(OrderPreview.RiskNone));
            Assert.That(OrderPreviewProjection.Risk(Fixed64.FromInt(2), Fixed64.One), Is.EqualTo(OrderPreview.RiskLow));
            Assert.That(OrderPreviewProjection.Risk(Fixed64.Parse("1.5"), Fixed64.One), Is.EqualTo(OrderPreview.RiskMedium));
            Assert.That(OrderPreviewProjection.Risk(Fixed64.Parse("0.9"), Fixed64.One), Is.EqualTo(OrderPreview.RiskHigh));
        }
    }
}
