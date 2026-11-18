using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Unity.Presentation
{
    /// <summary>
    /// Kural katmanından sunuma TEK YÖNLÜ dönüşüm (CLAUDE.md §2.1). Sonuç yalnız çizim,
    /// animasyon ve metin içindir; hiçbir float değeri emir veya kural koduna geri verilmez.
    /// </summary>
    public static class Fixed64DisplayExtensions
    {
        private const float InverseScale = 1f / Fixed64.Scale;

        /// <summary>Ekranda konumlandırma ve görsel ölçek için yaklaşık float değer.</summary>
        public static float ToDisplayFloat(this Fixed64 value)
        {
            long integerPart = value.Raw / Fixed64.Scale;
            long fractionPart = value.Raw % Fixed64.Scale;
            return integerPart + (fractionPart * InverseScale);
        }
    }
}
