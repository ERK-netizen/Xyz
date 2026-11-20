using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Tam ekran menü panellerinin ortak öğeleri (Adım 20): koyu arka plan, ≥ 112 px dokunma hedefli düğmeler, başlık
    /// satırı ve yazılar. Yalnız görünüm üretir; kural ve durum taşımaz.
    /// </summary>
    public static class PanelKit
    {
        public const int TouchTarget = 112;

        public static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.12f, 0.97f);
        public static readonly Color ButtonColor = new Color(0.17f, 0.27f, 0.42f, 1f);
        public static readonly Color AccentColor = new Color(0.85f, 0.62f, 0.15f, 1f);
        public static readonly Color DangerColor = new Color(0.6f, 0.18f, 0.16f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.95f, 0.92f, 1f);
        public static readonly Color MutedColor = new Color(0.7f, 0.74f, 0.8f, 1f);

        /// <summary>Tam ekran, kaydırılabilir gövdeli panel; <paramref name="body"/> içerik kabıdır.</summary>
        public static VisualElement Screen(string name, out VisualElement body)
        {
            var root = new VisualElement { name = name };
            root.style.flexGrow = 1;
            root.style.alignSelf = Align.Stretch;
            root.style.backgroundColor = PanelColor;
            root.style.paddingLeft = 24;
            root.style.paddingRight = 24;
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            body = scroll.contentContainer;
            root.Add(scroll);
            root.style.display = DisplayStyle.None;
            return root;
        }

        /// <summary>Başlık ve sağda tek düğme (Geri/Kapat).</summary>
        public static VisualElement Header(string title, string buttonText, Action onButton)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 8;
            Label label = Text(title, 40, true);
            label.style.flexGrow = 1;
            header.Add(label);
            if (onButton != null)
            {
                Button back = Button(buttonText, onButton, ButtonColor);
                back.style.marginTop = 0;
                header.Add(back);
            }

            return header;
        }

        public static Label Text(string text, int size, bool bold)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = TextColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            if (bold)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            return label;
        }

        public static Button Button(string text, Action onClick, Color color)
        {
            var button = new Button(onClick) { text = text };
            button.style.minHeight = TouchTarget;
            button.style.fontSize = 32;
            button.style.color = TextColor;
            button.style.backgroundColor = color;
            button.style.marginTop = 8;
            button.style.paddingLeft = 24;
            button.style.paddingRight = 24;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.borderTopLeftRadius = 12;
            button.style.borderTopRightRadius = 12;
            button.style.borderBottomLeftRadius = 12;
            button.style.borderBottomRightRadius = 12;
            return button;
        }

        public static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexWrap = Wrap.Wrap;
            return row;
        }
    }
}
