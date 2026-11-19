using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary>Savaş ilanı; <see cref="Goals"/> hedef ülkenin talep edilen bölgeleri (boş olabilir).</summary>
    public sealed class DeclareWarCommand : GameCommand
    {
        public const string KindKey = "emir.diplomasi.savasIlani";

        private readonly RegionId[] _goals;

        public DeclareWarCommand(CountryId issuer, CountryId target, RegionId[] goals)
            : base(issuer)
        {
            Target = target;
            _goals = goals ?? new RegionId[0];
        }

        public CountryId Target { get; }

        public IReadOnlyList<RegionId> Goals => _goals;

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
