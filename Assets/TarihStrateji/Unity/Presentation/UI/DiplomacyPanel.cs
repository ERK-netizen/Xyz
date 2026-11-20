using System;
using System.Collections.Generic;
using System.Text;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Diplomasi ekranı (Adım 16): üstte gelen teklifler (Kabul / Ret) ve bekleyen kendi tekliflerimiz, altta
    /// tüm ülkeler (savaşta / barışta) ve ülkeye göre eylemler: savaş ilanı, beyaz barış, saldırmazlık, ittifak.
    /// Her düğmenin emri <see cref="IGameCommandGateway.Validate"/> ile önceden doğrulanır (geçersizse pasif);
    /// tek taraflı barış yoktur, barış teklifi karşı tarafın kabulünü bekler. Kural yazmaz.
    /// </summary>
    public sealed class DiplomacyPanel
    {
        private const int TouchTarget = 112;

        private static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.12f, 0.96f);
        private static readonly Color RowColor = new Color(0.1f, 0.16f, 0.26f, 1f);
        private static readonly Color ButtonColor = new Color(0.17f, 0.27f, 0.42f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.95f, 0.92f, 1f);
        private static readonly Color WarColor = new Color(0.9f, 0.35f, 0.3f, 1f);

        private readonly IGameSession _session;
        private readonly Localizer _text;
        private readonly Func<CountryId, string> _countryName;
        private readonly Action<GameCommand> _submit;
        private readonly List<ProposalView> _proposals = new List<ProposalView>(16);
        private readonly List<CountryId> _countries = new List<CountryId>(16);
        private readonly StringBuilder _builder = new StringBuilder(128);
        private VisualElement _list;

        public DiplomacyPanel(IGameSession session, Localizer text, Func<CountryId, string> countryName, Action<GameCommand> submit)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _countryName = countryName ?? throw new ArgumentNullException(nameof(countryName));
            _submit = submit ?? throw new ArgumentNullException(nameof(submit));
        }

        public VisualElement Root { get; private set; }

        public bool IsVisible => Root != null && Root.style.display == DisplayStyle.Flex;

        /// <summary>Teklif türünün ad anahtarı.</summary>
        public static string ProposalKey(byte kind)
        {
            switch (kind)
            {
                case ProposalView.KindPeace:
                    return "arayuz.teklif.baris";
                case ProposalView.KindNonAggression:
                    return "arayuz.teklif.saldirmazlik";
                default:
                    return "arayuz.teklif.ittifak";
            }
        }

        public VisualElement Build(Action close)
        {
            Root = new VisualElement { name = "diplomasi" };
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
            title.text = _text.Get("arayuz.diplomasi");
            title.style.flexGrow = 1;
            header.Add(title);
            header.Add(MakeButton(_text.Get("arayuz.kapat"), close));
            Root.Add(header);

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
                Refresh();
            }
        }

        public void Refresh()
        {
            if (!IsVisible)
            {
                return;
            }

            IGameReadModel model = _session.ReadModel;
            IGameCommandFactory orders = _session.Orders;
            CountryId player = model.PlayerCountry;
            _list.Clear();

            _proposals.Clear();
            model.GetProposals(_proposals);
            _list.Add(Section("arayuz.teklifler"));
            if (_proposals.Count == 0)
            {
                _list.Add(Line(_text.Get("arayuz.teklifYok")));
            }

            for (int i = 0; i < _proposals.Count; i++)
            {
                ProposalView p = _proposals[i];
                VisualElement row = Row();
                bool incoming = p.To == player;
                _builder.Clear();
                _builder.Append(_countryName(incoming ? p.From : p.To)).Append(" · ").Append(_text.Get(ProposalKey(p.Kind)))
                    .Append(" · ").Append(_text.Get(incoming ? "arayuz.gelen" : "arayuz.bekliyor")).Append(" (")
                    .Append(_text.Get("arayuz.sonGun")).Append(' ').Append(DisplayFormat.Date(p.LastDay, _text)).Append(')');
                row.Add(Line(_builder.ToString()));
                if (incoming)
                {
                    ulong id = p.Id;
                    VisualElement buttons = Buttons();
                    AddOrder(buttons, "arayuz.kabul", orders.RespondProposal(id, true));
                    AddOrder(buttons, "arayuz.ret", orders.RespondProposal(id, false));
                    row.Add(buttons);
                }

                _list.Add(row);
            }

            _countries.Clear();
            model.GetCountries(_countries);
            _list.Add(Section("arayuz.ulkeler"));
            for (int i = 0; i < _countries.Count; i++)
            {
                CountryId other = _countries[i];
                if (other == player)
                {
                    continue;
                }

                bool atWar = player.IsValid && model.IsAtWarWith(other);
                VisualElement row = Row();
                Label name = Line(_countryName(other) + " · " + _text.Get(atWar ? "arayuz.savasta" : "arayuz.barista"));
                if (atWar)
                {
                    name.style.color = WarColor;
                }

                row.Add(name);
                if (player.IsValid)
                {
                    VisualElement buttons = Buttons();
                    if (atWar)
                    {
                        AddOrder(buttons, "arayuz.barisOner", orders.ProposeWhitePeace(other));
                    }
                    else
                    {
                        AddOrder(buttons, "arayuz.savasIlan", orders.DeclareWar(other));
                        AddOrder(buttons, "arayuz.saldirmazlikOner", orders.ProposeNonAggression(other));
                        AddOrder(buttons, "arayuz.ittifakOner", orders.ProposeAlliance(other));
                    }

                    row.Add(buttons);
                }

                _list.Add(row);
            }
        }

        private void AddOrder(VisualElement parent, string key, GameCommand command)
        {
            Button button = MakeButton(_text.Get(key), () =>
            {
                _submit(command);
                Refresh();
            });
            button.SetEnabled(_session.Commands.Validate(command).IsValid);
            parent.Add(button);
        }

        private Label Section(string key)
        {
            Label label = MakeLabel(34, true);
            label.text = _text.Get(key);
            label.style.marginTop = 14;
            label.style.marginBottom = 6;
            return label;
        }

        private static Label Line(string text)
        {
            Label label = MakeLabel(30, false);
            label.text = text;
            return label;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.backgroundColor = RowColor;
            row.style.marginBottom = 8;
            row.style.paddingLeft = 14;
            row.style.paddingRight = 14;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.borderTopLeftRadius = 12;
            row.style.borderTopRightRadius = 12;
            row.style.borderBottomLeftRadius = 12;
            row.style.borderBottomRightRadius = 12;
            return row;
        }

        private static VisualElement Buttons()
        {
            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexWrap = Wrap.Wrap;
            buttons.style.marginTop = 6;
            return buttons;
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

        private static Button MakeButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.minHeight = TouchTarget;
            button.style.fontSize = 30;
            button.style.color = TextColor;
            button.style.backgroundColor = ButtonColor;
            button.style.marginRight = 8;
            button.style.marginTop = 4;
            button.style.paddingLeft = 20;
            button.style.paddingRight = 20;
            button.style.borderTopLeftRadius = 10;
            button.style.borderTopRightRadius = 10;
            button.style.borderBottomLeftRadius = 10;
            button.style.borderBottomRightRadius = 10;
            return button;
        }
    }
}
