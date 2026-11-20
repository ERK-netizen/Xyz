using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>
    /// Kayıt/yükle ekranındaki bir yuva (sunum içindir, Adım 20): senaryo, yuva adı, oyun tarihi, oyuncu ülkesi ve ad
    /// anahtarı, türü. Elle yuvalar <c>yuva_1..3</c>, otomatik halka <c>otomatik_*</c>, uygulama kapanış kaydı <c>son_oturum</c>.
    /// </summary>
    public sealed class SaveSlotView
    {
        public SaveSlotView(string scenarioId, string slot, GameDate date, CountryId player, string playerNameKey, SaveSlotKind kind)
        {
            ScenarioId = scenarioId ?? throw new ArgumentNullException(nameof(scenarioId));
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Date = date;
            Player = player;
            PlayerNameKey = playerNameKey ?? string.Empty;
            Kind = kind;
        }

        public string ScenarioId { get; }

        public string Slot { get; }

        public GameDate Date { get; }

        public CountryId Player { get; }

        public string PlayerNameKey { get; }

        public SaveSlotKind Kind { get; }
    }
}
