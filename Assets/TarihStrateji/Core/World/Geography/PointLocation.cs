namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Noktanın bir halka/çokgene göre konumu. Sınır üzerindeki nokta içeride sayılır.</summary>
    public enum PointLocation : byte
    {
        Outside = 0,
        Inside = 1,
        OnBoundary = 2,
    }
}
