using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Bir harita parçasının (<see cref="ChunkSize"/> × <see cref="ChunkSize"/> hücre) ağını kurar.
    /// LOD 0: hücre başına 7 köşe / 6 üçgen (merkez açık, kenarlar koyu: hücre sınırı görünür);
    /// LOD L ≥ 1: 2^L × 2^L hücrelik blok başına tek dörtgen, renk bloğun ortasındaki hücreden.
    /// Köşeler parça başlangıcına göredir; sarmalama yalnız parçanın konumunu kaydırır. Tamponlar
    /// yeniden kullanılır (kare başına tahsis yok).
    /// </summary>
    public sealed class MapChunkBuilder
    {
        public const int ChunkSize = 16;
        private const float EdgeShade = 0.82f;

        private readonly List<Vector3> _vertices = new List<Vector3>(ChunkSize * ChunkSize * 7);
        private readonly List<Color32> _colors = new List<Color32>(ChunkSize * ChunkSize * 7);
        private readonly List<int> _triangles = new List<int>(ChunkSize * ChunkSize * 18);

        public int VertexCount => _vertices.Count;

        public int TriangleCount => _triangles.Count / 3;

        public static int ChunksAcross(int mapWidth) => (mapWidth + ChunkSize - 1) / ChunkSize;

        public static int ChunksDown(int mapHeight) => (mapHeight + ChunkSize - 1) / ChunkSize;

        /// <summary>Parçanın dünya başlangıcı (sarmalama kayması hariç).</summary>
        public static Vector2 ChunkOrigin(int chunkX, int chunkY) =>
            new Vector2(HexLayout.ColumnWidth * chunkX * ChunkSize, -HexLayout.RowHeight * chunkY * ChunkSize);

        /// <param name="cellColors">Tüm haritanın hücre renkleri (dizin = satır × genişlik + sütun).</param>
        public void Build(int chunkX, int chunkY, int mapWidth, int mapHeight, int level, Color32[] cellColors)
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();

            int c0 = chunkX * ChunkSize;
            int r0 = chunkY * ChunkSize;
            int c1 = Mathf.Min(c0 + ChunkSize, mapWidth);
            int r1 = Mathf.Min(r0 + ChunkSize, mapHeight);
            Vector2 origin = ChunkOrigin(chunkX, chunkY);

            if (level <= 0)
            {
                for (int row = r0; row < r1; row++)
                {
                    for (int column = c0; column < c1; column++)
                    {
                        AddHex(HexLayout.CellCenter(column, row) - origin, cellColors[(row * mapWidth) + column]);
                    }
                }

                return;
            }

            int step = MapLod.Step(level);
            for (int row = r0; row < r1; row += step)
            {
                int rows = Mathf.Min(step, r1 - row);
                for (int column = c0; column < c1; column += step)
                {
                    int columns = Mathf.Min(step, c1 - column);
                    int sampleRow = row + (rows / 2);
                    int sampleColumn = column + (columns / 2);
                    Color32 color = cellColors[(sampleRow * mapWidth) + sampleColumn];
                    float x0 = (HexLayout.ColumnWidth * (column - 0.25f)) - origin.x;
                    float x1 = x0 + (HexLayout.ColumnWidth * columns);
                    float yTop = (-HexLayout.RowHeight * row) + 0.75f - origin.y;
                    float yBottom = yTop - (HexLayout.RowHeight * rows);
                    AddQuad(x0, x1, yTop, yBottom, color);
                }
            }
        }

        public void Apply(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = _vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0, true);
        }

        private void AddHex(Vector2 center, Color32 color)
        {
            int first = _vertices.Count;
            Color32 edge = MapPalette.Scale(color, EdgeShade);
            _vertices.Add(center);
            _colors.Add(color);
            for (int i = 0; i < 6; i++)
            {
                _vertices.Add(center + (HexLayout.Corner(i) * 0.98f));
                _colors.Add(edge);
            }

            for (int i = 0; i < 6; i++)
            {
                // Saat yönünde (ekran): ön yüz kameraya bakar.
                _triangles.Add(first);
                _triangles.Add(first + 1 + ((i + 1) % 6));
                _triangles.Add(first + 1 + i);
            }
        }

        private void AddQuad(float x0, float x1, float yTop, float yBottom, Color32 color)
        {
            int first = _vertices.Count;
            _vertices.Add(new Vector3(x0, yTop, 0f));
            _vertices.Add(new Vector3(x1, yTop, 0f));
            _vertices.Add(new Vector3(x1, yBottom, 0f));
            _vertices.Add(new Vector3(x0, yBottom, 0f));
            for (int i = 0; i < 4; i++)
            {
                _colors.Add(color);
            }

            _triangles.Add(first);
            _triangles.Add(first + 1);
            _triangles.Add(first + 2);
            _triangles.Add(first);
            _triangles.Add(first + 2);
            _triangles.Add(first + 3);
        }
    }
}
