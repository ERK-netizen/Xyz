using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Hava üstünlüğü muharebesi (P13'ün ilk alt adımı): aynı hücreyi hedefleyen birbirine düşman
    /// kanatlar hava saldırı güçleriyle tek <see cref="CombatResolver"/> üzerinden çarpışır. Teşkilatı
    /// kırılan kanat görevi bırakıp üsse döner; gücü eşiğin altına düşen kanat kaybedilir. Sonraki
    /// deniz/kara muharebelerinde destek, kalan güçlerle hesaplanan üstünlük payıyla ölçeklenir.
    /// </summary>
    public static class AirCombatRules
    {
        public const string BattleNote = "bilgi.hava.muharebe";

        public static void ContestedHexes(WarfareContext ctx, List<HexId> into)
        {
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (w.Mission == AirMission.None || into.Contains(w.MissionTarget))
                {
                    continue;
                }

                for (int j = 0; j < ctx.Warfare.AirWings.Count; j++)
                {
                    AirWing v = ctx.Warfare.AirWings.ValueAt(j);
                    if (v.Mission != AirMission.None && v.MissionTarget == w.MissionTarget && UnitQueries.IsHostile(ctx, w.Owner, v.Owner))
                    {
                        into.Add(w.MissionTarget);
                        break;
                    }
                }
            }
        }

        public static void ResolveBattle(WarfareContext ctx, HexId hex, ref RngStream rng, List<string> notes)
        {
            var sideA = new List<AirWing>();
            var sideB = new List<AirWing>();
            CountryId a = CountryId.None;
            for (int i = 0; i < ctx.Warfare.AirWings.Count; i++)
            {
                AirWing w = ctx.Warfare.AirWings.ValueAt(i);
                if (w.Mission == AirMission.None || w.MissionTarget != hex)
                {
                    continue;
                }

                if (!a.IsValid)
                {
                    a = w.Owner;
                }

                if (w.Owner == a)
                {
                    sideA.Add(w);
                }
                else if (UnitQueries.IsHostile(ctx, a, w.Owner))
                {
                    sideB.Add(w);
                }
            }

            if (sideA.Count == 0 || sideB.Count == 0)
            {
                return;
            }

            Fixed64 powerA = SidePower(ctx, sideA) * CombatRules.CountryModifier(ctx, a, WarfareStatKeys.AirAttack);
            Fixed64 powerB = SidePower(ctx, sideB) * CombatRules.CountryModifier(ctx, sideB[0].Owner, WarfareStatKeys.AirAttack);
            CombatOutcome outcome = CombatResolver.Resolve(powerA, powerB, ctx.Catalog.Parameters, ref rng);
            notes.Add(BattleNote + " @ " + hex.Value.ToString(CultureInfo.InvariantCulture));
            Apply(ctx, sideA, outcome.AttackerDamage, notes);
            Apply(ctx, sideB, outcome.DefenderDamage, notes);
        }

        private static Fixed64 SidePower(WarfareContext ctx, List<AirWing> wings)
        {
            Fixed64 sum = Fixed64.Zero;
            for (int i = 0; i < wings.Count; i++)
            {
                AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[wings[i].Type];
                sum += CombatResolver.AirWingPower(wings[i], type, type.AirAttack);
            }

            return sum;
        }

        private static void Apply(WarfareContext ctx, List<AirWing> wings, Fixed64 damage, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            for (int i = 0; i < wings.Count; i++)
            {
                AirWing w = wings[i];
                AirWingTypeDefinition type = ctx.Catalog.AirWingTypes[w.Type];
                w.Organization = Fixed64.Max(Fixed64.Zero, w.Organization - (type.MaxOrganization * damage));
                long manpower = w.Manpower;
                Fixed64 equipment = w.Equipment;
                CasualtyRules.ApplyLoss(ctx, w.HomeRegion, type.Crew, ref manpower, ref equipment, damage * prm.StrengthLossPerDamage);
                w.Manpower = manpower;
                w.Equipment = equipment;
                if (w.Strength(type) < prm.DestroyedBelowStrength)
                {
                    AirRules.Remove(ctx, w, false);
                    notes.Add(AirRules.LostNote + " @ " + w.Id.Value.ToString(CultureInfo.InvariantCulture));
                }
                else if (w.Organization < type.MaxOrganization * prm.RetreatOrganization)
                {
                    AirRules.SetMission(w, AirMission.None, HexId.None);
                }
            }
        }
    }
}
