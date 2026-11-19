using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Bölge kontrol aşaması geçişleri (§4.4, MAP-G08). Günlük girdi: bölgede baskın ülke (kilit şehir
    /// ve lojistik düğümleri ile kuvvet üstünlüğü Warfare'de hesaplanır; karışıksa <see cref="CountryId.None"/>)
    /// ve baskın tarafın ikmali. Kurallar:
    /// <list type="bullet">
    /// <item>baskın yoksa güvenli bölge çekişmeli olur, sayaç sıfırlanır;</item>
    /// <item>kontrolcü baskınsa sayaç artar; <c>DaysToSecure</c> dolunca güvenli olur;</item>
    /// <item>yabancı baskınsa bölge çekişmelidir; aynı ülke ikmalli olarak <c>DaysToOccupy</c> gün tutarsa
    /// kontrolcü olur ve aşama <see cref="RegionControlStage.Occupied"/> (sahip değişmez);</item>
    /// <item>ikmalsiz baskın sayaç ilerletmez.</item>
    /// </list>
    /// Dönüş: kontrolcü değiştiyse doğru.
    /// </summary>
    public static class RegionControlRules
    {
        public static bool Advance(RegionControl control, CountryId dominant, bool dominantSupplied, RegionControlParameters parameters)
        {
            if (!dominant.IsValid)
            {
                if (control.Stage == RegionControlStage.Secured)
                {
                    control.Stage = RegionControlStage.Contested;
                }

                control.Challenger = CountryId.None;
                control.DaysHeld = 0;
                return false;
            }

            if (dominant == control.Controller)
            {
                control.Challenger = CountryId.None;
                if (control.Stage == RegionControlStage.Secured)
                {
                    return false;
                }

                control.DaysHeld++;
                if (control.DaysHeld >= parameters.DaysToSecure)
                {
                    control.Stage = RegionControlStage.Secured;
                    control.DaysHeld = 0;
                }

                return false;
            }

            if (control.Challenger != dominant)
            {
                control.Challenger = dominant;
                control.DaysHeld = 0;
            }

            control.Stage = RegionControlStage.Contested;
            if (!dominantSupplied)
            {
                return false;
            }

            control.DaysHeld++;
            if (control.DaysHeld < parameters.DaysToOccupy)
            {
                return false;
            }

            control.Controller = dominant;
            control.Challenger = CountryId.None;
            control.DaysHeld = 0;
            control.Stage = control.Controller == control.Owner ? RegionControlStage.Contested : RegionControlStage.Occupied;
            return true;
        }
    }
}
