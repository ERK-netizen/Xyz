using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>Birlik sorguları (kimlik sırasında, tahsis çağıranın listesinde).</summary>
    public static class UnitQueries
    {
        public static bool IsHostile(WarfareContext ctx, CountryId a, CountryId b)
        {
            return a.IsValid && b.IsValid && a != b && ctx.Hostility.AtWar(a, b);
        }

        /// <summary>Hücredeki birlikleri <paramref name="into"/>'ya ekler.</summary>
        public static void UnitsAt(WarfareContext ctx, HexId hex, List<LandUnit> into)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Hex == hex && !u.IsEmbarked)
                {
                    into.Add(u);
                }
            }
        }

        /// <summary>Hücrede <paramref name="country"/>'ye düşman birlik var mı.</summary>
        public static bool HasHostileUnits(WarfareContext ctx, HexId hex, CountryId country)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Hex == hex && !u.IsEmbarked && IsHostile(ctx, u.Owner, country))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Birlik bu hücreye yürüyerek girebilir mi: kara; düşman birliği yok; düşman elindeki şehir
        /// değil (şehir sektör sektör alınır); kendi, sahipsiz veya savaştığı ülkenin toprağı.
        /// </summary>
        public static bool CanEnter(WarfareContext ctx, CountryId country, HexCell cell)
        {
            if (cell.IsWater || HasHostileUnits(ctx, cell.Id, country))
            {
                return false;
            }

            if (cell.Controller == country || !cell.Controller.IsValid)
            {
                return true;
            }

            bool hostileLand = IsHostile(ctx, country, cell.Controller);
            return hostileLand && !cell.City.IsValid;
        }
    }
}
