using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Yeni oyun ekranı (Adım 18): önce senaryo (başlık + başlangıç tarihi), sonra o senaryonun ülkeleri; ülkeye dokunmak
    /// <see cref="IGameSession.StartNewGame"/> ile oyunu kurar (çalışan oyunun yerine geçer; uyarı metni gösterilir).
    /// Varsayılan oyuncu vurgulanır. Kural yazmaz; sonuç Türkçe bildirim olarak döner.
    /// </summary>
    public sealed class NewGamePanel
    {
        private const int TouchTarget = 112;

        private static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.12f, 0.96f);
        private static readonly Color ButtonColor = new Color(0.17f, 0.27f, 0.42f, 1f);
        private static readonly Color DefaultColor = new Color(0.85f, 0.62f, 0.15f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.95f, 0.92f, 1f);

        private readonly IGameSession _session;
        private readonly Localizer _text;
        private readonly Action<string> _notify;
        private readonly List<ScenarioView> _scenarios = new List<ScenarioView>(4);
        private VisualElement _list;
        private Label _hint;
        private Action _close;

        public NewGamePanel(IGameSession session, Localizer text, Action<string> notify)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _notify = notify ?? throw new ArgumentNullException(nameof(notify));
        }

        public VisualElement Root { get; private set; }

        public bool IsVisible => Root != null && Root.style.display == DisplayStyle.Flex;

        public VisualElement Build(Action close)
        {
            _close = close;
            Root = new VisualElement { name = "yeniOyun" };
            Root.style.flexGrow = 1;
            Root.style.backgroundColor = PanelColor;
            Root.style.paddingLeft = 24;
            Root.style.paddingRight = 24;
            Root.style.paddingTop = 16;
            Root.style.paddingBottom = 16;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            Label title = MakeLabel(40, true);
            title.text = _text.Get("arayuz.yeniOyun");
            title.style.flexGrow = 1;
            header.Add(title);
            header.Add(MakeButton(_text.Get("arayuz.kapat"), close, false));
            Root.Add(header);

            _hint = MakeLabel(28, false);
            _hint.text = _text.Get("arayuz.yeniOyunUyari");
            _hint.style.marginTop = 8;
            Root.Add(_hint);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.marginTop = 12;
            _list = scroll.contentContainer;
            Root.Add(scroll);
            Root.style.display = DisplayStyle.None;
            return Root;
        }

        public void Show(bool visible)
        {
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                ShowScenarios();
            }
        }

        private void ShowScenarios()
        {
            _list.Clear();
            _scenarios.Clear();
            _session.GetScenarios(_scenarios);
            for (int i = 0; i < _scenarios.Count; i++)
            {
                ScenarioView s = _scenarios[i];
                bool current = string.Equals(s.Id, _session.CurrentScenario, StringComparison.Ordinal);
                string label = _text.Get(s.TitleKey) + " · " + DisplayFormat.Date(s.Date, _text) + (current ? " (" + _text.Get("arayuz.oynanan") + ")" : string.Empty);
                _list.Add(MakeButton(label, () => ShowCountries(s), false));
            }
        }

        private void ShowCountries(ScenarioView scenario)
        {
            _list.Clear();
            Label heading = MakeLabel(34, true);
            heading.text = _text.Get(scenario.TitleKey) + " — " + _text.Get("arayuz.ulkeSec");
            heading.style.marginBottom = 8;
            _list.Add(heading);
            for (int i = 0; i < scenario.Countries.Count; i++)
            {
                CountryId country = scenario.Countries[i];
                bool isDefault = country == scenario.DefaultPlayer;
                _list.Add(MakeButton(_text.Get(scenario.CountryNameKeys[i]), () => Start(scenario, country), isDefault));
            }

            _list.Add(MakeButton(_text.Get("arayuz.geri"), ShowScenarios, false));
        }

        private void Start(ScenarioView scenario, CountryId country)
        {
            ValidationResult started = _session.StartNewGame(scenario.Id, country);
            _notify(started.IsValid ? "arayuz.yeniOyunKuruldu" : started.ErrorKey);
            if (started.IsValid)
            {
                _close?.Invoke();
            }
        }

        private static Label MakeLabel(int size, bool bold)
        {
            var label = new Label();
            label.style.fontSize = size;
            label.style.color = TextColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            if (bold)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            return label;
        }

        private static Button MakeButton(string text, Action onClick, bool highlight)
        {
            var button = new Button(onClick) { text = text };
            button.style.minHeight = TouchTarget;
            button.style.fontSize = 30;
            button.style.color = TextColor;
            button.style.backgroundColor = highlight ? DefaultColor : ButtonColor;
            button.style.marginTop = 6;
            button.style.paddingLeft = 20;
            button.style.paddingRight = 20;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.borderTopLeftRadius = 10;
            button.style.borderTopRightRadius = 10;
            button.style.borderBottomLeftRadius = 10;
            button.style.borderBottomRightRadius = 10;
            return button;
        }
    }
}
