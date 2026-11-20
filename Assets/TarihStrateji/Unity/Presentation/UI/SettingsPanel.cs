using System;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Ayarlar sayfası (Adım 20): yazı boyutu, ekranı açık tutma, kare hızı, şehir adları. Her seçim hemen kaydedilir ve
    /// <c>changed</c> ile HUD'a bildirilir (ölçek, yazı katmanı, uygulama ayarları uygulanır).
    /// </summary>
    public sealed class SettingsPanel
    {
        private readonly GameSettings _settings;
        private readonly Localizer _text;
        private readonly Action _changed;
        private VisualElement _body;

        public SettingsPanel(GameSettings settings, Localizer text, Action changed)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _changed = changed ?? throw new ArgumentNullException(nameof(changed));
        }

        public VisualElement Root { get; private set; }

        public VisualElement Build(Action back)
        {
            Root = PanelKit.Screen("ayarlar", out VisualElement body);
            Root.Insert(0, PanelKit.Header(_text.Get("arayuz.ayarlar"), _text.Get("arayuz.geri"), back));
            _body = body;
            return Root;
        }

        public void Show(bool visible)
        {
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            _body.Clear();
            Choice("arayuz.ayar.yaziBoyu", new[] { "arayuz.ayar.kucuk", "arayuz.ayar.normal", "arayuz.ayar.buyuk" }, _settings.TextSize, v => _settings.TextSize = v);
            Choice("arayuz.ayar.ekranAcik", new[] { "arayuz.ayar.acik", "arayuz.ayar.kapali" }, _settings.KeepScreenOn ? 0 : 1, v => _settings.KeepScreenOn = v == 0);
            Choice("arayuz.ayar.kareHizi", new[] { "arayuz.ayar.fps30", "arayuz.ayar.fps60" }, _settings.FrameRate <= 30 ? 0 : 1, v => _settings.FrameRate = v == 0 ? 30 : 60);
            Choice("arayuz.ayar.sehirAdlari", new[] { "arayuz.ayar.goster", "arayuz.ayar.gizle" }, _settings.CityNames ? 0 : 1, v => _settings.CityNames = v == 0);
        }

        private void Choice(string titleKey, string[] optionKeys, int selected, Action<int> set)
        {
            Label title = PanelKit.Text(_text.Get(titleKey), 32, true);
            title.style.marginTop = 16;
            _body.Add(title);
            VisualElement row = PanelKit.Row();
            for (int i = 0; i < optionKeys.Length; i++)
            {
                int value = i;
                Button option = PanelKit.Button(_text.Get(optionKeys[i]), () =>
                {
                    set(value);
                    _settings.Save();
                    _changed();
                    Refresh();
                }, i == selected ? PanelKit.AccentColor : PanelKit.ButtonColor);
                option.style.marginRight = 8;
                option.style.flexGrow = 1;
                row.Add(option);
            }

            _body.Add(row);
        }
    }
}
