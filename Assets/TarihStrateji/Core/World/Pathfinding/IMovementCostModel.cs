using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World.Pathfinding
{
    /// <summary>
    /// Bir hareket alanının (kara, deniz, ikmal…) maliyet modeli.
    /// <para>Sözleşme:</para>
    /// <list type="bullet">
    /// <item>Geçilebilirlik simetriktir: <see cref="TryGetStepCost"/> yalnız ve yalnız
    /// <c>CanEnter(from) &amp;&amp; CanEnter(to)</c> iken başarılıdır. Bağlantı bileşenleri buna dayanır.</item>
    /// <item>Maliyet <c>km × çarpan</c>'dır ve çarpan hiçbir zaman <see cref="MinimumCostPerKm"/>'den
    /// küçük değildir (A* sezgiselinin kabul edilebilirliği buna dayanır).</item>
    /// <item>Aynı girdiye her zaman aynı sonucu verir (durumsuz, deterministik).</item>
    /// </list>
    /// </summary>
    public interface IMovementCostModel
    {
        Fixed64 MinimumCostPerKm { get; }

        bool CanEnter(HexCell cell);

        bool TryGetStepCost(HexCell from, HexCell to, Fixed64 km, out Fixed64 cost);
    }
}
