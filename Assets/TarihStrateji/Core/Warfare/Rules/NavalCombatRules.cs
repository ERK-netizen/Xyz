using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Deniz muharebesi (P13): birbirine düşman filoların aynı deniz hücresinde bulunması çatışmadır.
    /// Taraf A, hücredeki en küçük kimlikli filonun sahibidir; B, A'ya düşman filolardır. Güç =
    /// Σ filo saldırısı × ülke etkisi + ortak destek (deniz taarruzu kanatları); tek
    /// <see cref="CombatResolver"/> çözer. Nakliye filoları güce katkı yapmaz ama hasar alır
    /// (konvoy baskını). Teşkilatı kırılan filo komşu güvenli deniz hücresine çekilir, gücü
    /// eşiğin altına düşen veya çekilecek yeri olmayan filo batar.
    /// </summary>
    public static class NavalCombatRules
    {
        public const string BattleNote = "bilgi.deniz.muharebe";

        /// <summary>Çatışmalı deniz hücreleri, ilk filonun kimlik sırasıyla.</summary>
        public static void ContestedHexes(WarfareContext ctx, List<HexId> into)
        {
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (!into.Contains(f.Hex) && ctx.World.Cell(f.Hex).IsWater && NavalQueries.HasHostileFleets(ctx, f.Hex, f.Owner))
                {
                    into.Add(f.Hex);
                }
            }
        }

        public static void ResolveBattle(WarfareContext ctx, HexId hex, ref RngStream rng, List<string> notes)
        {
            var sideA = new List<Fleet>();
            var sideB = new List<Fleet>();
            CountryId a = CountryId.None;
            for (int i = 0; i < ctx.Warfare.Fleets.Count; i++)
            {
                Fleet f = ctx.Warfare.Fleets.ValueAt(i);
                if (f.Hex != hex)
                {
                    continue;
                }

                if (!a.IsValid)
                {
                    a = f.Owner;
                }

                if (f.Owner == a)
                {
                    sideA.Add(f);
                }
                else if (UnitQueries.IsHostile(ctx, a, f.Owner))
                {
                    sideB.Add(f);
                }
            }

            if (sideA.Count == 0 || sideB.Count == 0)
            {
                return;
            }

            HexCell cell = ctx.World.Cell(hex);
            CountryId b = sideB[0].Owner;
            Fixed64 powerA = (SidePower(ctx, sideA) * CombatRules.CountryModifier(ctx, a, WarfareStatKeys.NavalAttack)) + JointSupportRules.Support(ctx, cell, a);
            Fixed64 powerB = (SidePower(ctx, sideB) * CombatRules.CountryModifier(ctx, b, WarfareStatKeys.NavalAttack)) + JointSupportRules.Support(ctx, cell, b);
            CombatOutcome outcome = CombatResolver.Resolve(powerA, powerB, ctx.Catalog.Parameters, ref rng);
            notes.Add(BattleNote + " @ " + hex.Value.ToString(CultureInfo.InvariantCulture));
            Damage(ctx, sideA, outcome.AttackerDamage);
            Damage(ctx, sideB, outcome.DefenderDamage);
            Settle(ctx, sideA, hex, notes);
            Settle(ctx, sideB, hex, notes);
        }

        private static Fixed64 SidePower(WarfareContext ctx, List<Fleet> fleets)
        {
            Fixed64 sum = Fixed64.Zero;
            for (int i = 0; i < fleets.Count; i++)
            {
                ShipTypeDefinition type = ctx.Catalog.ShipTypes[fleets[i].Type];
                sum += CombatResolver.FleetPower(fleets[i], type, type.Attack);
            }

            return sum;
        }

        private static void Damage(WarfareContext ctx, List<Fleet> fleets, Fixed64 damage)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            for (int i = 0; i < fleets.Count; i++)
            {
                Fleet f = fleets[i];
                ShipTypeDefinition type = ctx.Catalog.ShipTypes[f.Type];
                f.Organization = Fixed64.Max(Fixed64.Zero, f.Organization - (type.MaxOrganization * damage));
                long manpower = f.Manpower;
                Fixed64 equipment = f.Equipment;
                CasualtyRules.ApplyLoss(ctx, f.HomeRegion, type.Crew, ref manpower, ref equipment, damage * prm.StrengthLossPerDamage);
                f.Manpower = manpower;
                f.Equipment = equipment;
            }
        }

        private static void Settle(WarfareContext ctx, List<Fleet> fleets, HexId hex, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            for (int i = 0; i < fleets.Count; i++)
            {
                Fleet f = fleets[i];
                if (!ctx.Warfare.Fleets.ContainsKey(f.Id))
                {
                    continue;
                }

                ShipTypeDefinition type = ctx.Catalog.ShipTypes[f.Type];
                if (f.Strength(type) < prm.DestroyedBelowStrength)
                {
                    FleetRules.Sink(ctx, f, notes);
                }
                else if (f.Organization < type.MaxOrganization * prm.RetreatOrganization)
                {
                    FleetRules.Withdraw(ctx, f, hex, notes);
                }
            }
        }
    }
}
