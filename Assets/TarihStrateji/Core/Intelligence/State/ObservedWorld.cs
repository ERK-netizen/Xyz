using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>
    /// Bir ülkenin savaş sisi altındaki dünya görüşü (CLAUDE.md §2.6: YZ yalnız bunu okur).
    /// <see cref="VisibleCells"/> her gün P14'te yeniden hesaplanır (geçicidir, kayda yazılmaz);
    /// <see cref="Contacts"/> kalıcıdır ve (tür, kimlik) sırasındadır.
    /// </summary>
    public sealed class ObservedWorld
    {
        internal ObservedWorld(CountryId country)
        {
            Country = country;
        }

        public CountryId Country { get; }

        internal List<int> VisibleList { get; } = new List<int>();

        /// <summary>Bugün görünen hücre dizinleri (artan sırada).</summary>
        public IReadOnlyList<int> VisibleCells => VisibleList;

        internal List<ContactReport> ContactList { get; } = new List<ContactReport>();

        public IReadOnlyList<ContactReport> Contacts => ContactList;

        public bool IsVisible(int cellIndex) => VisibleList.BinarySearch(cellIndex) >= 0;

        public bool TryGetContact(SightingKind kind, ulong entityId, out ContactReport report)
        {
            int index = IndexOf(((ulong)kind << 56) | entityId);
            report = index >= 0 ? ContactList[index] : null;
            return index >= 0;
        }

        internal int IndexOf(ulong key)
        {
            int lo = 0;
            int hi = ContactList.Count - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) / 2);
                ulong k = ContactList[mid].SortKey;
                if (k == key)
                {
                    return mid;
                }

                if (k < key)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return ~lo;
        }
    }
}
