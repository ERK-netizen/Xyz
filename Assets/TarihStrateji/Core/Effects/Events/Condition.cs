using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>Tek koşul: tür + kimlik/gün parametresi + sayısal eşik.</summary>
    public readonly struct Condition
    {
        public Condition(ConditionKind kind, ulong id, Fixed64 value)
        {
            Kind = kind;
            Id = id;
            Value = value;
        }

        public ConditionKind Kind { get; }

        public ulong Id { get; }

        public Fixed64 Value { get; }
    }
}
