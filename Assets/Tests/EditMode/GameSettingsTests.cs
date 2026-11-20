using NUnit.Framework;
using TarihStrateji.Unity.Presentation.UI;

namespace TarihStrateji.Tests.EditMode
{
    /// <summary>Oyuncu ayarlarının (Adım 20) ham değerlerden sıkıştırılması ve arayüz ölçeği.</summary>
    public class GameSettingsTests
    {
        [Test]
        public void Sanitized_ClampsOutOfRangeValues()
        {
            GameSettings s = GameSettings.Sanitized(7, 0, 144, 0);
            Assert.That(s.TextSize, Is.EqualTo(2));
            Assert.That(s.KeepScreenOn, Is.False);
            Assert.That(s.FrameRate, Is.EqualTo(60));
            Assert.That(s.CityNames, Is.False);

            GameSettings low = GameSettings.Sanitized(-3, 5, 12, 1);
            Assert.That(low.TextSize, Is.EqualTo(0));
            Assert.That(low.KeepScreenOn, Is.True);
            Assert.That(low.FrameRate, Is.EqualTo(30));
            Assert.That(low.CityNames, Is.True);
        }

        [Test]
        public void PanelScale_FollowsTextSize()
        {
            Assert.That(GameSettings.Sanitized(0, 1, 60, 1).PanelScale, Is.LessThan(1f));
            Assert.That(GameSettings.Sanitized(1, 1, 60, 1).PanelScale, Is.EqualTo(1f));
            Assert.That(GameSettings.Sanitized(2, 1, 60, 1).PanelScale, Is.GreaterThan(1f));
        }
    }
}
