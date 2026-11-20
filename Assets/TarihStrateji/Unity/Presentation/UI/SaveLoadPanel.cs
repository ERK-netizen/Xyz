using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Views;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Kayıt ve yükleme sayfası (Adım 20): çalışan oyunu üç elle yuvadan birine yazar; tüm senaryoların yuvalarını
    /// (elle, otomatik, son oturum) tarih ve ülkeyle listeler, yükler; elle yuvayı iki dokunuşla (onaylı) siler.
    /// Dosya işlemleri <see cref="IGameSession"/> üzerinden yapılır; yükleme oyunun yerine geçer ve sunum yeniden kurulur.
    /// </summary>
    public sealed class SaveLoadPanel
    {
        private readonly IGameSession _session;
        private readonly Localizer _text;
        private readonly Action<string> _notify;
        private readonly List<SaveSlotView> _slots = new List<SaveSlotView>(16);
        private readonly List<ScenarioView> _scenarios = new List<ScenarioView>(4);
        private VisualElement _body;
        private string _pendingDelete = string.Empty;

        public SaveLoadPanel(IGameSession session, Localizer text, Action<string> notify)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _notify = notify ?? throw new ArgumentNullException(nameof(notify));
        }

        public VisualElement Root { get; private set; }

        public VisualElement Build(Action back)
        {
            Root = PanelKit.Screen("kayitlar", out VisualElement body);
            Root.Insert(0, PanelKit.Header(_text.Get("arayuz.kayitYukle"), _text.Get("arayuz.geri"), back));
            _body = body;
            return Root;
        }

        public void Show(bool visible)
        {
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _pendingDelete = string.Empty;
            if (visible)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            _body.Clear();
            _slots.Clear();
            _session.GetSaveSlots(_slots);
            _scenarios.Clear();
            _session.GetScenarios(_scenarios);

            _body.Add(Section("arayuz.kaydet"));
            string current = _session.CurrentScenario;
            for (int i = 0; i < 3; i++)
            {
                string slot = "yuva_" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                SaveSlotView existing = Find(current, slot);
                string label = SlotName(slot, SaveSlotKind.Manual) + " · " + (existing == null ? _text.Get("arayuz.bos") : Describe(existing));
                _body.Add(PanelKit.Button(label, () => Save(slot), PanelKit.ButtonColor));
            }

            _body.Add(Section("arayuz.yukle"));
            if (_slots.Count == 0)
            {
                Label none = PanelKit.Text(_text.Get("arayuz.kayitYok"), 30, false);
                none.style.color = PanelKit.MutedColor;
                _body.Add(none);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                SaveSlotView s = _slots[i];
                var row = PanelKit.Row();
                row.style.flexWrap = Wrap.NoWrap;
                Button load = PanelKit.Button(ScenarioTitle(s.ScenarioId) + " · " + SlotName(s.Slot, s.Kind) + "\n" + Describe(s),
                    () => Load(s), PanelKit.ButtonColor);
                load.style.flexGrow = 1;
                row.Add(load);
                if (s.Kind == SaveSlotKind.Manual)
                {
                    string key = s.ScenarioId + "/" + s.Slot;
                    bool confirm = string.Equals(_pendingDelete, key, StringComparison.Ordinal);
                    Button delete = PanelKit.Button(_text.Get(confirm ? "arayuz.silOnay" : "arayuz.sil"), () => Delete(s, key), PanelKit.DangerColor);
                    delete.style.marginLeft = 8;
                    row.Add(delete);
                }

                _body.Add(row);
            }
        }

        private Label Section(string key)
        {
            Label label = PanelKit.Text(_text.Get(key), 34, true);
            label.style.marginTop = 16;
            return label;
        }

        private SaveSlotView Find(string scenario, string slot)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (string.Equals(_slots[i].ScenarioId, scenario, StringComparison.Ordinal) && string.Equals(_slots[i].Slot, slot, StringComparison.Ordinal))
                {
                    return _slots[i];
                }
            }

            return null;
        }

        private string Describe(SaveSlotView s) =>
            DisplayFormat.Date(s.Date, _text) + (s.PlayerNameKey.Length == 0 ? string.Empty : " · " + _text.Get(s.PlayerNameKey));

        private string SlotName(string slot, SaveSlotKind kind)
        {
            switch (kind)
            {
                case SaveSlotKind.Autosave:
                    return _text.Get("arayuz.otomatikKayit");
                case SaveSlotKind.LastSession:
                    return _text.Get("arayuz.sonOturum");
                default:
                    return _text.Get("arayuz.yuva") + " " + slot.Substring(slot.Length - 1);
            }
        }

        private string ScenarioTitle(string id)
        {
            for (int i = 0; i < _scenarios.Count; i++)
            {
                if (string.Equals(_scenarios[i].Id, id, StringComparison.Ordinal))
                {
                    return _text.Get(_scenarios[i].TitleKey);
                }
            }

            return id;
        }

        private void Save(string slot)
        {
            ValidationResult saved = _session.SaveGame(slot);
            _notify(saved.IsValid ? "arayuz.kaydedildi" : saved.ErrorKey);
            _pendingDelete = string.Empty;
            Refresh();
        }

        private void Load(SaveSlotView slot)
        {
            ValidationResult loaded = _session.LoadGame(slot.ScenarioId, slot.Slot);
            if (!loaded.IsValid)
            {
                _notify(loaded.ErrorKey);
            }
        }

        private void Delete(SaveSlotView slot, string key)
        {
            if (!string.Equals(_pendingDelete, key, StringComparison.Ordinal))
            {
                _pendingDelete = key;
                Refresh();
                return;
            }

            ValidationResult deleted = _session.DeleteSave(slot.ScenarioId, slot.Slot);
            _notify(deleted.IsValid ? "arayuz.silindi" : deleted.ErrorKey);
            _pendingDelete = string.Empty;
            Refresh();
        }
    }
}
