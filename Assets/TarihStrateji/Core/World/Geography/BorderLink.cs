using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>İki bölge arasındaki ortak sınır: uzunluk (metre) ve kara geçişine açık olup olmadığı.</summary>
    public readonly struct BorderLink
    {
        public BorderLink(RegionId neighbor, long sharedLengthMeters, bool landPassable)
        {
            Neighbor = neighbor;
            SharedLengthMeters = sharedLengthMeters;
            LandPassable = landPassable;
        }

        public RegionId Neighbor { get; }

        public long SharedLengthMeters { get; }

        /// <summary>Yanlış: yalnız deniz/boğazla ayrılmış veya geçilmez sıradağ; kara yolu açılamaz.</summary>
        public bool LandPassable { get; }
    }
}
