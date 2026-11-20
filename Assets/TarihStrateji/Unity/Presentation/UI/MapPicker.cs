using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.Map;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Dokunuşu harita nesnesine çevirir (yalnız sunum): <c>tolerance</c> içindeki en yakın görünür kara
    /// birliği, yoksa en yakın şehir, yoksa noktayı içeren bölge (çekirdeğin 1°'lik bölge atlası), yoksa
    /// nokta. Birlik ve şehirler mekânsal ızgarada (<see cref="SpatialGrid"/>) tutulur; sorgu yalnız
    /// dokunuşun çevresindeki hücreleri tarar. Durum değişince <see cref="Rebuild"/> çağrılır.
    /// </summary>
    public sealed class MapPicker
    {
        private const float CellSize = 0.5f;

        private readonly SpatialGrid _unitGrid = new SpatialGrid(CellSize);
        private readonly SpatialGrid _cityGrid = new SpatialGrid(CellSize);
        private readonly List<UnitView> _units = new List<UnitView>(256);
        private readonly List<CityView> _cities = new List<CityView>(256);

        public void Rebuild(List<UnitView> units, List<CityView> cities)
        {
            _unitGrid.Clear();
            _cityGrid.Clear();
            _units.Clear();
            _cities.Clear();
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Kind == UnitViewKind.Land)
                {
                    _units.Add(units[i]);
                    _unitGrid.Add(MapProjection.ToWorld(units[i].Location));
                }
            }

            for (int i = 0; i < cities.Count; i++)
            {
                _cities.Add(cities[i]);
                _cityGrid.Add(MapProjection.ToWorld(cities[i].Location));
            }
        }

        public MapPick Pick(IGameReadModel model, Vector2 world, float tolerance)
        {
            GeoPoint at = MapProjection.ToGeo(world);
            int unit = _unitGrid.Nearest(world, tolerance);
            if (unit >= 0)
            {
                UnitView u = _units[unit];
                return MapPick.OfUnit(u.Id, u.IsOwn, model.FindRegion(u.Location), u.Location);
            }

            int city = _cityGrid.Nearest(world, tolerance);
            if (city >= 0)
            {
                CityView c = _cities[city];
                return MapPick.OfCity(c.Id, model.FindRegion(c.Location), c.Location);
            }

            RegionId region = model.FindRegion(at);
            return region.IsValid ? MapPick.OfRegion(region, at) : MapPick.OfPoint(at);
        }
    }
}
