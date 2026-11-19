using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Coğrafi dünyanın günlük değişmezleri (§17.7): her şehrin hukukî sahibi ve fiilî kontrolcüsü geçerli,
    /// gelişim aralıkta, düğümü kendi bölgesinde; her bölgenin sahibi ve kontrolcüsü geçerli; "işgal" aşaması
    /// yalnız kontrolcü ≠ sahip iken.
    /// </summary>
    public static class GeoWorldInvariants
    {
        public const string Violation = "degismez.cografya";

        /// <returns>Eklenen ihlal sayısı.</returns>
        public static int Check(GeoWorld world, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City c);
                bool nodeOk = world.Transport.TryGetNode(c.Node, out TransportNode node) && node.Region == c.Region;
                if (!c.Owner.IsValid || !c.Controller.IsValid || !nodeOk
                    || c.DevelopmentLevel < City.MinDevelopment || c.DevelopmentLevel > City.MaxDevelopment)
                {
                    violations.Add(Violation + ":sehir " + c.Id.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            IReadOnlyList<RegionId> regions = world.Regions;
            for (int i = 0; i < regions.Count; i++)
            {
                world.TryGetControl(regions[i], out RegionControl rc);
                if (!rc.Owner.IsValid || !rc.Controller.IsValid || (rc.Stage == RegionControlStage.Occupied && rc.Controller == rc.Owner) || rc.DaysHeld < 0)
                {
                    violations.Add(Violation + ":bolge " + rc.Region.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            return violations.Count - before;
        }
    }
}
