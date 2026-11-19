using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Warfare;

namespace TarihStrateji.Core.Simulation.State
{
    /// <summary>
    /// Savaş halindeki ülke çiftleri (simetrik; (küçük, büyük) kimlik sırasıyla). Adım 6'da senaryo
    /// kurulumu doldurur; Adım 8'de savaş ilanı ve iki taraflı barış Diplomacy üzerinden yürür.
    /// </summary>
    public sealed class WarRegistry : IHostility
    {
        private readonly List<ulong> _pairs = new List<ulong>(); // (min << 32) | max

        public int Count => _pairs.Count;

        public bool AtWar(CountryId a, CountryId b)
        {
            return a.IsValid && b.IsValid && a != b && _pairs.BinarySearch(Key(a, b)) >= 0;
        }

        /// <summary>Senaryo kurulumu (ve Adım 8'de diplomasi kuralları) için.</summary>
        public void SetAtWar(CountryId a, CountryId b, bool atWar)
        {
            if (!a.IsValid || !b.IsValid || a == b)
            {
                throw new ArgumentException("Geçersiz savaş çifti.");
            }

            ulong key = Key(a, b);
            int index = _pairs.BinarySearch(key);
            if (atWar && index < 0)
            {
                _pairs.Insert(~index, key);
            }
            else if (!atWar && index >= 0)
            {
                _pairs.RemoveAt(index);
            }
        }

        internal void Write(CanonicalWriter w)
        {
            w.WriteInt32(_pairs.Count);
            for (int i = 0; i < _pairs.Count; i++)
            {
                w.WriteUInt64(_pairs[i]);
            }
        }

        internal void Read(CanonicalReader r)
        {
            _pairs.Clear();
            int count = r.ReadCount(r.Remaining);
            for (int i = 0; i < count; i++)
            {
                ulong key = r.ReadUInt64();
                if ((_pairs.Count > 0 && _pairs[_pairs.Count - 1] >= key) || (key >> 32) >= (key & 0xFFFFFFFFUL) || (key >> 32) == 0UL)
                {
                    throw new FormatException(Domain.DomainErrorKeys.KayitBozuk);
                }

                _pairs.Add(key);
            }
        }

        private static ulong Key(CountryId a, CountryId b)
        {
            ulong x = a.Value;
            ulong y = b.Value;
            if (x > 0xFFFFFFFFUL || y > 0xFFFFFFFFUL)
            {
                throw new ArgumentOutOfRangeException(nameof(a), "Ülke kimliği 32 biti aşamaz.");
            }

            return x < y ? (x << 32) | y : (y << 32) | x;
        }
    }
}
