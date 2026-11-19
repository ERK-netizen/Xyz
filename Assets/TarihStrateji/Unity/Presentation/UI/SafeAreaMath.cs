using UnityEngine;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Ekranın güvenli alanını (çentik, yuvarlak köşe, hareket çubuğu) arayüz paneli birimine çevirir.
    /// <c>Screen.safeArea</c> alt-sol orijinli pikseldir; sonuç üst/alt/sol/sağ iç boşluktur.
    /// </summary>
    public static class SafeAreaMath
    {
        public static Vector4 Insets(Rect safeArea, float screenWidth, float screenHeight, float panelWidth, float panelHeight)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                return Vector4.zero;
            }

            float sx = panelWidth / screenWidth;
            float sy = panelHeight / screenHeight;
            float top = Mathf.Max(0f, screenHeight - safeArea.yMax) * sy;
            float bottom = Mathf.Max(0f, safeArea.yMin) * sy;
            float left = Mathf.Max(0f, safeArea.xMin) * sx;
            float right = Mathf.Max(0f, screenWidth - safeArea.xMax) * sx;
            return new Vector4(top, bottom, left, right);
        }
    }
}
