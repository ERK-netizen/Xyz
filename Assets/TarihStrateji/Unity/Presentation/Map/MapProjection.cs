using TarihStrateji.Core.Domain.Geography;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Görüntü projeksiyonu (yalnız sunum, ADR-011): eşdikdörtgen; dünya birimi = 1° enlem, boylam
    /// <see cref="LonScale"/> (≈ cos 37°) ile sıkıştırılır ki Akdeniz enlemlerinde şekiller doğru oranda
    /// görünsün. Kural kodu bu dönüşümü görmez: dokunulan nokta E6 tamsayıya çevrilip <see cref="TargetRef"/>
    /// olarak gönderilir, doğrulama çekirdektedir.
    /// </summary>
    public static class MapProjection
    {
        public const float LonScale = 0.8f;
        private const float E6 = 1_000_000f;

        public static Vector2 ToWorld(GeoPoint p) => new Vector2(p.LongitudeE6 / E6 * LonScale, p.LatitudeE6 / E6);

        /// <summary>Bir tam boylam turunun dünya birimindeki genişliği (±180° dikişi; Adım 21 dünya haritası).</summary>
        public const float WrapWidth = 360f * LonScale;

        /// <summary><paramref name="x"/>'i <paramref name="reference"/>'a en yakın kopyasına kaydırır (dikişin öte yanındaki şehir
        /// kameraya yakın görünür). Bölgesel haritalarda fark yarım turdan küçük olduğundan değer değişmez.</summary>
        public static float NearestCopy(float x, float reference)
        {
            float d = x - reference;
            d -= Mathf.Round(d / WrapWidth) * WrapWidth;
            return reference + d;
        }

        /// <summary>Dünya x'ini [−180°, 180°) karşılığına indirir (dokunma, kamera sarmalaması).</summary>
        public static float Canonical(float x) => NearestCopy(x, 0f);

        /// <summary>Harita tüm boylamları kaplıyorsa (dünya senaryosu) kamera yatayda sarmalanır.</summary>
        public static bool IsWorldWide(Rect bounds) => bounds.width >= WrapWidth * 0.9f;

        public static GeoPoint ToGeo(Vector2 world)
        {
            float lat = Mathf.Clamp(world.y, -90f, 90f);
            return new GeoPoint(Mathf.RoundToInt(lat * E6), Mathf.RoundToInt(world.x / LonScale * E6));
        }
    }
}
