using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P13: günlük saldırı emirleri hedef hücre başına tek birleşik muharebede çözülür (hedefler
    /// ilk saldırganın kimlik sırasıyla). Bugün anında çözülmüş (çözüm defterinde) birlik atlanır;
    /// artık geçersiz emir silinir.
    /// </summary>
    public sealed class LandCombatSystem : MilitarySystem
    {
        public LandCombatSystem(GameContent content) : base(content) { }

        public override string Name => "savas.muharebe";

        public override SimulationPhase Phase => SimulationPhase.P13Combat;

        protected override void Run(SimulationContext context, WarfareContext war)
        {
            // Hedef hücre başına (ilk saldırganın kimlik sırasıyla) tek birleşik muharebe.
            var targets = new List<HexId>();
            for (int i = 0; i < war.Warfare.Units.Count; i++)
            {
                HexId target = war.Warfare.Units.ValueAt(i).AttackTarget;
                if (target.IsValid && !targets.Contains(target))
                {
                    targets.Add(target);
                }
            }

            var attackers = new List<LandUnit>();
            for (int t = 0; t < targets.Count; t++)
            {
                HexCoord target = context.World.Grid.FromId(targets[t]);
                attackers.Clear();
                CountryId side = CountryId.None;
                for (int i = 0; i < war.Warfare.Units.Count; i++)
                {
                    LandUnit unit = war.Warfare.Units.ValueAt(i);
                    if (unit.AttackTarget != targets[t] || (side.IsValid && unit.Owner != side))
                    {
                        continue;
                    }

                    if (unit.LastResolution.IsValid && context.Ledger.Contains(unit.LastResolution))
                    {
                        continue;
                    }

                    if (!CombatRules.CheckAttack(war, unit.Owner, unit.Id, target).IsValid)
                    {
                        MovementRules.ClearOrders(unit);
                        continue;
                    }

                    side = unit.Owner;
                    attackers.Add(unit);
                }

                if (attackers.Count > 0)
                {
                    RngStream rng = context.Rng(RngSubsystem.Combat, attackers[0].Id.Value, targets[t].Value);
                    CombatRules.Resolve(war, attackers, target, ref rng, Notes);
                }
            }
        }
    }
}
