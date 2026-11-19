using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Dünya ızgarasının ekran geometrisi (CLAUDE.md §2.3): sivri tepeli altıgen, "odd-r" düzen, dış
    /// yarıçap 1 birim. Satır 0 kuzeydedir ve ekranda yukarıdadır (y = −1,5 × satır); tek satırlar
    /// yarım hücre doğuya kaymıştır. Sütunlar ±180°'de sarmalanır: dünya genişliği √3 × sütun sayısı.
    /// Yalnız çizim ve dokunma seçimi içindir; kural koduna float geçmez.
    /// </summary>
    public static class HexLayout
    {
        public const float Sqrt3 = 1.7320508f;
        public const float ColumnWidth = Sqrt3;
        public const float RowHeight = 1.5f;

        private static readonly Vector2[] CornerOffsets =
        {
            new Vector2(Sqrt3 * 0.5f, 0.5f),
            new Vector2(0f, 1f),
            new Vector2(-Sqrt3 * 0.5f, 0.5f),
            new Vector2(-Sqrt3 * 0.5f, -0.5f),
            new Vector2(0f, -1f),
            new Vector2(Sqrt3 * 0.5f, -0.5f),
        };

        public static float WorldWidth(int mapWidth) => ColumnWidth * mapWidth;

        public static Vector2 CellCenter(int column, int row) => new Vector2(ColumnWidth * (column + (0.5f * (row & 1))), -RowHeight * row);

        /// <summary>Hücre dizininden (satır × genişlik + sütun) merkez.</summary>
        public static Vector2 CellCenterOfIndex(int cellIndex, int mapWidth) => CellCenter(cellIndex % mapWidth, cellIndex / mapWidth);

        /// <summary>Merkeze göre köşe (0 = doğu-kuzeydoğu, saat yönünün tersine).</summary>
        public static Vector2 Corner(int index) => CornerOffsets[index];

        public static int WrapColumn(int column, int mapWidth)
        {
            int c = column % mapWidth;
            return c < 0 ? c + mapWidth : c;
        }

        /// <summary>
        /// Dünya noktasındaki hücrenin dizini (satır × genişlik + sütun); harita dışı satırda −1.
        /// Yatayda sarmalanır, böylece haritanın herhangi bir kopyasına dokunmak aynı hücreyi verir.
        /// </summary>
        public static int Pick(Vector2 world, int mapWidth, int mapHeight)
        {
            float down = -world.y;
            float q = ((Sqrt3 / 3f) * world.x) - (down / 3f);
            float r = (2f / 3f) * down;
            float s = -q - r;

            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);
            float dq = Mathf.Abs(rq - q);
            float dr = Mathf.Abs(rr - r);
            float ds = Mathf.Abs(rs - s);
            if (dq > dr && dq > ds)
            {
                rq = -rr - rs;
            }
            else if (dr > ds)
            {
                rr = -rq - rs;
            }

            int row = rr;
            if (row < 0 || row >= mapHeight)
            {
                return -1;
            }

            int column = WrapColumn(rq + ((row - (row & 1)) / 2), mapWidth);
            return (row * mapWidth) + column;
        }

        /// <summary>
        /// Bir x konumunu kameraya en yakın harita kopyasına taşımak için eklenecek kayma (dünya
        /// genişliğinin tam katı). ±180° dikişi görünmez kılar.
        /// </summary>
        public static float WrapShift(float x, float cameraX, float worldWidth) => Mathf.Round((cameraX - x) / worldWidth) * worldWidth;
    }
}
