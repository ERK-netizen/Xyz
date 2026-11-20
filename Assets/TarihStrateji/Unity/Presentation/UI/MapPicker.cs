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
    /// Dokunuşu harita nesnesine çevirir (yalnız sunum): dünya biriminde <c>tolerance</c> içindeki en
    /// yakın kara birliği (yalnız görünenler), yoksa en yakın şehir, yoksa noktayı içeren bölge, yoksa nokta.
    /// Eşit uzaklıkta listede önce gelen kazanır.
    /// </summary>
    public static class MapPicker
    {
        public static MapPick Pick(IGameReadModel model, Vector2 world, float tolerance, List<UnitView> units, List<CityView> cities)
        {
            GeoPoint at = MapProjection.ToGeo(world);
            RegionId region = model.FindRegion(at);
            float best = tolerance * tolerance;
            int unit = -1;
            for (int i = 0; i < units.Count; i++)
            {
                float d = (MapProjection.ToWorld(units[i].Location) - world).sqrMagnitude;
                if (units[i].Kind == UnitViewKind.Land && d <= best)
                {
                    best = d;
                    unit = i;
                }
            }

            if (unit >= 0)
            {
                UnitView u = units[unit];
                return MapPick.OfUnit(u.Id, u.IsOwn, region, u.Location);
            }

            best = tolerance * tolerance;
            int city = -1;
            for (int i = 0; i < cities.Count; i++)
            {
                float d = (MapProjection.ToWorld(cities[i].Location) - world).sqrMagnitude;
                if (d <= best)
                {
                    best = d;
                    city = i;
                }
            }

            if (city >= 0)
            {
                return MapPick.OfCity(cities[city].Id, model.FindRegion(cities[city].Location), cities[city].Location);
            }

            return region.IsValid ? MapPick.OfRegion(region, at) : MapPick.OfPoint(at);
        }
    }
}
