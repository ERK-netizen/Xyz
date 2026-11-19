using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Tek kanonik coğrafi dünya (ADR-011): değişmez geometri (<see cref="Atlas"/>, <see cref="Borders"/>,
    /// <see cref="Transport"/>) ile değişken durum (şehir ve bölge kontrolü) aynı yerde, ayrı katmanlarda.
    /// Şehirler ve bölgeler kimlik sırasıyla gezilir. Kurulum doğrulamalı yöntemlerle yapılır;
    /// <see cref="Validate"/> bütünleşik denetimleri (MAP-G02, CITY-001/002, liman bağlantısı) çalıştırır.
    /// </summary>
    public sealed class GeoWorld
    {
        private readonly Dictionary<CityId, City> _cities = new Dictionary<CityId, City>();
        private readonly List<CityId> _cityOrder = new List<CityId>();
        private readonly Dictionary<RegionId, RegionControl> _control = new Dictionary<RegionId, RegionControl>();

        public GeoWorld()
        {
            Atlas = new RegionAtlas();
            Borders = new BorderGraph();
            Transport = new TransportGraph(Atlas, Borders);
        }

        public RegionAtlas Atlas { get; }

        public BorderGraph Borders { get; }

        public TransportGraph Transport { get; }

        /// <summary>Kimlik sırasıyla şehirler.</summary>
        public IReadOnlyList<CityId> Cities => _cityOrder;

        public ValidationResult AddRegion(RegionId id, GeoMultiPolygon shape, CountryId owner)
        {
            ValidationResult added = Atlas.AddRegion(id, shape);
            if (added.IsValid)
            {
                _control.Add(id, new RegionControl(id, owner));
            }

            return added;
        }

        public ValidationResult AddCity(City city)
        {
            if (city == null || !city.Id.IsValid || _cities.ContainsKey(city.Id))
            {
                return ValidationResult.Fail(GeographyErrorKeys.SehirTekrar);
            }

            if (!Atlas.Contains(city.Region, city.Location))
            {
                return ValidationResult.Fail(GeographyErrorKeys.SehirBolgeDisinda);
            }

            if (!Transport.TryGetNode(city.Node, out TransportNode node) || node.Region != city.Region
                || (node.Kind != TransportNodeKind.City && node.Kind != TransportNodeKind.Port))
            {
                return ValidationResult.Fail(GeographyErrorKeys.SehirDugumUyumsuz);
            }

            _cities.Add(city.Id, city);
            int index = _cityOrder.BinarySearch(city.Id);
            _cityOrder.Insert(~index, city.Id);
            return ValidationResult.Ok;
        }

        public bool TryGetCity(CityId id, out City city) => _cities.TryGetValue(id, out city);

        public bool TryGetControl(RegionId region, out RegionControl control) => _control.TryGetValue(region, out control);

        public bool IsPort(City city) => Transport.TryGetNode(city.Node, out TransportNode node) && node.Kind == TransportNodeKind.Port;

        /// <summary>Kurulum sonrası bütünlük: liman şehrinin deniz yolu bağlantısı olmalı.</summary>
        public ValidationResult Validate()
        {
            for (int i = 0; i < _cityOrder.Count; i++)
            {
                City city = _cities[_cityOrder[i]];
                if (!IsPort(city))
                {
                    continue;
                }

                bool hasSeaLane = false;
                IReadOnlyList<TransportEdge> edges = Transport.EdgesOf(city.Node);
                for (int e = 0; e < edges.Count && !hasSeaLane; e++)
                {
                    hasSeaLane = edges[e].Kind == TransportEdgeKind.SeaLane;
                }

                if (!hasSeaLane)
                {
                    return ValidationResult.Fail(GeographyErrorKeys.LimanDenizBaglantisiYok);
                }
            }

            return ValidationResult.Ok;
        }
    }
}
