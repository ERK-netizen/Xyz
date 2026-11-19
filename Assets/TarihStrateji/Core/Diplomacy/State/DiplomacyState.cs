using System.Collections.Generic;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>
    /// Savaşlar, antlaşmalar, teklifler ve sürgündeki hükümetler. Savaş çiftleri sıralı bir dizinle
    /// (<see cref="AtWar"/> O(log n)) tutulur; dizin her savaş ekleme/kaldırmada yeniden kurulur.
    /// </summary>
    public sealed class DiplomacyState
    {
        private readonly List<ulong> _pairs = new List<ulong>();

        public StableIdMap<WarId, War> Wars { get; } = new StableIdMap<WarId, War>();

        public StableIdAllocator WarIds { get; internal set; } = new StableIdAllocator();

        public StableIdMap<TreatyId, Treaty> Treaties { get; } = new StableIdMap<TreatyId, Treaty>();

        public StableIdAllocator TreatyIds { get; internal set; } = new StableIdAllocator();

        public StableIdMap<ProposalId, Proposal> Proposals { get; } = new StableIdMap<ProposalId, Proposal>();

        public StableIdAllocator ProposalIds { get; internal set; } = new StableIdAllocator();

        /// <summary>Sürgündeki hükümet → ev sahibi müttefik.</summary>
        public StableIdMap<CountryId, CountryId> Exiles { get; } = new StableIdMap<CountryId, CountryId>();

        public bool AtWar(CountryId a, CountryId b)
        {
            return a.IsValid && b.IsValid && a != b && a.Value <= 0xFFFFFFFFUL && b.Value <= 0xFFFFFFFFUL && _pairs.BinarySearch(Key(a, b)) >= 0;
        }

        public bool TryGetWar(CountryId a, CountryId b, out War war)
        {
            for (int i = 0; i < Wars.Count; i++)
            {
                War w = Wars.ValueAt(i);
                if ((w.Attacker == a && w.Defender == b) || (w.Attacker == b && w.Defender == a))
                {
                    war = w;
                    return true;
                }
            }

            war = null;
            return false;
        }

        public bool IsAtAnyWar(CountryId country)
        {
            for (int i = 0; i < Wars.Count; i++)
            {
                if (Wars.ValueAt(i).Involves(country))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasTreaty(TreatyKind kind, CountryId a, CountryId b, GameDate today)
        {
            for (int i = 0; i < Treaties.Count; i++)
            {
                Treaty t = Treaties.ValueAt(i);
                if (t.Kind == kind && t.Binds(a, b) && t.IsActiveOn(today))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsExiled(CountryId country) => Exiles.ContainsKey(country);

        internal void RebuildIndex()
        {
            _pairs.Clear();
            for (int i = 0; i < Wars.Count; i++)
            {
                War w = Wars.ValueAt(i);
                _pairs.Add(Key(w.Attacker, w.Defender));
            }

            _pairs.Sort();
        }

        internal static ulong Key(CountryId a, CountryId b)
        {
            ulong x = a.Value;
            ulong y = b.Value;
            return x < y ? (x << 32) | y : (y << 32) | x;
        }
    }
}
