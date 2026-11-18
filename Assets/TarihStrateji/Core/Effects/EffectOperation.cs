namespace TarihStrateji.Core.Effects
{
    /// <summary>
    /// Etki işlemi. Sonuç = (taban + Σ<see cref="Add"/>) × (1 + Σ<see cref="Percent"/>):
    /// yüzdeler toplanarak birikir (0.10 = %10), böylece sonuç ekleme sırasından bağımsızdır.
    /// </summary>
    public enum EffectOperation : byte
    {
        Add = 0,
        Percent = 1,
    }
}
