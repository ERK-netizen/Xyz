using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Doğrudan hedefli kara muharebesi (§5.1, §5.5, MAP-G06/G07, WAR-L01…L10). Kendi birliğini seç →
    /// düşman birliği veya düşman kontrolündeki şehri hedefle. Hedef düğüm, saldıranın düğümü veya ona tek
    /// kara kenarıyla bağlı (girilebilir) düğüm olmalıdır; kenar ortasındaki birlik saldıramaz. Aynı hedef
    /// düğüme saldıran tüm birlikler tek muharebede birleşir; tek <see cref="CombatResolver"/> kullanılır:
    /// savunma = birlikler × ülke etkisi × arazi (yaklaşma kenarının arazisi) × şehir tahkimatı. Kırılan
    /// savunan ulaşım grafında güvenli komşu düğüme çekilir (saldıranın düğümü hariç); yoksa yok olur.
    /// Savunansız düşman şehrine garnizon muharebesi: garnizon × tahkimat × (kuşatmada çarpan); şehir
    /// alınırsa yalnız <c>Controller</c> değişir (CITY-005, REG-030). Anında saldırı AP harcar, çözüm
    /// kimliği alır ve o gün günlük fazda tekrar çözülmez (REG-016).
    /// </summary>
    public static class GeoCombatRules
    {
        public static ValidationResult CheckAttack(GeoWarfareContext ctx, CountryId country, LandUnitId unitId, TargetRef target, out TransportNodeId targetNode)
        {
            targetNode = TransportNodeId.None;
            ValidationResult owned = GeoUnitRules.CheckOwned(ctx, country, unitId, out LandUnit unit);
            if (!owned.IsValid)
            {
                return owned;
            }

            if (unit.IsEmbarked)
            {
                return ValidationResult.Fail(WarfareErrorKeys.BirlikGemide);
            }

            if (unit.RouteProgress > 0L)
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefKomsuDegil);
            }

            UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
            if (unit.Organization < type.MaxOrganization * ctx.Catalog.Parameters.MinimumAttackOrganization)
            {
                return ValidationResult.Fail(WarfareErrorKeys.MoralYetersiz);
            }

            CountryId enemy;
            switch (target.Kind)
            {
                case TargetKind.LandUnit:
                    if (!ctx.Warfare.Units.TryGetValue(new LandUnitId(target.EntityId), out LandUnit other) || other.IsEmbarked || other.RouteProgress > 0L)
                    {
                        return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
                    }

                    enemy = other.Owner;
                    targetNode = other.Node;
                    break;
                case TargetKind.City:
                    if (!ctx.World.TryGetCity(new CityId(target.EntityId), out City city))
                    {
                        return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
                    }

                    enemy = city.Controller;
                    targetNode = city.Node;
                    break;
                default:
                    return ValidationResult.Fail(WarfareErrorKeys.HedefGecersiz);
            }

            if (!ctx.IsHostile(country, enemy))
            {
                return ValidationResult.Fail(WarfareErrorKeys.SavasYok);
            }

            if (targetNode != unit.Node && !TryApproachEdge(ctx, unit, targetNode, out _))
            {
                return ValidationResult.Fail(WarfareErrorKeys.HedefKomsuDegil);
            }

            return ValidationResult.Ok;
        }

        public static ValidationResult CheckImmediateAttack(GeoWarfareContext ctx, CountryId country, LandUnitId unitId, TargetRef target)
        {
            ValidationResult check = CheckAttack(ctx, country, unitId, target, out _);
            if (!check.IsValid)
            {
                return check;
            }

            LandUnit unit = ctx.Warfare.Units[unitId];
            return unit.ActionPoints >= ctx.Catalog.UnitTypes[unit.Type].AttackActionCost
                ? ValidationResult.Ok
                : ValidationResult.Fail(WarfareErrorKeys.ApYetersiz);
        }

        /// <summary>Günlük saldırı emri (P13'te çözülür); rota iptal olur.</summary>
        public static void SetAttack(LandUnit unit, TargetRef target)
        {
            GeoMovementRules.Stop(unit);
            unit.GeoAttackTarget = target;
        }

        /// <summary>Anında saldırı: AP harcar, çözüm kimliği yazar, tek başına çözülür.</summary>
        public static void ResolveImmediate(GeoWarfareContext ctx, LandUnit attacker, TargetRef target, ResolutionId resolution, ref RngStream rng, List<string> notes)
        {
            CheckAttack(ctx, attacker.Owner, attacker.Id, target, out TransportNodeId node);
            attacker.ActionPoints -= ctx.Catalog.UnitTypes[attacker.Type].AttackActionCost;
            attacker.LastResolution = resolution;
            ResolveAt(ctx, new List<LandUnit> { attacker }, node, Fixed64.One, ref rng, notes);
        }

        /// <summary>
        /// P13: geçerli saldırı emri olan birlikler hedef düğüme göre gruplanır (düğüm kimliği sırasıyla) ve
        /// her grup tek muharebede çözülür. Bugün anında çözülmüş birlikler atlanır. Rastgelelik anahtarı:
        /// (Combat, gün, hedef düğüm).
        /// </summary>
        public static void ResolveDay(GeoWarfareContext ctx, ulong worldSeed, ICollection<ResolutionId> resolvedToday, List<string> notes)
        {
            var groups = new SortedDictionary<ulong, List<LandUnit>>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit unit = ctx.Warfare.Units.ValueAt(i);
                if (!unit.GeoAttackTarget.IsValid || (unit.LastResolution.IsValid && resolvedToday.Contains(unit.LastResolution)))
                {
                    continue;
                }

                if (!CheckAttack(ctx, unit.Owner, unit.Id, unit.GeoAttackTarget, out TransportNodeId node).IsValid)
                {
                    unit.GeoAttackTarget = TargetRef.None;
                    continue;
                }

                if (!groups.TryGetValue(node.Value, out List<LandUnit> list))
                {
                    list = new List<LandUnit>();
                    groups.Add(node.Value, list);
                }

                list.Add(unit);
            }

            foreach (KeyValuePair<ulong, List<LandUnit>> group in groups)
            {
                var rng = new RngStream(worldSeed, RngSubsystem.Combat, ctx.Today.DayNumber, group.Key, 0UL);
                ResolveAt(ctx, group.Value, new TransportNodeId(group.Key), Fixed64.One, ref rng, notes);
            }
        }

        public static Fixed64 CountryModifier(GeoWarfareContext ctx, CountryId country, string stat)
        {
            var targets = new[] { EffectTarget.Global, EffectTarget.Of(country) };
            return Fixed64.Max(Fixed64.Zero, ctx.Effects.Evaluate(targets, stat, Fixed64.One, ctx.Today));
        }

        /// <summary>Saldıranın düğümünden hedef düğüme kara kenarı (kenar kimliği sırasıyla ilk girilebilir).</summary>
        public static bool TryApproachEdge(GeoWarfareContext ctx, LandUnit unit, TransportNodeId target, out TransportEdge approach)
        {
            var access = new LandAccessCostModel(ctx, unit.Owner);
            IReadOnlyList<TransportEdge> edges = ctx.World.Transport.EdgesOf(unit.Node);
            ctx.World.Transport.TryGetNode(unit.Node, out TransportNode from);
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].Other(unit.Node) != target)
                {
                    continue;
                }

                ctx.World.Transport.TryGetNode(target, out TransportNode to);
                if (access.TryGetCost(edges[i], from, to, out _))
                {
                    approach = edges[i];
                    return true;
                }
            }

            approach = null;
            return false;
        }

        /// <summary>Muharebe çözümü; çıkarmada <paramref name="attackFactor"/> = çıkarma çarpanı.</summary>
        internal static void ResolveAt(GeoWarfareContext ctx, List<LandUnit> attackers, TransportNodeId node, Fixed64 attackFactor, ref RngStream rng, List<string> notes)
        {
            LandUnit lead = attackers[0];
            var defenders = new List<LandUnit>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Node == node && u.RouteProgress == 0L && !u.IsEmbarked && ctx.IsHostile(lead.Owner, u.Owner))
                {
                    defenders.Add(u);
                }
            }

            City city = GeoUnitQueries.CityAt(ctx, node);
            for (int i = 0; i < attackers.Count; i++)
            {
                attackers[i].GeoAttackTarget = TargetRef.None;
            }

            if (defenders.Count > 0)
            {
                Battle(ctx, attackers, node, city, defenders, attackFactor, ref rng, notes);
            }
            else if (city != null && ctx.IsHostile(lead.Owner, city.Controller))
            {
                Assault(ctx, attackers, node, city, attackFactor, ref rng, notes);
            }
        }

        private static void Battle(GeoWarfareContext ctx, List<LandUnit> attackers, TransportNodeId node, City city, List<LandUnit> defenders, Fixed64 attackFactor, ref RngStream rng, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            LandUnit lead = attackers[0];
            CountryId defender = defenders[0].Owner;
            Fixed64 attack = (CombatResolver.SidePower(ctx.Catalog, attackers, true) * CountryModifier(ctx, lead.Owner, WarfareStatKeys.LandAttack) * attackFactor)
                + GeoJointSupportRules.Support(ctx, node, lead.Owner);
            Fixed64 defense = CombatResolver.SidePower(ctx.Catalog, defenders, false) * CountryModifier(ctx, defender, WarfareStatKeys.LandDefense)
                * prm.TerrainDefense(ApproachTerrain(ctx, lead, node)) * Fortification(city, defender)
                + GeoJointSupportRules.Support(ctx, node, defender);
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
                attackerWins &= d.Organization < type.MaxOrganization * prm.RetreatOrganization || d.Strength(type) < prm.DestroyedBelowStrength;
            }

            notes.Add(CombatRules.BattleNote + " @ " + lead.Id.Value.ToString(CultureInfo.InvariantCulture) + ":" + (attackerWins ? "zafer" : "puskurtuldu"));
            if (!attackerWins)
            {
                return;
            }

            for (int i = 0; i < defenders.Count; i++)
            {
                Retreat(ctx, defenders[i], lead.Node, notes);
            }

            if (city == null || !ctx.IsHostile(lead.Owner, city.Controller))
            {
                Advance(attackers, node);
            }
        }

        private static void Assault(GeoWarfareContext ctx, List<LandUnit> attackers, TransportNodeId node, City city, Fixed64 attackFactor, ref RngStream rng, List<string> notes)
        {
            WarfareParameters prm = ctx.Catalog.Parameters;
            LandUnit lead = attackers[0];
            Fixed64 attack = (CombatResolver.SidePower(ctx.Catalog, attackers, true) * CountryModifier(ctx, lead.Owner, WarfareStatKeys.LandAttack) * attackFactor)
                + GeoJointSupportRules.Support(ctx, node, lead.Owner);
            Fixed64 garrison = prm.GarrisonDefense * Fortification(city, city.Controller)
                * (city.Siege == SiegeState.Besieged ? prm.BesiegedGarrisonFactor : Fixed64.One)
                + GeoJointSupportRules.Support(ctx, node, city.Controller);
            CombatOutcome outcome = CombatResolver.Resolve(attack, garrison, prm, ref rng);
            for (int i = 0; i < attackers.Count; i++)
            {
                ApplyDamage(ctx, attackers[i], outcome.AttackerDamage);
            }

            if (!rng.Chance(outcome.Ratio))
            {
                notes.Add(CombatRules.BattleNote + " @ " + lead.Id.Value.ToString(CultureInfo.InvariantCulture) + ":garnizonDirendi");
                return;
            }

            city.Controller = lead.Owner;
            city.Siege = SiegeState.None;
            Advance(attackers, node);
            notes.Add(CombatRules.CityCapturedNote + " @ " + city.Id.Value.ToString(CultureInfo.InvariantCulture));
        }

        private static Fixed64 Fortification(City city, CountryId defender) =>
            city != null && city.Controller == defender && city.Fortification > Fixed64.One ? city.Fortification : Fixed64.One;

        private static TerrainType ApproachTerrain(GeoWarfareContext ctx, LandUnit lead, TransportNodeId node)
        {
            if (lead.Node != node && TryApproachEdge(ctx, lead, node, out TransportEdge edge) && edge.Terrain <= (byte)TerrainType.Tundra)
            {
                return (TerrainType)edge.Terrain;
            }

            return TerrainType.Plains;
        }

        private static void Advance(List<LandUnit> attackers, TransportNodeId node)
        {
            for (int i = 0; i < attackers.Count; i++)
            {
                attackers[i].Node = node;
                attackers[i].Carrier = FleetId.None;
                attackers[i].RouteProgress = 0L;
                attackers[i].RouteList.Clear();
            }
        }

        private static void ApplyDamage(GeoWarfareContext ctx, LandUnit unit, Fixed64 damage)
        {
            UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
            unit.Organization = Fixed64.Max(Fixed64.Zero, unit.Organization - (type.MaxOrganization * damage));
            CasualtyRules.ApplyStrengthLoss(ctx.Population, unit, type, damage * ctx.Catalog.Parameters.StrengthLossPerDamage);
        }

        /// <summary>
        /// Kenar kimliği sırasıyla ilk güvenli komşu düğüme çekilir: kara kenarı, saldıranın düğümü değil,
        /// düşman birliği yok, bölgesi kendinde veya tarafsız-dost elde. Koridor yoksa birlik yok olur
        /// (kuşatılmış birlik rastgele dost şehre ışınlanmaz, §5.4).
        /// </summary>
        private static void Retreat(GeoWarfareContext ctx, LandUnit unit, TransportNodeId attackerNode, List<string> notes)
        {
            GeoMovementRules.Stop(unit);
            IReadOnlyList<TransportEdge> edges = ctx.World.Transport.EdgesOf(unit.Node);
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].IsNaval || !edges[i].IsOpenOn(ctx.Today))
                {
                    continue;
                }

                TransportNodeId next = edges[i].Other(unit.Node);
                ctx.World.Transport.TryGetNode(next, out TransportNode node);
                if (next == attackerNode || !node.IsLand || GeoUnitQueries.HasHostileUnitsAt(ctx, next, unit.Owner))
                {
                    continue;
                }

                if (ctx.World.TryGetControl(node.Region, out RegionControl control)
                    && (control.Controller == unit.Owner || (control.Controller.IsValid && !ctx.IsHostile(unit.Owner, control.Controller))))
                {
                    City city = GeoUnitQueries.CityAt(ctx, next);
                    if (city == null || !ctx.IsHostile(unit.Owner, city.Controller))
                    {
                        unit.Node = next;
                        return;
                    }
                }
            }

            CasualtyRules.RecordDeaths(ctx.Population, unit.HomeRegion, unit.Manpower);
            unit.Manpower = 0L;
            Remove(ctx, unit, false);
            notes.Add(CombatRules.UnitDestroyedNote + " @ " + unit.Id.Value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Birliği kaldırır: kalan asker eve döner; istenirse ekipman envantere iade edilir.</summary>
        public static void Remove(GeoWarfareContext ctx, LandUnit unit, bool returnEquipment)
        {
            UnitTypeDefinition type = ctx.Catalog.UnitTypes[unit.Type];
            if (returnEquipment && unit.Equipment.IsPositive)
            {
                InventoryRules.Return(ctx.Economy, unit.Owner, type.Equipment, unit.Equipment);
            }

            CasualtyRules.SendHome(ctx.Population, unit.HomeRegion, unit.Manpower);
            unit.Manpower = 0L;
            ctx.Warfare.Units.Remove(unit.Id);
        }
    }
}
