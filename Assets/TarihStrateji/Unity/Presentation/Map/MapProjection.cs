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

        public static GeoPoint ToGeo(Vector2 world)
        {
            float lat = Mathf.Clamp(world.y, -90f, 90f);
            return new GeoPoint(Mathf.RoundToInt(lat * E6), Mathf.RoundToInt(world.x / LonScale * E6));
        }
    }
}
