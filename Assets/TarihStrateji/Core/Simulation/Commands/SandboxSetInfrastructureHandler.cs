using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary><see cref="SandboxSetInfrastructureCommand"/> kuralları ve kayıt biçimi.</summary>
    public sealed class SandboxSetInfrastructureHandler : CommandHandler<SandboxSetInfrastructureCommand>
    {
        public override string Kind => SandboxSetInfrastructureCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, SandboxSetInfrastructureCommand command)
        {
            return state.World.CheckInfrastructure(command.Hex, command.Infrastructure);
        }

        protected override void ApplyTyped(CommandContext context, SandboxSetInfrastructureCommand command)
        {
            context.State.World.SetInfrastructure(command.Hex, command.Infrastructure);
        }

        protected override void WriteTyped(SandboxSetInfrastructureCommand command, CanonicalWriter writer)
        {
            writer.WriteInt32(command.Hex.Column);
            writer.WriteInt32(command.Hex.Row);
            writer.WriteByte((byte)command.Infrastructure.RoadLevel);
            writer.WriteByte((byte)command.Infrastructure.RailLevel);
            writer.WriteBool(command.Infrastructure.HasPort);
        }

        protected override SandboxSetInfrastructureCommand ReadTyped(CountryId issuer, CanonicalReader reader)
        {
            var hex = new HexCoord(reader.ReadInt32(), reader.ReadInt32());
            var infrastructure = new HexInfrastructure(reader.ReadByte(), reader.ReadByte(), reader.ReadBool());
            return new SandboxSetInfrastructureCommand(issuer, hex, infrastructure);
        }
    }
}
