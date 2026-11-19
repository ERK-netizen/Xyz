using NUnit.Framework;
using TarihStrateji.Unity.Presentation.Input;
using UnityEngine;

namespace TarihStrateji.Tests.EditMode
{
    public class GestureMathTests
    {
        [Test]
        public void Tap_RequiresShortTimeAndSmallMovement()
        {
            Assert.That(GestureMath.IsTap(new Vector2(100, 100), new Vector2(105, 102), 0.1f, 400f, false), Is.True);
            Assert.That(GestureMath.IsTap(new Vector2(100, 100), new Vector2(160, 100), 0.1f, 400f, false), Is.False, "sürükleme");
            Assert.That(GestureMath.IsTap(new Vector2(100, 100), new Vector2(101, 100), 0.8f, 400f, false), Is.False, "uzun basma");
            Assert.That(GestureMath.IsTap(new Vector2(100, 100), new Vector2(100, 100), 0.1f, 400f, true), Is.False, "kaydırıp geri dönmek");
        }

        [Test]
        public void Slop_UsesFallbackDpiWhenUnknown()
        {
            Assert.That(GestureMath.SlopPixels(0f), Is.EqualTo(GestureMath.TapSlopInches * GestureMath.FallbackDpi).Within(1e-4f));
            Assert.That(GestureMath.SlopPixels(480f), Is.EqualTo(GestureMath.TapSlopInches * 480f).Within(1e-4f));
        }

        [Test]
        public void Pinch_SpreadingFingersZoomsIn_AndIsClamped()
        {
            Assert.That(GestureMath.PinchOrthographicSize(20f, 100f, 200f, 4f, 180f), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(GestureMath.PinchOrthographicSize(20f, 200f, 100f, 4f, 180f), Is.EqualTo(40f).Within(1e-4f));
            Assert.That(GestureMath.PinchOrthographicSize(5f, 100f, 1000f, 4f, 180f), Is.EqualTo(4f));
            Assert.That(GestureMath.PinchOrthographicSize(170f, 300f, 10f, 4f, 180f), Is.EqualTo(180f));
            Assert.That(GestureMath.PinchOrthographicSize(20f, 0f, 100f, 4f, 180f), Is.EqualTo(20f), "sıfır mesafe güvenli");
        }

        [Test]
        public void Scroll_ZoomsMultiplicatively()
        {
            Assert.That(GestureMath.ScrollOrthographicSize(20f, 1f, 4f, 180f), Is.LessThan(20f));
            Assert.That(GestureMath.ScrollOrthographicSize(20f, -1f, 4f, 180f), Is.GreaterThan(20f));
            Assert.That(GestureMath.ScrollOrthographicSize(20f, 0f, 4f, 180f), Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void WrapX_StaysInsideTheWorld()
        {
            Assert.That(GestureMath.WrapX(-1f, 100f), Is.EqualTo(99f).Within(1e-4f));
            Assert.That(GestureMath.WrapX(250f, 100f), Is.EqualTo(50f).Within(1e-4f));
            Assert.That(GestureMath.WrapX(0f, 100f), Is.EqualTo(0f));
        }

        [Test]
        public void ClampY_KeepsMapOnScreen_OrCentersSmallMaps()
        {
            Assert.That(GestureMath.ClampY(10f, 5f, 1f, -100f), Is.EqualTo(-4f).Within(1e-4f));
            Assert.That(GestureMath.ClampY(-200f, 5f, 1f, -100f), Is.EqualTo(-95f).Within(1e-4f));
            Assert.That(GestureMath.ClampY(-50f, 5f, 1f, -100f), Is.EqualTo(-50f).Within(1e-4f));
            Assert.That(GestureMath.ClampY(-3f, 80f, 1f, -27f), Is.EqualTo(-13f).Within(1e-4f), "harita ekrandan küçük: ortala");
        }
    }
}
