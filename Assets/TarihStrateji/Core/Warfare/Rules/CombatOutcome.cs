using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>Tek muharebe turunun sayısal sonucu (hasar: azami teşkilat oranı cinsinden).</summary>
    public readonly struct CombatOutcome
    {
        public CombatOutcome(Fixed64 attackPower, Fixed64 defensePower, Fixed64 attackerDamage, Fixed64 defenderDamage)
        {
            AttackPower = attackPower;
            DefensePower = defensePower;
            AttackerDamage = attackerDamage;
            DefenderDamage = defenderDamage;
        }

        public Fixed64 AttackPower { get; }

        public Fixed64 DefensePower { get; }

        public Fixed64 AttackerDamage { get; }

        public Fixed64 DefenderDamage { get; }

        /// <summary>Saldırı payı A / (A + D).</summary>
        public Fixed64 Ratio => (AttackPower + DefensePower).IsPositive ? Fixed64.MulDiv(AttackPower, Fixed64.One, AttackPower + DefensePower) : Fixed64.Zero;
    }
}
