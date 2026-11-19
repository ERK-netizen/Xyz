using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Bölge komşuluğu (§3.3): yalnız açıkça tanımlanan ortak sınırlar komşuluk yaratır; çokgenlerin
    /// görsel yakınlığı veya ekran kenarında yan yana durması komşuluk değildir. Kenarlar simetriktir
    /// ve komşu listesi kimlik sırasındadır. Değerleri içerik derleyici üretir.
    /// </summary>
    public sealed class BorderGraph
    {
        private static readonly List<BorderLink> Empty = new List<BorderLink>(0);
        private readonly Dictionary<RegionId, List<BorderLink>> _links = new Dictionary<RegionId, List<BorderLink>>();

        public ValidationResult AddBorder(RegionId a, RegionId b, long sharedLengthMeters, bool landPassable)
        {
            if (!a.IsValid || !b.IsValid || a == b || sharedLengthMeters <= 0L)
            {
                return ValidationResult.Fail(GeographyErrorKeys.SinirKendisiyle);
            }

            if (TryGetLink(a, b, out _))
            {
                return ValidationResult.Fail(GeographyErrorKeys.BolgeTekrar);
            }

            Insert(a, new BorderLink(b, sharedLengthMeters, landPassable));
            Insert(b, new BorderLink(a, sharedLengthMeters, landPassable));
            return ValidationResult.Ok;
        }

        public IReadOnlyList<BorderLink> Neighbors(RegionId region) => _links.TryGetValue(region, out List<BorderLink> list) ? list : Empty;

        public bool TryGetLink(RegionId a, RegionId b, out BorderLink link)
        {
            if (_links.TryGetValue(a, out List<BorderLink> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Neighbor == b)
                    {
                        link = list[i];
                        return true;
                    }
                }
            }

            link = default;
            return false;
        }

        /// <summary>Aynı bölge veya kara geçişine açık ortak sınır.</summary>
        public bool LandConnected(RegionId a, RegionId b) => a == b || (TryGetLink(a, b, out BorderLink link) && link.LandPassable);

        private void Insert(RegionId owner, BorderLink link)
        {
            if (!_links.TryGetValue(owner, out List<BorderLink> list))
            {
                list = new List<BorderLink>(4);
                _links.Add(owner, list);
            }

            int index = 0;
            while (index < list.Count && list[index].Neighbor < link.Neighbor)
            {
                index++;
            }

            list.Insert(index, link);
        }
    }
}
