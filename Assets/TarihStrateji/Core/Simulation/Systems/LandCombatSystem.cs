using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P13 (son): kara muharebeleri ve çıkarmalar. Hedef hücre başına (ilk saldırganın kimlik
    /// sırasıyla) önce karadan saldıranlar tek birleşik muharebede, sonra gemiden çıkarma yapanlar
    /// ayrı bir muharebede çözülür. O gün anında çözülmüş (<c>ResolutionId</c> defterde) birlikler
    /// atlanır; geçersizleşen emirler temizlenir. Boş ve girilebilir hedefe çıkarma muharebesizdir.
    /// </summary>
    public sealed class LandCombatSystem : MilitarySystem
    {
        public LandCombatSystem(GameContent content) : base(content) { }

        public override string Name => "savas.muharebe";

        public override SimulationPhase Phase => SimulationPhase.P13Combat;

        protected override void Run(SimulationContext context, WarfareContext war)
        {
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
                CollectAttackers(context, war, targets[t], target, false, attackers);
                if (attackers.Count > 0)
                {
                    RngStream rng = context.Rng(RngSubsystem.Combat, attackers[0].Id.Value, targets[t].Value);
                    CombatRules.Resolve(war, attackers, target, ref rng, Notes);
                }

                CollectAttackers(context, war, targets[t], target, true, attackers);
                if (attackers.Count == 0)
                {
                    continue;
                }

                if (AmphibiousRules.IsAssault(war, attackers[0].Owner, context.World.Cell(target)))
                {
                    RngStream rng = context.Rng(RngSubsystem.Combat, attackers[0].Id.Value, targets[t].Value);
                    CombatRules.ResolveLanding(war, attackers, target, ref rng, Notes);
                }
                else
                {
                    for (int i = 0; i < attackers.Count; i++)
                    {
                        AmphibiousRules.Disembark(attackers[i], targets[t]);
                        Notes.Add(AmphibiousRules.LandedNote + " @ " + attackers[i].Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
        }

        private static void CollectAttackers(SimulationContext context, WarfareContext war, HexId targetId, HexCoord target, bool embarked, List<LandUnit> into)
        {
            into.Clear();
            CountryId side = CountryId.None;
            for (int i = 0; i < war.Warfare.Units.Count; i++)
            {
                LandUnit unit = war.Warfare.Units.ValueAt(i);
                if (unit.AttackTarget != targetId || unit.IsEmbarked != embarked || (side.IsValid && unit.Owner != side))
                {
                    continue;
                }

                if (unit.LastResolution.IsValid && context.Ledger.Contains(unit.LastResolution))
                {
                    continue;
                }

                bool valid = embarked
                    ? AmphibiousRules.CheckLanding(war, unit.Owner, unit.Id, target).IsValid
                    : CombatRules.CheckAttack(war, unit.Owner, unit.Id, target).IsValid;
                if (!valid)
                {
                    MovementRules.ClearOrders(unit);
                    continue;
                }

                side = unit.Owner;
                into.Add(unit);
            }
        }
    }
}
