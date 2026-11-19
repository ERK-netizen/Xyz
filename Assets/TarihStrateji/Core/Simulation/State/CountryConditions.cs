using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Research.State;

namespace TarihStrateji.Core.Simulation.State
{
    /// <summary>Olay/odak koşullarını <see cref="GameState"/>'e göre değerlendirir.</summary>
    public sealed class CountryConditions : ICountryConditions
    {
        private readonly GameState _state;

        public CountryConditions(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public bool Holds(CountryId country, Condition condition)
        {
            switch (condition.Kind)
            {
                case ConditionKind.DateOnOrAfter:
                    return (ulong)_state.Today.DayNumber >= condition.Id;
                case ConditionKind.AtWar:
                    return _state.Diplomacy.IsAtAnyWar(country);
                case ConditionKind.NotAtWar:
                    return !_state.Diplomacy.IsAtAnyWar(country);
                case ConditionKind.StabilityBelow:
                    return _state.Politics.Countries.TryGetValue(country, out CountryPolitics low) && low.Stability < condition.Value;
                case ConditionKind.StabilityAtLeast:
                    return _state.Politics.Countries.TryGetValue(country, out CountryPolitics high) && high.Stability >= condition.Value;
                case ConditionKind.HasTechnology:
                    return _state.Research.Countries.TryGetValue(country, out CountryResearch research) && research.Has(new TechnologyId(condition.Id));
                case ConditionKind.FocusCompleted:
                    return _state.Narrative.Countries.TryGetValue(country, out CountryNarrative narrative) && narrative.CompletedFocuses.ContainsKey(new FocusId(condition.Id));
                case ConditionKind.TreasuryAtLeast:
                    return _state.Economy.Countries.TryGetValue(country, out CountryEconomy economy) && economy.Treasury >= condition.Value;
                default:
                    return false;
            }
        }
    }
}
