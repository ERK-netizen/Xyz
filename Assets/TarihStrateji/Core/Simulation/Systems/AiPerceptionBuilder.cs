using System.Collections.Generic;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.Simulation.Systems.Geo;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// <see cref="AiPerception"/>'ı kurar ve "YZ yalnız kendi gördüğünü okur" kuralını burada
    /// yapısal olarak uygular: yabancı birlik listesi algıya hiç girmez (yalnız
    /// <c>ObservedWorld</c> temasları); kendi durumu ve kamuya açık harita/diplomasi girer.
    /// Seçenek listeleri (teknoloji, odak, toplama yeri) yalnız ülkenin kendi durumuna bakan
    /// kurallarla süzülür.
    /// </summary>
    public static class AiPerceptionBuilder
    {
        public static AiPerception Build(GameState state, GameContent content, CountryId country, List<ulong> borders)
        {
            var p = new AiPerception(country, state.Today, state.World, state.Diplomacy, content.Diplomacy, state.Intelligence.For(country), content.Warfare);

            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                LandUnit u = state.Warfare.Units.ValueAt(i);
                if (u.Owner == country)
                {
                    p.OwnUnits.Add(u);
                }
            }

            if (state.Economy.Countries.TryGetValue(country, out CountryEconomy economy))
            {
                p.Treasury = economy.Treasury;
                p.TaxRate = economy.TaxRate;
            }

            if (state.Politics.Countries.TryGetValue(country, out CountryPolitics politics))
            {
                p.Stability = politics.Stability;
            }

            if (state.Research.Countries.TryGetValue(country, out CountryResearch research))
            {
                p.CurrentResearch = research.Current;
                for (int i = 0; i < content.Technology.Technologies.Count; i++)
                {
                    TechnologyId tech = content.Technology.Technologies.KeyAt(i);
                    if (ResearchRules.CheckSetCurrent(state.Research, content.Technology, country, tech, state.Today).IsValid)
                    {
                        p.AvailableTechnologies.Add(tech);
                    }
                }
            }

            var conditions = new CountryConditions(state);
            state.Narrative.Countries.TryGetValue(country, out CountryNarrative narrative);
            p.CurrentFocus = narrative != null ? narrative.CurrentFocus : FocusId.None;
            for (int i = 0; i < content.Narrative.Focuses.Count; i++)
            {
                FocusId focus = content.Narrative.Focuses.KeyAt(i);
                if (NarrativeRules.CheckSelectFocus(state.Narrative, content.Narrative, conditions, country, focus).IsValid)
                {
                    p.AvailableFocuses.Add(focus);
                }
            }

            if (narrative != null)
            {
                for (int i = 0; i < narrative.Pending.Count; i++)
                {
                    p.PendingEvents.Add(narrative.Pending[i].EventId);
                }
            }

            GeoWarfareContext war = GeoWarfareContexts.From(state, content);
            for (int i = 0; i < state.World.Cities.Count; i++)
            {
                state.World.TryGetCity(state.World.Cities[i], out City city);
                if (city.Owner == country && GeoUnitRules.CheckRecruit(war, country, content.Ai.RecruitUnitType, city.Id).IsValid)
                {
                    p.RecruitSites.Add(city.Id);
                }
            }

            for (int i = 0; i < state.World.Regions.Count; i++)
            {
                state.World.TryGetControl(state.World.Regions[i], out RegionControl region);
                if (region.Owner == country && state.Population.Regions.TryGetValue(region.Region, out RegionPopulation pop))
                {
                    p.WorkingAge += pop.WorkingAge;
                    p.Mobilized += pop.Mobilized;
                }
            }

            for (int i = 0; i < state.Economy.Constructions.Count; i++)
            {
                if (state.Economy.Constructions.ValueAt(i).Country == country)
                {
                    p.ConstructionsInProgress++;
                }
            }

            if (economy != null && economy.Treasury >= content.Ai.ConstructionReserve)
            {
                AddBuildOptions(state, content, country, p);
            }

            BorderIndex.NeighborsOf(borders, country, p.Neighbors);
            return p;
        }

        /// <summary>Tercih sırasındaki her tür için kendi (sahip + kontrol) şehirlerinden (kimlik sırasıyla) ilk geçerli olan.</summary>
        private static void AddBuildOptions(GameState state, GameContent content, CountryId country, AiPerception p)
        {
            var priority = content.Ai.BuildPriority;
            for (int t = 0; t < priority.Count; t++)
            {
                for (int c = 0; c < state.World.Cities.Count; c++)
                {
                    state.World.TryGetCity(state.World.Cities[c], out City city);
                    if (city.Owner == country && city.Controller == country
                        && ConstructionRules.Validate(state.Economy, content.Economy, state.World, country, priority[t], city.Id).IsValid)
                    {
                        p.BuildOptions.Add(new AI.Perception.AiBuildOption(priority[t], city.Id));
                        break;
                    }
                }
            }
        }
    }
}
