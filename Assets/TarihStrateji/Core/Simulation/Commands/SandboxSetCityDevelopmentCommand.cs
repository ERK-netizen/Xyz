using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary>
    /// Serbest düzenleme: şehrin gelişim seviyesini (1–10) doğrudan ayarlar. Yalnız Sandbox modunda
    /// geçerlidir; Normal modda <see cref="GameModeRules"/> reddeder (CLAUDE.md §2.6).
    /// </summary>
    public sealed class SandboxSetCityDevelopmentCommand : GameCommand
    {
        public const string KindKey = "emir.sandbox.sehirGelisimi";

        public SandboxSetCityDevelopmentCommand(CountryId issuer, CityId city, int level)
            : base(issuer)
        {
            City = city;
            Level = level;
        }

        public CityId City { get; }

        public int Level { get; }

        public override CommandAuthority Authority => CommandAuthority.Sandbox;

        public override string Kind => KindKey;
    }
}
