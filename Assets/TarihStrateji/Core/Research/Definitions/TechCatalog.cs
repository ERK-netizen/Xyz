using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Effects;

namespace TarihStrateji.Core.Research.Definitions
{
    /// <summary>
    /// Doğrulanmış çağ ve teknoloji tanımları (CLAUDE.md §2.6: teknoloji ağacı döngüsüz ve
    /// önkoşulludur). <see cref="TopologicalOrder"/>, Kahn algoritmasıyla ve eşitlikte artan
    /// <see cref="TechnologyId"/> sırasıyla üretilir; deterministiktir.
    /// </summary>
    public sealed class TechCatalog
    {
        private readonly EraDefinition[] _eras;
        private readonly TechnologyId[] _topological;

        private TechCatalog(EraDefinition[] eras, StableIdMap<TechnologyId, TechnologyDefinition> technologies, TechnologyId[] topological, Fixed64 basePointsPerDay)
        {
            _eras = eras;
            Technologies = technologies;
            _topological = topological;
            BasePointsPerDay = basePointsPerDay;
        }

        public IReadOnlyList<EraDefinition> Eras => _eras;

        public StableIdMap<TechnologyId, TechnologyDefinition> Technologies { get; }

        public IReadOnlyList<TechnologyId> TopologicalOrder => _topological;

        /// <summary>Ülke başına taban günlük araştırma puanı.</summary>
        public Fixed64 BasePointsPerDay { get; }

        public static Result<TechCatalog> Create(IReadOnlyList<EraDefinition> eras, IReadOnlyList<TechnologyDefinition> technologies, Fixed64 basePointsPerDay)
        {
            if (eras == null || technologies == null)
            {
                throw new ArgumentNullException(eras == null ? nameof(eras) : nameof(technologies));
            }

            if (eras.Count == 0 || !basePointsPerDay.IsPositive)
            {
                return Result<TechCatalog>.Failure(ResearchErrorKeys.TanimGecersiz);
            }

            var eraArray = new EraDefinition[eras.Count];
            for (int i = 0; i < eras.Count; i++)
            {
                EraDefinition e = eras[i];
                if (e == null || e.Index != i || string.IsNullOrEmpty(e.Key) || (i > 0 && e.StartDate < eras[i - 1].StartDate)
                    || (i == 0 && e.GateTechnologies.Count > 0))
                {
                    return Result<TechCatalog>.Failure(ResearchErrorKeys.CagSirasiGecersiz);
                }

                eraArray[i] = e;
            }

            var map = new StableIdMap<TechnologyId, TechnologyDefinition>(technologies.Count);
            for (int i = 0; i < technologies.Count; i++)
            {
                TechnologyDefinition t = technologies[i];
                if (t == null || !t.Id.IsValid || string.IsNullOrEmpty(t.Key) || t.Era < 0 || t.Era >= eraArray.Length
                    || !t.Cost.IsPositive || !EffectGrants.AllValid(t.Effects) || !map.TryAdd(t.Id, t))
                {
                    return Result<TechCatalog>.Failure(ResearchErrorKeys.TanimGecersiz);
                }
            }

            for (int i = 0; i < map.Count; i++)
            {
                TechnologyDefinition t = map.ValueAt(i);
                for (int p = 0; p < t.Prerequisites.Count; p++)
                {
                    if (!map.TryGetValue(t.Prerequisites[p], out TechnologyDefinition pre) || pre.Id == t.Id)
                    {
                        return Result<TechCatalog>.Failure(ResearchErrorKeys.OnkosulBilinmiyor);
                    }

                    if (pre.Era > t.Era)
                    {
                        return Result<TechCatalog>.Failure(ResearchErrorKeys.OnkosulCagiIleride);
                    }
                }
            }

            for (int i = 1; i < eraArray.Length; i++)
            {
                for (int g = 0; g < eraArray[i].GateTechnologies.Count; g++)
                {
                    if (!map.TryGetValue(eraArray[i].GateTechnologies[g], out TechnologyDefinition gate) || gate.Era >= i)
                    {
                        return Result<TechCatalog>.Failure(ResearchErrorKeys.CagSirasiGecersiz);
                    }
                }
            }

            TechnologyId[] order = TopologicalSort(map);
            if (order == null)
            {
                return Result<TechCatalog>.Failure(ResearchErrorKeys.AgacDongulu);
            }

            return Result<TechCatalog>.Success(new TechCatalog(eraArray, map, order, basePointsPerDay));
        }

        /// <summary>Kahn algoritması; hazır düğümler arasında en küçük kimlik önce. Döngüde null.</summary>
        private static TechnologyId[] TopologicalSort(StableIdMap<TechnologyId, TechnologyDefinition> map)
        {
            int n = map.Count;
            var indegree = new int[n];
            for (int i = 0; i < n; i++)
            {
                indegree[i] = map.ValueAt(i).Prerequisites.Count;
            }

            var done = new bool[n];
            var order = new TechnologyId[n];
            for (int k = 0; k < n; k++)
            {
                int pick = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!done[i] && indegree[i] == 0)
                    {
                        pick = i;
                        break;
                    }
                }

                if (pick < 0)
                {
                    return null;
                }

                done[pick] = true;
                order[k] = map.KeyAt(pick);
                for (int i = 0; i < n; i++)
                {
                    IReadOnlyList<TechnologyId> pre = map.ValueAt(i).Prerequisites;
                    for (int p = 0; p < pre.Count; p++)
                    {
                        if (pre[p] == order[k])
                        {
                            indegree[i]--;
                        }
                    }
                }
            }

            return order;
        }
    }
}
