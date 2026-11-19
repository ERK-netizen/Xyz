using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>
    /// Doğrulanmış olay ve odak tanımları. Odak ön koşulları döngüsüz olmalıdır (Kahn; eşitlikte
    /// küçük <see cref="FocusId"/>); dışlamalar var olan odaklara ve simetrik yorumlanır.
    /// </summary>
    public sealed class NarrativeCatalog
    {
        private NarrativeCatalog(StableIdMap<EventId, EventDefinition> events, StableIdMap<FocusId, FocusDefinition> focuses)
        {
            Events = events;
            Focuses = focuses;
        }

        public StableIdMap<EventId, EventDefinition> Events { get; }

        public StableIdMap<FocusId, FocusDefinition> Focuses { get; }

        public static Result<NarrativeCatalog> Create(IReadOnlyList<EventDefinition> events, IReadOnlyList<FocusDefinition> focuses)
        {
            if (events == null || focuses == null)
            {
                throw new ArgumentNullException(events == null ? nameof(events) : nameof(focuses));
            }

            var eventMap = new StableIdMap<EventId, EventDefinition>(events.Count);
            for (int i = 0; i < events.Count; i++)
            {
                EventDefinition e = events[i];
                if (e == null || !e.Id.IsValid || string.IsNullOrEmpty(e.Key) || e.MeanDays < 1 || e.ResponseDays < 1 || e.Options.Count == 0
                    || !ValidConditions(e.Conditions) || !eventMap.TryAdd(e.Id, e))
                {
                    return Result<NarrativeCatalog>.Failure(NarrativeErrorKeys.TanimGecersiz);
                }

                for (int o = 0; o < e.Options.Count; o++)
                {
                    EventOption option = e.Options[o];
                    if (option == null || string.IsNullOrEmpty(option.Key) || option.DurationDays < 0 || !EffectGrants.AllValid(option.Grants))
                    {
                        return Result<NarrativeCatalog>.Failure(NarrativeErrorKeys.TanimGecersiz);
                    }
                }
            }

            var focusMap = new StableIdMap<FocusId, FocusDefinition>(focuses.Count);
            for (int i = 0; i < focuses.Count; i++)
            {
                FocusDefinition f = focuses[i];
                if (f == null || !f.Id.IsValid || string.IsNullOrEmpty(f.Key) || f.Days < 1 || !ValidConditions(f.Conditions)
                    || !EffectGrants.AllValid(f.Grants) || !focusMap.TryAdd(f.Id, f))
                {
                    return Result<NarrativeCatalog>.Failure(NarrativeErrorKeys.TanimGecersiz);
                }
            }

            for (int i = 0; i < focusMap.Count; i++)
            {
                FocusDefinition f = focusMap.ValueAt(i);
                for (int p = 0; p < f.Prerequisites.Count; p++)
                {
                    if (!focusMap.ContainsKey(f.Prerequisites[p]) || f.Prerequisites[p] == f.Id)
                    {
                        return Result<NarrativeCatalog>.Failure(NarrativeErrorKeys.TanimGecersiz);
                    }
                }

                for (int x = 0; x < f.Exclusive.Count; x++)
                {
                    if (!focusMap.ContainsKey(f.Exclusive[x]) || f.Exclusive[x] == f.Id)
                    {
                        return Result<NarrativeCatalog>.Failure(NarrativeErrorKeys.TanimGecersiz);
                    }
                }
            }

            return IsAcyclic(focusMap)
                ? Result<NarrativeCatalog>.Success(new NarrativeCatalog(eventMap, focusMap))
                : Result<NarrativeCatalog>.Failure(NarrativeErrorKeys.OdakDongusu);
        }

        /// <summary>İki odak birbirini dışlıyor mu (tanımlardan herhangi birinde belirtilmişse).</summary>
        public bool AreExclusive(FocusId a, FocusId b)
        {
            return (Focuses.TryGetValue(a, out FocusDefinition fa) && Contains(fa.Exclusive, b))
                || (Focuses.TryGetValue(b, out FocusDefinition fb) && Contains(fb.Exclusive, a));
        }

        private static bool Contains(IReadOnlyList<FocusId> list, FocusId id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ValidConditions(IReadOnlyList<Condition> conditions)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].Kind < ConditionKind.DateOnOrAfter || conditions[i].Kind > ConditionKind.TreasuryAtLeast)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAcyclic(StableIdMap<FocusId, FocusDefinition> map)
        {
            int n = map.Count;
            var indegree = new int[n];
            for (int i = 0; i < n; i++)
            {
                indegree[i] = map.ValueAt(i).Prerequisites.Count;
            }

            var ready = new SortedSet<int>();
            for (int i = 0; i < n; i++)
            {
                if (indegree[i] == 0)
                {
                    ready.Add(i);
                }
            }

            int visited = 0;
            while (ready.Count > 0)
            {
                int current = ready.Min;
                ready.Remove(current);
                visited++;
                FocusId id = map.KeyAt(current);
                for (int j = 0; j < n; j++)
                {
                    if (Contains(map.ValueAt(j).Prerequisites, id) && --indegree[j] == 0)
                    {
                        ready.Add(j);
                    }
                }
            }

            return visited == n;
        }
    }
}
