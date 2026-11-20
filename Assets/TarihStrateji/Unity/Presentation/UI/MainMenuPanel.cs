using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Ana menü (Adım 20): uygulama açılışında ve HUD'daki "Menü" düğmesiyle açılır; oyun bu sırada duraklatılır (HUD).
    /// Sayfalar: ana liste (Devam et, Yeni oyun, Kaydet / Yükle, Ayarlar, Hakkında), <see cref="SaveLoadPanel"/>,
    /// <see cref="SettingsPanel"/> ve Hakkında (sürüm, harita verisinin lisansı ve atfı — CShapes 2.0, CC BY-NC-SA 4.0).
    /// </summary>
    public sealed class MainMenuPanel
    {
        private readonly IGameSession _session;
        private readonly Localizer _text;
        private readonly SaveLoadPanel _saves;
        private readonly SettingsPanel _settings;
        private readonly List<ScenarioView> _scenarios = new List<ScenarioView>(4);
        private VisualElement _home;
        private VisualElement _about;
        private Label _current;

        public MainMenuPanel(IGameSession session, Localizer text, Action<string> notify, GameSettings settings, Action settingsChanged)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _saves = new SaveLoadPanel(session, text, notify);
            _settings = new SettingsPanel(settings, text, settingsChanged);
        }

        public VisualElement Root { get; private set; }

        public bool IsVisible => Root != null && Root.style.display == DisplayStyle.Flex;

        /// <param name="resume">Devam et: menü kapanır, oyun kaldığı hızla sürer.</param>
        /// <param name="newGame">Yeni oyun ekranını açar (menü kapanır).</param>
        public VisualElement Build(Action resume, Action newGame)
        {
            Root = new VisualElement { name = "anaMenu" };
            Root.style.flexGrow = 1;
            Root.style.alignSelf = Align.Stretch;

            _home = PanelKit.Screen("anaMenuListe", out VisualElement body);
            Label title = PanelKit.Text(_text.Get("arayuz.oyunAdi"), 56, true);
            title.style.color = PanelKit.AccentColor;
            title.style.marginTop = 24;
            body.Add(title);
            _current = PanelKit.Text(string.Empty, 30, false);
            _current.style.color = PanelKit.MutedColor;
            _current.style.marginBottom = 16;
            body.Add(_current);
            body.Add(PanelKit.Button(_text.Get("arayuz.devamEt"), resume, PanelKit.AccentColor));
            body.Add(PanelKit.Button(_text.Get("arayuz.yeniOyun"), newGame, PanelKit.ButtonColor));
            body.Add(PanelKit.Button(_text.Get("arayuz.kayitYukle"), () => Page(_saves.Root), PanelKit.ButtonColor));
            body.Add(PanelKit.Button(_text.Get("arayuz.ayarlar"), () => Page(_settings.Root), PanelKit.ButtonColor));
            body.Add(PanelKit.Button(_text.Get("arayuz.hakkinda"), () => Page(_about), PanelKit.ButtonColor));
            Root.Add(_home);

            Root.Add(_saves.Build(() => Page(_home)));
            Root.Add(_settings.Build(() => Page(_home)));

            _about = PanelKit.Screen("hakkinda", out VisualElement about);
            _about.Insert(0, PanelKit.Header(_text.Get("arayuz.hakkinda"), _text.Get("arayuz.geri"), () => Page(_home)));
            about.Add(PanelKit.Text(_text.Get("arayuz.oyunAdi") + " " + Application.version, 34, true));
            about.Add(Paragraph("arayuz.hakkinda.oyun"));
            about.Add(Paragraph("arayuz.hakkinda.harita"));
            about.Add(Paragraph("arayuz.hakkinda.lisans"));
            about.Add(Paragraph("arayuz.hakkinda.motor"));
            Root.Add(_about);

            Root.style.display = DisplayStyle.None;
            return Root;
        }

        public void Show(bool visible)
        {
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                _current.text = CurrentGame();
                Page(_home);
            }
        }

        private Label Paragraph(string key)
        {
            Label label = PanelKit.Text(_text.Get(key), 28, false);
            label.style.marginTop = 14;
            return label;
        }

        private void Page(VisualElement page)
        {
            _home.style.display = page == _home ? DisplayStyle.Flex : DisplayStyle.None;
            _about.style.display = page == _about ? DisplayStyle.Flex : DisplayStyle.None;
            _saves.Show(page == _saves.Root);
            _settings.Show(page == _settings.Root);
        }

        /// <summary>"1914 — … · 3 Ağustos 1914 · Osmanlı İmparatorluğu".</summary>
        private string CurrentGame()
        {
            _scenarios.Clear();
            _session.GetScenarios(_scenarios);
            string title = _session.CurrentScenario;
            for (int i = 0; i < _scenarios.Count; i++)
            {
                if (string.Equals(_scenarios[i].Id, _session.CurrentScenario, StringComparison.Ordinal))
                {
                    title = _text.Get(_scenarios[i].TitleKey);
                }
            }

            IGameReadModel model = _session.ReadModel;
            string player = model.PlayerCountry.IsValid ? " · " + _text.Get(model.CountryNameKey(model.PlayerCountry)) : string.Empty;
            return title + " · " + DisplayFormat.Date(model.CurrentDate, _text) + player;
        }
    }
}
