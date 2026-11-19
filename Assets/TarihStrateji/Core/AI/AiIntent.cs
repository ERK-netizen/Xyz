using System.Collections.Generic;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.AI
{
    /// <summary>
    /// YZ kararı (saf veri). Alanlar türe göre anlam taşır: <see cref="Id"/> teknoloji/odak/olay/
    /// teklif/birlik kimliği, <see cref="Country"/> muhatap ülke, <see cref="Target"/> hücre,
    /// <see cref="Option"/> olay seçeneği, <see cref="Transfers"/> barış maddeleri.
    /// </summary>
    public readonly struct AiIntent
    {
        private static readonly RegionTransfer[] NoTransfers = new RegionTransfer[0];

        private AiIntent(AiIntentKind kind, ulong id, CountryId country, HexCoord target, int option, RegionTransfer[] transfers, ulong secondary = 0UL, Fixed64 amount = default)
        {
            Secondary = secondary;
            Amount = amount;
            Kind = kind;
            Id = id;
            Country = country;
            Target = target;
            Option = option;
            TransferArray = transfers ?? NoTransfers;
        }

        public AiIntentKind Kind { get; }

        public ulong Id { get; }

        public CountryId Country { get; }

        public HexCoord Target { get; }

        public int Option { get; }

        /// <summary>İkinci kimlik (inşaatta sektör).</summary>
        public ulong Secondary { get; }

        /// <summary>Sayısal değer (vergi oranı).</summary>
        public Fixed64 Amount { get; }

        private RegionTransfer[] TransferArray { get; }

        public IReadOnlyList<RegionTransfer> Transfers => TransferArray;

        public static AiIntent Research(TechnologyId tech) => new AiIntent(AiIntentKind.Research, tech.Value, CountryId.None, default, 0, null);

        public static AiIntent Focus(FocusId focus) => new AiIntent(AiIntentKind.SelectFocus, focus.Value, CountryId.None, default, 0, null);

        public static AiIntent Event(EventId eventId, int option) => new AiIntent(AiIntentKind.ChooseEventOption, eventId.Value, CountryId.None, default, option, null);

        public static AiIntent Respond(ProposalId proposal, bool accept) =>
            new AiIntent(accept ? AiIntentKind.AcceptProposal : AiIntentKind.RejectProposal, proposal.Value, CountryId.None, default, 0, null);

        public static AiIntent Peace(CountryId with, RegionTransfer[] transfers) => new AiIntent(AiIntentKind.ProposePeace, 0, with, default, 0, transfers);

        public static AiIntent Alliance(CountryId with) => new AiIntent(AiIntentKind.ProposeAlliance, 0, with, default, 0, null);

        public static AiIntent War(CountryId target) => new AiIntent(AiIntentKind.DeclareWar, 0, target, default, 0, null);

        public static AiIntent Recruit(UnitTypeId type, HexCoord at) => new AiIntent(AiIntentKind.Recruit, type.Value, CountryId.None, at, 0, null);

        public static AiIntent Move(LandUnitId unit, HexCoord to) => new AiIntent(AiIntentKind.Move, unit.Value, CountryId.None, to, 0, null);

        public static AiIntent Tax(Fixed64 rate) => new AiIntent(AiIntentKind.SetTaxRate, 0, CountryId.None, default, 0, null, 0UL, rate);

        public static AiIntent Build(FacilityTypeId type, SectorId sector) => new AiIntent(AiIntentKind.Build, type.Value, CountryId.None, default, 0, null, sector.Value);

        public static AiIntent Attack(LandUnitId unit, HexCoord target) => new AiIntent(AiIntentKind.Attack, unit.Value, CountryId.None, target, 0, null);
    }
}
