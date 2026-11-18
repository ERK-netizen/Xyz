using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary>
    /// Serbest düzenleme: hücre altyapısını doğrudan ayarlar. Yalnız Sandbox modunda geçerlidir;
    /// Normal modda <see cref="GameModeRules"/> reddeder (CLAUDE.md §2.6).
    /// </summary>
    public sealed class SandboxSetInfrastructureCommand : GameCommand
    {
        public const string KindKey = "emir.sandbox.altyapiAyarla";

        public SandboxSetInfrastructureCommand(CountryId issuer, HexCoord hex, HexInfrastructure infrastructure)
            : base(issuer)
        {
            Hex = hex;
            Infrastructure = infrastructure;
        }

        public HexCoord Hex { get; }

        public HexInfrastructure Infrastructure { get; }

        public override CommandAuthority Authority => CommandAuthority.Sandbox;

        public override string Kind => KindKey;
    }
}
