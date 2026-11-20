using System;
using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.Map;
using TarihStrateji.Unity.Presentation.UI;

namespace TarihStrateji.Tests.EditMode
{
    /// <summary>Mobil emir akışı (Adım 16): önizle → onayla/vazgeç, seçim değişince kapanma, durum değişince tazeleme, rota rengi, biçim.</summary>
    public class OrderFlowTests
    {
        private static readonly TargetRef Target = TargetRef.Entity(TargetKind.City, 7UL);

        [Test]
        public void Confirm_ProducesTheMatchingCommand_AndCloses()
        {
            var model = new FakeModel { Next = Valid(3, OrderPreview.RiskNone) };
            var orders = new FakeOrders();
            var flow = new OrderFlow();

            Assert.That(flow.Open(model, 5UL, Target, false), Is.True);
            Assert.That(flow.IsOpen && flow.CanConfirm, Is.True);
            Assert.That(flow.Path.Count, Is.EqualTo(3));
            Assert.That(model.LastAttack, Is.False);

            GameCommand command = flow.Confirm(orders);
            Assert.That(command, Is.Not.Null);
            Assert.That(orders.Last, Is.EqualTo("yuru:5"));
            Assert.That(flow.IsOpen, Is.False);
            Assert.That(flow.Path.Count, Is.EqualTo(0));

            flow.Open(model, 5UL, Target, true);
            flow.Confirm(orders);
            Assert.That(orders.Last, Is.EqualTo("saldir:5"));
        }

        [Test]
        public void InvalidPreview_StaysOpenWithReason_ButCannotConfirm()
        {
            var model = new FakeModel { Next = OrderPreview.Invalid("hata.savas.rotaYok") };
            var orders = new FakeOrders();
            var flow = new OrderFlow();

            Assert.That(flow.Open(model, 5UL, Target, false), Is.False);
            Assert.That(flow.IsOpen, Is.True, "sebep gösterilsin diye açık kalır");
            Assert.That(flow.CanConfirm, Is.False);
            Assert.That(flow.Preview.ErrorKey, Is.EqualTo("hata.savas.rotaYok"));
            Assert.That(flow.Confirm(orders), Is.Null);
            Assert.That(orders.Last, Is.Null, "geçersiz emir üretilmez");
            Assert.That(flow.Open(model, 0UL, Target, false), Is.False);
            Assert.That(flow.IsOpen, Is.False, "birliksiz önizleme açılmaz");
        }

        [Test]
        public void SelectionChange_Closes_StateChange_Refreshes()
        {
            var model = new FakeModel { Next = Valid(2, OrderPreview.RiskLow) };
            var flow = new OrderFlow();
            int changes = 0;
            flow.Changed += () => changes++;

            flow.Open(model, 5UL, Target, true);
            flow.FollowSelection(5UL, Target);
            Assert.That(flow.IsOpen, Is.True, "aynı seçim kapatmaz");

            model.Next = OrderPreview.Invalid("hata.savas.hedefGecersiz");
            flow.Refresh(model);
            Assert.That(flow.CanConfirm, Is.False, "gün geçince geçersizleşen emir onaylanamaz");
            Assert.That(model.Calls, Is.EqualTo(2));

            flow.FollowSelection(5UL, TargetRef.Entity(TargetKind.City, 8UL));
            Assert.That(flow.IsOpen, Is.False, "hedef değişince kapanır");
            Assert.That(changes, Is.EqualTo(3));
            flow.Refresh(model);
            Assert.That(model.Calls, Is.EqualTo(2), "kapalı akış önizleme istemez");
        }

        [Test]
        public void RouteColor_FollowsOrderKindAndRisk_AndFormatting()
        {
            Assert.That(RouteLayer.ColorFor(false, OrderPreview.RiskHigh), Is.EqualTo(RouteLayer.ColorFor(false, OrderPreview.RiskNone)), "yürüyüş tek renk");
            Assert.That(RouteLayer.ColorFor(true, OrderPreview.RiskHigh), Is.Not.EqualTo(RouteLayer.ColorFor(true, OrderPreview.RiskLow)));
            Assert.That(RouteLayer.ColorFor(true, 99), Is.EqualTo(RouteLayer.ColorFor(true, OrderPreview.RiskHigh)), "taşma en yüksek riske sıkışır");
            Assert.That(HudController.RiskKey(OrderPreview.RiskMedium), Is.EqualTo("arayuz.risk.orta"));
            Assert.That(DisplayFormat.Decimal1(Fixed64.Parse("1.25")), Is.EqualTo("1,3"));
            Assert.That(DisplayFormat.Decimal1(Fixed64.Parse("-0.04")), Is.EqualTo("0,0"));
            Assert.That(DisplayFormat.Decimal1(Fixed64.FromInt(1234)), Is.EqualTo("1.234,0"));
        }

        private static OrderPreview Valid(int days, int risk) =>
            new OrderPreview(true, null, false, days, Fixed64.One, Fixed64.FromInt(2), Fixed64.One, false, risk);

        private sealed class FakeModel : IGameReadModel
        {
            public OrderPreview Next;
            public int Calls;
            public bool LastAttack;

            public event Action<long> StateChanged { add { } remove { } }

            public GameMode Mode => GameMode.Normal;

            public GameDate CurrentDate => GameDate.FromYmd(1914, 6, 28);

            public CountryId PlayerCountry => new CountryId(1);

            public long StateVersion => 1L;

            public OrderPreview PreviewOrder(ulong unitId, TargetRef target, bool attack, List<GeoPoint> path)
            {
                Calls++;
                LastAttack = attack;
                path.Clear();
                if (Next.IsValid)
                {
                    path.Add(new GeoPoint(0, 0));
                    path.Add(new GeoPoint(1_000_000, 0));
                    path.Add(new GeoPoint(2_000_000, 0));
                }

                return Next;
            }

            public void GetRegions(List<RegionView> into) { }

            public void GetRegionRings(RegionId region, List<RegionRingView> into) { }

            public RegionId FindRegion(GeoPoint point) => RegionId.None;

            public void GetCities(List<CityView> into) { }

            public bool TryGetCountry(CountryId country, out CountryView view)
            {
                view = default;
                return false;
            }

            public string CountryNameKey(CountryId country) => "ulke.1";

            public void GetCountries(List<CountryId> into) { }

            public void GetVisibleUnits(List<UnitView> into) { }

            public bool IsAtWarWith(CountryId other) => false;
        }

        private sealed class FakeOrders : IGameCommandFactory
        {
            public string Last;

            public GameCommand MoveUnit(ulong unitId, TargetRef target) => Make("yuru:" + unitId);

            public GameCommand Attack(ulong unitId, TargetRef target) => Make("saldir:" + unitId);

            public GameCommand HoldPosition(ulong unitId) => Make("bekle");

            public GameCommand RecruitInfantry(CityId city) => Make("topla");

            public GameCommand DeclareWar(CountryId target) => Make("savas");

            public GameCommand ProposeWhitePeace(CountryId target) => Make("baris");

            private GameCommand Make(string kind)
            {
                Last = kind;
                return new FakeCommand(kind);
            }
        }

        private sealed class FakeCommand : GameCommand
        {
            private readonly string _kind;

            public FakeCommand(string kind) : base(new CountryId(1)) => _kind = kind;

            public override CommandAuthority Authority => CommandAuthority.Player;

            public override string Kind => _kind;
        }
    }
}
