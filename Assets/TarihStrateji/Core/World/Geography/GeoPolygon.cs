using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Dış halka + delikler (göl, anklav). Deliğin sınırı çokgene aittir.</summary>
    public sealed class GeoPolygon
    {
        private readonly GeoRing[] _holes;

        public GeoPolygon(GeoRing outer, IReadOnlyList<GeoRing> holes = null)
        {
            Outer = outer ?? throw new ArgumentNullException(nameof(outer));
            _holes = new GeoRing[holes?.Count ?? 0];
            for (int i = 0; i < _holes.Length; i++)
            {
                _holes[i] = holes[i] ?? throw new ArgumentNullException(nameof(holes));
            }
        }

        public GeoRing Outer { get; }

        public int HoleCount => _holes.Length;

        public GeoRing Hole(int index) => _holes[index];

        public GeoBounds Bounds => Outer.Bounds;

        public PointLocation Locate(GeoPoint p)
        {
            PointLocation outer = Outer.Locate(p);
            if (outer != PointLocation.Inside)
            {
                return outer;
            }

            for (int i = 0; i < _holes.Length; i++)
            {
                PointLocation hole = _holes[i].Locate(p);
                if (hole == PointLocation.Inside)
                {
                    return PointLocation.Outside;
                }

                if (hole == PointLocation.OnBoundary)
                {
                    return PointLocation.OnBoundary;
                }
            }

            return PointLocation.Inside;
        }
    }
}
