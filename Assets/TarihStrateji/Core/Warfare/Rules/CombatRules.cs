using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Saldırının çözümü. Hedef komşu hücrede düşman birlikleri varsa muharebe; savunanların
    /// hepsi geri çekilme eşiğinin altına düşerse veya yok olursa saldırgan kazanır, savunanlar
    /// komşu güvenli hücreye çekilir (yoksa kuşatılmış sayılıp yok olur), hedef şehir değilse
    /// saldırgan ilerler. Hedef birliksiz düşman şehriyse garnizona karşı bir sektör alınmaya
    /// çalışılır: giriş yuvası saldırı yönünden, merkez en son (kale sektörleri ve kuşatma etkiler).
    /// </summary>
    public static class CombatRules
    {
        public const string BattleNote = "bilgi.savas.muharebe";
        public const string SectorCapturedNote = "bilgi.savas.sektorAlindi";
        public const string CityCapturedNote = "bilgi.savas.sehirAlindi";
        public const string UnitDestroyedNote = "bilgi.savas.birlikYokOldu";

        public static ValidationResult CheckAttack(WarfareContext ctx, CountryId country, LandUnitId unitId, HexCoord target)
        {
            ValidationResult owned = UnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (!ctx.World.Grid.Contains(target) || !IsAdjacent(ctx, unit.Hex, ctx.World.Grid.ToId(target), out _))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefKomsuDegil);
            }

            UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
            if (unit.Organization < type.MaxOrganization * ctx.Catalog.Parameters.MinimumAttackOrganization)
            {
                return ValidationResult.Fail(WarfareErrorKeys.MoralYetersiz);
            }

            HexCell cell = ctx.World.Cell(target);
            if (UnitQueries.HasHostileUnits(ctx, cell.Id, country))
            {
                return ValidationResult.Ok;
            }

            if (cell.City.IsValid && UnitQueries.IsHostile(ctx, country, cell.Controller))
            {
                return ValidationResult.Ok;
            }

            if (cell.City.IsValid && HasHostileSector(ctx, cell.City, country))
            {
                return ValidationResult.Ok;
            }

            return cell.Controller.IsValid && cell.Controller != country && !UnitQueries.IsHostile(ctx, country, cell.Controller)
                ? ValidationResult.Fail(WarfareErrorKeys.SavasYok)
                : ValidationResult.Fail(WarfareErrorKeys.HedefBos);
        }

        public static ValidationResult CheckImmediateAttack(WarfareContext ctx, CountryId country, LandUnitId unitId, HexCoord target)
        {
            ValidationResult check = CheckAttack(ctx, country, unitId, target);
            if (!check.IsValid)
            {
                return check;
            }

            LandUnit unit = ctx.Warfare.Units[unitId];
            return unit.ActionPoints < ctx.Catalog.UnitTypes[unit.Type].AttackActionCost ? ValidationResult.Fail(WarfareErrorKeys.ApYetersiz) : ValidationResult.Ok;
        }

        /// <summary>
        /// Gün içi anında saldırı: AP düşülür, çözüm kimliği birliğe yazılır (P13 o gün atlar) ve
        /// saldırı çözülür. Çağıran <see cref="CheckImmediateAttack"/>'i önceden çağırmıştır.
        /// </summary>
        public static void ResolveImmediate(WarfareContext ctx, LandUnit attacker, HexCoord target, ResolutionId resolution, ref RngStream rng, List<string> notes)
        {
            attacker.ActionPoints -= ctx.Catalog.UnitTypes[attacker.Type].AttackActionCost;
            attacker.LastResolution = resolution;
            Resolve(ctx, new List<LandUnit> { attacker }, target, ref rng, notes);
        }

        /// <summary>
        /// Aynı hedefe saldıran birlikler (aynı ülke, hepsi <see cref="CheckAttack"/>'ten geçmiş)
        /// tek muharebede birleşir: güçler toplanır, hasar oranı her birliğe uygulanır. Böylece
        /// kuvvet üstünlüğü hasar sıklığına değil kuvvet oranına yansır.
        /// </summary>
        public static void Resolve(WarfareContext ctx, List<LandUnit> attackers, HexCoord target, ref RngStream rng, List<string> notes)
        {
            LandUnit attacker = attackers[0];
            HexCell cell = ctx.World.Cell(target);
            var defenders = new List<LandUnit>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Hex == cell.Id && UnitQueries.IsHostile(ctx, attacker.Owner, u.Owner))
                {
                    defenders.Add(u);
                }
            }

            if (defenders.Count > 0)
            {
                Battle(ctx, attackers, cell, defenders, ref rng, notes);
            }
            else if (cell.City.IsValid)
            {
                AssaultCity(ctx, attackers, cell, ref rng, notes);
            }
        }

        private static void Battle(WarfareContext ctx, List<LandUnit> attackers, HexCell cell, List<LandUnit> defenders, ref RngStream rng, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            LandUnit attacker = attackers[0];
            CountryId defenderCountry = defenders[0].Owner;
            Fixed64 attack = CombatResolver.SidePower(ctx.Catalog, attackers, true) * CountryModifier(ctx, attacker.Owner, WarfareStatKeys.LandAttack);
            Fixed64 defense = CombatResolver.SidePower(ctx.Catalog, defenders, false) * CountryModifier(ctx, defenderCountry, WarfareStatKeys.LandDefense)
                * prm.TerrainDefense(cell.Terrain) * FortressFactor(ctx, cell, defenderCountry);
            CombatOutcome outcome = CombatResolver.Resolve(attack, defense, prm, ref rng);

            for (int i = 0; i < attackers.Count; i++)
            {
                ApplyDamage(ctx, attackers[i], outcome.AttackerDamage);
            }

            bool attackerWins = true;
            for (int i = 0; i < defenders.Count; i++)
            {
                LandUnit d = defenders[i];
                ApplyDamage(ctx, d, outcome.DefenderDamage);
                UnitTypeDefinition type = ctx.Catalog.UnitTypes[d.Type];
                bool broken = d.Organization < type.MaxOrganization * prm.RetreatOrganization || d.Strength(type) < prm.DestroyedBelowStrength;
                attackerWins &= broken;
            }

            notes.Add(BattleNote + " @ " + attacker.Id.Value.ToString(CultureInfo.InvariantCulture) + ":" + (attackerWins ? "zafer" : "puskurtuldu"));
            if (!attackerWins)
            {
                return;
            }

            for (int i = 0; i < defenders.Count; i++)
            {
                Retreat(ctx, defenders[i], attacker.Hex, notes);
            }

            if (!cell.City.IsValid && UnitQueries.CanEnter(ctx, attacker.Owner, cell))
            {
                for (int i = 0; i < attackers.Count; i++)
                {
                    MovementRules.ClearOrders(attackers[i]);
                    attackers[i].Hex = cell.Id;
                }
            }
        }

        private static void AssaultCity(WarfareContext ctx, List<LandUnit> attackers, HexCell cell, ref RngStream rng, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            LandUnit attacker = attackers[0];
            City city = ctx.World.Cities[cell.City];
            CountryId holder = cell.Controller;
            Fixed64 attack = CombatResolver.SidePower(ctx.Catalog, attackers, true) * CountryModifier(ctx, attacker.Owner, WarfareStatKeys.LandAttack);
            Fixed64 garrison = prm.GarrisonDefense * FortressFactor(ctx, cell, holder) * (ctx.Warfare.IsBesieged(city.Id) ? prm.BesiegedGarrisonFactor : Fixed64.One);
            CombatOutcome outcome = CombatResolver.Resolve(attack, garrison, prm, ref rng);
            for (int i = 0; i < attackers.Count; i++)
            {
                ApplyDamage(ctx, attackers[i], outcome.AttackerDamage);
            }

            if (!rng.Chance(outcome.Ratio))
            {
                notes.Add(BattleNote + " @ " + attacker.Id.Value.ToString(CultureInfo.InvariantCulture) + ":garnizonDirendi");
                return;
            }

            IsAdjacent(ctx, cell.Id, attacker.Hex, out HexDirection side);
            var remaining = new List<SectorSlot>();
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                CitySector s = ctx.World.Sectors[city.Sectors[i]];
                if (s.Controller != attacker.Owner)
                {
                    remaining.Add(s.Slot);
                }
            }

            SectorSlot slot = CityLayout.EntrySlotFrom(side, remaining);
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                CitySector s = ctx.World.Sectors[city.Sectors[i]];
                if (s.Slot == slot)
                {
                    ctx.World.SetSectorController(s.Id, attacker.Owner);
                    notes.Add(SectorCapturedNote + " @ " + s.Id.Value.ToString(CultureInfo.InvariantCulture));
                    break;
                }
            }

            if (ctx.World.Cell(city.Hex).Controller == attacker.Owner)
            {
                for (int i = 0; i < attackers.Count; i++)
                {
                    MovementRules.ClearOrders(attackers[i]);
                    attackers[i].Hex = cell.Id;
                }

                notes.Add(CityCapturedNote + " @ " + city.Id.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void ApplyDamage(WarfareContext ctx, LandUnit unit, Fixed64 damage)
        {
            UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
            unit.Organization = Fixed64.Max(Fixed64.Zero, unit.Organization - (type.MaxOrganization * damage));
            CasualtyRules.ApplyStrengthLoss(ctx, unit, type, damage * ctx.Catalog.Parameters.StrengthLossPerDamage);
        }

        /// <summary>Komşu güvenli hücreye (yön sırasıyla ilk uygun) çekilir; yoksa birlik yok olur.</summary>
        private static void Retreat(WarfareContext ctx, LandUnit unit, HexId attackerHex, List<string> notes)
        {
            MovementRules.ClearOrders(unit);
            HexCoord from = ctx.World.Grid.FromId(unit.Hex);
            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (!ctx.World.Grid.TryGetNeighbor(from, (HexDirection)d, out HexCoord n))
                {
                    continue;
                }

                HexCell cell = ctx.World.Cell(n);
                if (cell.Id == attackerHex || cell.IsWater || UnitQueries.HasHostileUnits(ctx, cell.Id, unit.Owner))
                {
                    continue;
                }

                if (cell.Controller == unit.Owner || (cell.Controller.IsValid && !UnitQueries.IsHostile(ctx, unit.Owner, cell.Controller)))
                {
                    unit.Hex = cell.Id;
                    return;
                }
            }

            CasualtyRules.RecordDeaths(ctx, unit, unit.Manpower);
            unit.Manpower = 0L;
            UnitRules.Remove(ctx, unit, false);
            notes.Add(UnitDestroyedNote + " @ " + unit.Id.Value.ToString(CultureInfo.InvariantCulture));
        }

        public static Fixed64 CountryModifier(WarfareContext ctx, CountryId country, string stat)
        {
            var targets = new[] { EffectTarget.Global, EffectTarget.Of(country) };
            return Fixed64.Max(Fixed64.Zero, ctx.Effects.Evaluate(targets, stat, Fixed64.One, ctx.Today));
        }

        /// <summary>1 + kale bonusu × savunanın kontrolündeki kale sektörü sayısı.</summary>
        public static Fixed64 FortressFactor(WarfareContext ctx, HexCell cell, CountryId defender)
        {
            Fixed64 factor = Fixed64.One;
            if (!cell.City.IsValid)
            {
                return factor;
            }

            City city = ctx.World.Cities[cell.City];
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                CitySector s = ctx.World.Sectors[city.Sectors[i]];
                if (s.Kind == SectorKind.Fortress && s.Controller == defender)
                {
                    factor += ctx.Catalog.Parameters.FortressBonusPerSector;
                }
            }

            return factor;
        }

        private static bool HasHostileSector(WarfareContext ctx, CityId cityId, CountryId country)
        {
            City city = ctx.World.Cities[cityId];
            for (int i = 0; i < city.Sectors.Count; i++)
            {
                if (UnitQueries.IsHostile(ctx, country, ctx.World.Sectors[city.Sectors[i]].Controller))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary><paramref name="from"/> → <paramref name="to"/> komşu mu; öyleyse yön.</summary>
        public static bool IsAdjacent(WarfareContext ctx, HexId from, HexId to, out HexDirection direction)
        {
            HexCoord a = ctx.World.Grid.FromId(from);
            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (ctx.World.Grid.TryGetNeighbor(a, (HexDirection)d, out HexCoord n) && ctx.World.Grid.ToId(n) == to)
                {
                    direction = (HexDirection)d;
                    return true;
                }
            }

            direction = HexDirection.East;
            return false;
        }
    }
}
