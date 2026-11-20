using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary><see cref="SandboxSetCityDevelopmentCommand"/> kuralları ve kayıt biçimi.</summary>
    public sealed class SandboxSetCityDevelopmentHandler : CommandHandler<SandboxSetCityDevelopmentCommand>
    {
        public override string Kind => SandboxSetCityDevelopmentCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, SandboxSetCityDevelopmentCommand command)
        {
            if (!state.World.TryGetCity(command.City, out _))
            {
                return ValidationResult.Fail(GeographyErrorKeys.SehirBulunamadi);
            }

            return command.Level < City.MinDevelopment || command.Level > City.MaxDevelopment
                ? ValidationResult.Fail(GeographyErrorKeys.SehirGelisimGecersiz)
                : ValidationResult.Ok;
        }

        protected override void ApplyTyped(CommandContext context, SandboxSetCityDevelopmentCommand command)
        {
            context.State.World.TryGetCity(command.City, out City city);
            city.DevelopmentLevel = command.Level;
        }

        protected override void WriteTyped(SandboxSetCityDevelopmentCommand command, CanonicalWriter writer)
        {
            writer.WriteId(command.City);
            writer.WriteInt32(command.Level);
        }

        protected override SandboxSetCityDevelopmentCommand ReadTyped(CountryId issuer, CanonicalReader reader) =>
            new SandboxSetCityDevelopmentCommand(issuer, new CityId(reader.ReadUInt64()), reader.ReadInt32());
    }
}
