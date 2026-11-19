using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Input
{
    /// <summary>
    /// Dokunma hareketlerinin saf hesapları (EditMode testli): dokunuş/sürükleme ayrımı, iki
    /// parmakla yakınlaştırma, kamera sınırları ve yatay sarmalama.
    /// </summary>
    public static class GestureMath
    {
        /// <summary>Dokunuş sayılması için en çok kayma (inç); ~2 mm parmak titremesini tolere eder.</summary>
        public const float TapSlopInches = 0.08f;

        public const float TapMaxSeconds = 0.35f;

        /// <summary>DPI bilinmeyen ekranlar (Screen.dpi = 0) için varsayılan.</summary>
        public const float FallbackDpi = 320f;

        public static float SlopPixels(float dpi) => TapSlopInches * (dpi > 1f ? dpi : FallbackDpi);

        public static bool IsTap(Vector2 down, Vector2 up, float seconds, float dpi, bool movedBeyondSlop) =>
            !movedBeyondSlop && seconds <= TapMaxSeconds && (up - down).magnitude <= SlopPixels(dpi);

        /// <summary>İki parmak arasındaki mesafe oranıyla yeni dikey yarı boy; [min, max] aralığında.</summary>
        public static float PinchOrthographicSize(float current, float previousDistance, float currentDistance, float min, float max)
        {
            if (previousDistance <= 0.5f || currentDistance <= 0.5f)
            {
                return Mathf.Clamp(current, min, max);
            }

            return Mathf.Clamp(current * (previousDistance / currentDistance), min, max);
        }

        /// <summary>Fare tekerleği: her çentik %12 yakınlaştırır/uzaklaştırır.</summary>
        public static float ScrollOrthographicSize(float current, float scrollDelta, float min, float max) =>
            Mathf.Clamp(current * Mathf.Pow(0.88f, scrollDelta), min, max);

        /// <summary>x'i [0, dünya genişliği) aralığına sarmalar.</summary>
        public static float WrapX(float x, float worldWidth)
        {
            float wrapped = x % worldWidth;
            return wrapped < 0f ? wrapped + worldWidth : wrapped;
        }

        /// <summary>
        /// Kamera merkezinin y sınırı: harita ekrandan büyükse kenarlar dışarı taşmaz; küçükse harita
        /// ortalanır. Harita y aralığı [bottom, top] (top = 1, bottom = −1,5 × (satır − 1) − 1).
        /// </summary>
        public static float ClampY(float y, float halfHeight, float top, float bottom)
        {
            float min = bottom + halfHeight;
            float max = top - halfHeight;
            if (min > max)
            {
                return (top + bottom) * 0.5f;
            }

            return Mathf.Clamp(y, min, max);
        }
    }
}
