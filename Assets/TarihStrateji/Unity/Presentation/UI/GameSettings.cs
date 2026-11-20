using UnityEngine;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Oyuncu ayarları (Adım 20): yazı boyutu, ekranı açık tutma, kare hızı, şehir adları. Yalnız sunum tercihidir (kural
    /// etkisi yok, kayda girmez); cihazda <see cref="PlayerPrefs"/>'te <c>ayar.*</c> anahtarlarıyla tutulur. Okunan
    /// değerler geçerli aralığa sıkıştırılır.
    /// </summary>
    public sealed class GameSettings
    {
        public const string TextSizeKey = "ayar.yaziBoyu";
        public const string KeepScreenOnKey = "ayar.ekranAcik";
        public const string FrameRateKey = "ayar.kareHizi";
        public const string CityNamesKey = "ayar.sehirAdlari";

        /// <summary>0 küçük, 1 normal, 2 büyük.</summary>
        public int TextSize { get; set; } = 1;

        public bool KeepScreenOn { get; set; } = true;

        /// <summary>30 ya da 60.</summary>
        public int FrameRate { get; set; } = 60;

        public bool CityNames { get; set; } = true;

        /// <summary>Arayüz ölçeği (PanelSettings.scale).</summary>
        public float PanelScale => TextSize <= 0 ? 0.9f : TextSize >= 2 ? 1.15f : 1f;

        public static GameSettings Load()
        {
            return Sanitized(PlayerPrefs.GetInt(TextSizeKey, 1), PlayerPrefs.GetInt(KeepScreenOnKey, 1), PlayerPrefs.GetInt(FrameRateKey, 60),
                PlayerPrefs.GetInt(CityNamesKey, 1));
        }

        /// <summary>Ham tamsayı değerlerden geçerli ayar (bozuk/eski değerler sıkıştırılır).</summary>
        public static GameSettings Sanitized(int textSize, int keepScreenOn, int frameRate, int cityNames)
        {
            return new GameSettings
            {
                TextSize = textSize < 0 ? 0 : textSize > 2 ? 2 : textSize,
                KeepScreenOn = keepScreenOn != 0,
                FrameRate = frameRate <= 30 ? 30 : 60,
                CityNames = cityNames != 0,
            };
        }

        public void Save()
        {
            PlayerPrefs.SetInt(TextSizeKey, TextSize);
            PlayerPrefs.SetInt(KeepScreenOnKey, KeepScreenOn ? 1 : 0);
            PlayerPrefs.SetInt(FrameRateKey, FrameRate);
            PlayerPrefs.SetInt(CityNamesKey, CityNames ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Uygulama genelindeki ayarları uygular (kare hızı, ekran uykusu); panel ölçeği ve yazılar HUD'dadır.</summary>
        public void ApplyApplication()
        {
            Application.targetFrameRate = FrameRate;
            Screen.sleepTimeout = KeepScreenOn ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }
    }
}
