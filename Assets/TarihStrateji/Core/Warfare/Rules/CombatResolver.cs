using System.Collections.Generic;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Tek muharebe çözücüsü (CLAUDE.md §2.6): kara, deniz ve hava muharebeleri aynı
    /// <see cref="Power"/> ve <see cref="Resolve"/> ile çözülür.
    /// Birlik gücü = taban × güç × (½ + ½ × teşkilat oranı) × (½ + ½ × ikmal). Saldırı gücü ülke
    /// saldırı etkileriyle, savunma gücü ülke savunma etkileri × arazi × kale ile çarpılır.
    /// Muharebe şansı güce uygulanır: A' = A × zarA, D' = D × zarD, zar ∈ [1 − s, 1 + s]
    /// (deterministik PRNG); böylece günlük kuvvet oranı dalgalanır ve sonuç bir "uçurum" olmaz.
    /// Oran r = A'/(A'+D'); savunana 2 × taban hasar × r, saldırana 2 × taban hasar × (1 − r).
    /// </summary>
    public static class CombatResolver
    {
        /// <summary>Ortak güç formülü: taban × güç × (½ + ½ × teşkilat oranı) × (½ + ½ × ikmal).</summary>
        public static Fixed64 Power(Fixed64 basePower, Fixed64 strength, Fixed64 organization, Fixed64 maxOrganization, Fixed64 supply)
        {
            Fixed64 orgRatio = Fixed64.MulDiv(organization, Fixed64.One, maxOrganization);
            Fixed64 org = Fixed64.Half + (Fixed64.Half * orgRatio);
            Fixed64 sup = Fixed64.Half + (Fixed64.Half * supply);
            return basePower * strength * org * sup;
        }

        public static Fixed64 UnitPower(LandUnit unit, UnitTypeDefinition type, Fixed64 basePower)
        {
            return Power(basePower, unit.Strength(type), unit.Organization, type.MaxOrganization, unit.Supply);
        }

        /// <summary>Filo gücü (denizde ikmal tam sayılır; limandan kopukluk teşkilata yansır).</summary>
        public static Fixed64 FleetPower(Fleet fleet, ShipTypeDefinition type, Fixed64 basePower)
        {
            return Power(basePower, fleet.Strength(type), fleet.Organization, type.MaxOrganization, Fixed64.One);
        }

        public static Fixed64 AirWingPower(AirWing wing, AirWingTypeDefinition type, Fixed64 basePower)
        {
            return Power(basePower, wing.Strength(type), wing.Organization, type.MaxOrganization, Fixed64.One);
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
