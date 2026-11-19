using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Harita renkleri: fiili kontrolcünün (controller) rengi araziyle karıştırılır; işgal edilmiş
    /// hücrede (sahip ≠ kontrolcü) hukuki sahibin rengi çizgili değil karışım olarak görünür ve ton
    /// koyulaşır; savaş sisindeki hücre karartılır. Arazi kodları <c>TerrainType</c> ile aynıdır.
    /// </summary>
    public static class MapPalette
    {
        public const byte TerrainOcean = 0;
        public const byte TerrainLake = 1;

        public static readonly Color32 Background = new Color32(14, 26, 40, 255);
        public static readonly Color32 Highlight = new Color32(255, 214, 64, 255);
        public static readonly Color32 Outline = new Color32(250, 250, 245, 255);
        public static readonly Color32 EnemyOutline = new Color32(200, 40, 40, 255);
        public static readonly Color32 EstimateOutline = new Color32(150, 150, 150, 255);

        private static readonly Color32[] CountryColors =
        {
            new Color32(200, 48, 48, 255),   // 1 Türkiye
            new Color32(46, 104, 196, 255),  // 2 Yunanistan
            new Color32(54, 150, 84, 255),   // 3 Bulgaristan
            new Color32(150, 72, 168, 255),  // 4 Arnavutluk
            new Color32(214, 140, 40, 255),
            new Color32(40, 160, 168, 255),
            new Color32(150, 110, 60, 255),
            new Color32(120, 120, 130, 255),
        };

        private static readonly Color32[] TerrainColors =
        {
            new Color32(28, 58, 92, 255),    // 0 okyanus
            new Color32(48, 96, 140, 255),   // 1 göl
            new Color32(150, 170, 100, 255), // 2 ova
            new Color32(70, 120, 70, 255),   // 3 orman
            new Color32(150, 130, 90, 255),  // 4 tepe
            new Color32(120, 110, 105, 255), // 5 dağ
            new Color32(210, 190, 130, 255), // 6 çöl
            new Color32(90, 120, 100, 255),  // 7 bataklık
            new Color32(180, 190, 190, 255), // 8 tundra
        };

        public static Color32 Country(CountryId country)
        {
            if (!country.IsValid)
            {
                return new Color32(128, 128, 128, 255);
            }

            return CountryColors[(int)((country.Value - 1UL) % (ulong)CountryColors.Length)];
        }

        public static Color32 Terrain(byte terrain) => terrain < TerrainColors.Length ? TerrainColors[terrain] : TerrainColors[2];

        public static Color32 Cell(in CellView cell, bool visible)
        {
            Color32 color;
            if (cell.IsWater)
            {
                color = Terrain(cell.Terrain);
            }
            else if (!cell.Controller.IsValid)
            {
                color = Terrain(cell.Terrain);
            }
            else
            {
                color = Color32.Lerp(Country(cell.Controller), Terrain(cell.Terrain), 0.3f);
                if (cell.Owner.IsValid && cell.Owner != cell.Controller)
                {
                    color = Scale(Color32.Lerp(color, Country(cell.Owner), 0.35f), 0.8f);
                }
            }

            return visible ? color : Scale(color, 0.55f);
        }

        public static Color32 Scale(Color32 color, float factor) =>
            new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
                color.a);
    }
}
