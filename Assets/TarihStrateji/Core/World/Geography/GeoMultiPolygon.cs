using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Parçalı toprak: anakara, adalar, anklavlar (§3.3). En az bir çokgen içerir.</summary>
    public sealed class GeoMultiPolygon
    {
        private readonly GeoPolygon[] _polygons;

        public GeoMultiPolygon(IReadOnlyList<GeoPolygon> polygons)
        {
            if (polygons == null || polygons.Count == 0)
            {
                throw new ArgumentException(GeographyErrorKeys.BolgeGeometrisiYok, nameof(polygons));
            }

            _polygons = new GeoPolygon[polygons.Count];
            GeoBounds bounds = polygons[0].Bounds;
            for (int i = 0; i < _polygons.Length; i++)
            {
                _polygons[i] = polygons[i] ?? throw new ArgumentNullException(nameof(polygons));
                bounds = bounds.Union(_polygons[i].Bounds);
            }

            Bounds = bounds;
        }

        public GeoMultiPolygon(GeoPolygon single)
            : this(new[] { single })
        {
        }

        public int Count => _polygons.Length;

        public GeoPolygon this[int index] => _polygons[index];

        public GeoBounds Bounds { get; }

        public bool Contains(GeoPoint p) => Locate(p) != PointLocation.Outside;

        public PointLocation Locate(GeoPoint p)
        {
            if (!Bounds.Contains(p))
            {
                return PointLocation.Outside;
            }

            PointLocation result = PointLocation.Outside;
            for (int i = 0; i < _polygons.Length; i++)
            {
                PointLocation location = _polygons[i].Locate(p);
                if (location == PointLocation.Inside)
                {
                    return PointLocation.Inside;
                }

                if (location == PointLocation.OnBoundary)
                {
                    result = PointLocation.OnBoundary;
                }
            }

            return result;
        }
    }
}
