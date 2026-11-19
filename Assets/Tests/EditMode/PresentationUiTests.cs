using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.Map;
using TarihStrateji.Unity.Presentation.UI;
using UnityEngine;

namespace TarihStrateji.Tests.EditMode
{
    /// <summary>Metin tablosu, Türkçe biçimler, dokunmatik seçim, güvenli alan ve harita renkleri.</summary>
    public class PresentationUiTests
    {
        [Test]
        public void Localizer_ParsesKeyValueLines_AndFallsBackToKey()
        {
            Localizer text = Localizer.Parse("# yorum\narayuz.a = Birinci\r\n\narayuz.b = İkinci = metin\nbozuk satır\n");
            Assert.That(text.Count, Is.EqualTo(2));
            Assert.That(text.Get("arayuz.a"), Is.EqualTo("Birinci"));
            Assert.That(text.Get("arayuz.b"), Is.EqualTo("İkinci = metin"));
            Assert.That(text.Get("arayuz.yok"), Is.EqualTo("arayuz.yok"));
            Assert.That(text.Get(null), Is.Empty);
        }

        [Test]
        public void Localizer_LoadsTheShippedTurkishTable()
        {
            Localizer text = Localizer.LoadTurkish();
            Assert.That(text.Count, Is.GreaterThan(300));
            Assert.That(text.Get("arayuz.baslik"), Is.EqualTo("Tarih Strateji"));
            Assert.That(text.Get("arayuz.ay8"), Is.EqualTo("Ağustos"));
            Assert.That(text.Contains("ulke.1"), Is.True);
        }

        [Test]
        public void Resources_ContainMapShaderAndUiTheme()
        {
            Assert.That(Resources.Load<Shader>("Shaders/HaritaRenk"), Is.Not.Null);
            Assert.That(Resources.Load<UnityEngine.UIElements.ThemeStyleSheet>(HudController.ThemeResource), Is.Not.Null);
        }

        [TestCase(0L, "0")]
        [TestCase(999L, "999")]
        [TestCase(1000L, "1.000")]
        [TestCase(1234567L, "1.234.567")]
        [TestCase(-45000L, "-45.000")]
        [TestCase(long.MinValue, "-9.223.372.036.854.775.808")]
        public void Thousands_UsesTurkishDotSeparator(long value, string expected)
        {
            Assert.That(DisplayFormat.Thousands(value), Is.EqualTo(expected));
        }

        [Test]
        public void PercentWholeAndDate_AreTurkish()
        {
            Assert.That(DisplayFormat.Percent(Fixed64.Parse("0.455")), Is.EqualTo("%46"));
            Assert.That(DisplayFormat.Percent(Fixed64.One), Is.EqualTo("%100"));
            Assert.That(DisplayFormat.Whole(Fixed64.Parse("40000.5")), Is.EqualTo("40.001"));
            Assert.That(DisplayFormat.Whole(Fixed64.Parse("-2.5")), Is.EqualTo("-3"));
            Localizer text = Localizer.Parse("arayuz.ay1 = Ocak\narayuz.ay12 = Aralık\n");
            Assert.That(DisplayFormat.Date(GameDate.FromYmd(1925, 1, 1), text), Is.EqualTo("1 Ocak 1925"));
            Assert.That(DisplayFormat.Date(GameDate.FromYmd(1939, 12, 31), text), Is.EqualTo("31 Aralık 1939"));
        }

        [Test]
        public void Selection_TapUnitThenTarget_ThenUnitFollowsMovement()
        {
            var selection = new SelectionModel();
            int changes = 0;
            selection.Changed += () => changes++;

            selection.Tap(10);
            Assert.That(selection.Cell, Is.EqualTo(10));
            Assert.That(selection.Target, Is.EqualTo(SelectionModel.None), "birlik yokken hedef olmaz");

            selection.SelectUnit(7UL, 10);
            selection.Tap(11);
            Assert.That(selection.Target, Is.EqualTo(11));
            Assert.That(selection.Cell, Is.EqualTo(11));
            selection.Tap(10);
            Assert.That(selection.Target, Is.EqualTo(SelectionModel.None), "kendi hücresine dokunmak hedefi kaldırır");

            var units = new List<UnitView> { Own(7UL, 12) };
            selection.Reconcile(units);
            Assert.That(selection.UnitCell, Is.EqualTo(12));
            Assert.That(selection.Cell, Is.EqualTo(12), "izlenen birlikle seçim kayar");

            selection.Reconcile(new List<UnitView>());
            Assert.That(selection.HasUnit, Is.False, "yok olan birlik seçimden düşer");
            selection.Tap(-1);
            Assert.That(selection.Cell, Is.EqualTo(SelectionModel.None));
            Assert.That(changes, Is.EqualTo(selection.Version));
        }

        [Test]
        public void Selection_IgnoresForeignUnitsWithTheSameId()
        {
            var selection = new SelectionModel();
            selection.SelectUnit(7UL, 10);
            selection.Reconcile(new List<UnitView> { new UnitView(7UL, UnitViewKind.Land, new CountryId(2UL), 10, Fixed64.One, false, false) });
            Assert.That(selection.HasUnit, Is.False);
        }

        [Test]
        public void SafeArea_ConvertsNotchAndGestureBarToPanelUnits()
        {
            // 1080×2400 ekran, üstte 120 px çentik, altta 60 px hareket çubuğu; panel 1080×2400 × 0,5.
            Vector4 insets = SafeAreaMath.Insets(new Rect(0f, 60f, 1080f, 2220f), 1080f, 2400f, 540f, 1200f);
            Assert.That(insets.x, Is.EqualTo(60f).Within(1e-3f));
            Assert.That(insets.y, Is.EqualTo(30f).Within(1e-3f));
            Assert.That(insets.z, Is.EqualTo(0f));
            Assert.That(insets.w, Is.EqualTo(0f));
            Assert.That(SafeAreaMath.Insets(new Rect(0f, 0f, 0f, 0f), 0f, 0f, 1f, 1f), Is.EqualTo(Vector4.zero));
        }

        [Test]
        public void Palette_ShowsControllerFogAndOccupation()
        {
            var turkey = new CountryId(1UL);
            var greece = new CountryId(2UL);
            Color32 own = MapPalette.Cell(new CellView(2, false, turkey, turkey, CityId.None), true);
            Color32 fogged = MapPalette.Cell(new CellView(2, false, turkey, turkey, CityId.None), false);
            Color32 occupied = MapPalette.Cell(new CellView(2, false, turkey, greece, CityId.None), true);
            Color32 sea = MapPalette.Cell(new CellView(0, true, CountryId.None, CountryId.None, CityId.None), true);
            Assert.That(fogged.r + fogged.g + fogged.b, Is.LessThan(own.r + own.g + own.b), "sis karartır");
            Assert.That(occupied.b, Is.GreaterThan(own.b), "Yunan işgali mavi ton katar");
            Assert.That(sea.b, Is.GreaterThan(sea.r));
            Assert.That(MapPalette.Country(turkey), Is.Not.EqualTo(MapPalette.Country(greece)));
        }

        private static UnitView Own(ulong id, int cell) => new UnitView(id, UnitViewKind.Land, new CountryId(1UL), cell, Fixed64.One, true, true);
    }
}
