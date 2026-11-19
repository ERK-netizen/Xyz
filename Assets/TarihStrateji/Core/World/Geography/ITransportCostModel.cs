namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Rota maliyeti (ör. hareket dakikası). Kenar kullanılamıyorsa (yanlış kuvvet türü, henüz açılmamış
    /// demiryolu, düşman kontrolü) yanlış döner. <see cref="MinCostPerKilometer"/> her kullanılabilir
    /// kenarın km başına maliyetinin alt sınırıdır; A* sezgiselinin kabul edilebilirliği buna dayanır.
    /// </summary>
    public interface ITransportCostModel
    {
        long MinCostPerKilometer { get; }

        bool TryGetCost(TransportEdge edge, TransportNode from, TransportNode to, out long cost);
    }
}
