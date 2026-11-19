using System.Collections.Generic;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Tek muharebe çözücüsü (CLAUDE.md §2.6; deniz ve hava Adım 7'de aynı çözücüyü kullanır).
    /// Birlik gücü = taban × güç × (½ + ½ × teşkilat oranı) × (½ + ½ × ikmal). Saldırı gücü ülke
    /// saldırı etkileriyle, savunma gücü ülke savunma etkileri × arazi × kale ile çarpılır.
    /// Muharebe şansı güce uygulanır: A' = A × zarA, D' = D × zarD, zar ∈ [1 − s, 1 + s]
    /// (deterministik PRNG); böylece günlük kuvvet oranı dalgalanır ve sonuç bir "uçurum" olmaz.
    /// Oran r = A'/(A'+D'); savunana 2 × taban hasar × r, saldırana 2 × taban hasar × (1 − r).
    /// </summary>
    public static class CombatResolver
    {
        public static Fixed64 UnitPower(LandUnit unit, UnitTypeDefinition type, Fixed64 basePower)
        {
            Fixed64 orgRatio = Fixed64.MulDiv(unit.Organization, Fixed64.One, type.MaxOrganization);
            Fixed64 org = Fixed64.Half + (Fixed64.Half * orgRatio);
            Fixed64 supply = Fixed64.Half + (Fixed64.Half * unit.Supply);
            return basePower * unit.Strength(type) * org * supply;
        }

        public static CombatOutcome Resolve(
            Fixed64 attackPower,
            Fixed64 defensePower,
            WarfareParameters parameters,
            ref RngStream rng)
        {
            Fixed64 attack = attackPower * Roll(parameters, ref rng);
            Fixed64 defense = defensePower * Roll(parameters, ref rng);
            Fixed64 total = attack + defense;
            Fixed64 ratio = total.IsPositive ? Fixed64.MulDiv(attack, Fixed64.One, total) : Fixed64.One;
            Fixed64 twice = parameters.BaseDamage * 2;
            return new CombatOutcome(attack, defense, twice * (Fixed64.One - ratio), twice * ratio);
        }

        public static Fixed64 SidePower(WarfareCatalog catalog, IReadOnlyList<LandUnit> units, bool attack)
        {
            Fixed64 sum = Fixed64.Zero;
            for (int i = 0; i < units.Count; i++)
            {
                UnitTypeDefinition type = catalog.UnitTypes[units[i].Type];
                sum += UnitPower(units[i], type, attack ? type.Attack : type.Defense);
            }

            return sum;
        }

        private static Fixed64 Roll(WarfareParameters parameters, ref RngStream rng)
        {
            Fixed64 spread = parameters.RandomSpread;
            return Fixed64.One - spread + (spread * 2 * rng.NextFraction());
        }
    }
}
